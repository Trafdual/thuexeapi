using ThueXe.Helpers;

namespace ThueXe.Controllers
{
    [ApiController]
    [Route("bookings")]
    [Authorize]
    public class BookingsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public BookingsController(ApplicationDbContext db) => _db = db;

        private long UserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET /bookings/{id} — chi tiết đơn + thanh toán + biên bản
        [HttpGet("{id:long}")]
        public async Task<ActionResult<BookingDetailDto>> Get(long id)
        {
            var don = await LayDonLienQuan(id);

            var thanhToan = await _db.Payments
                .Where(p => p.BookingId == id)
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();

            var bienBan = await _db.Handovers
                .Include(h => h.Photos)
                .Where(h => h.BookingId == id)
                .ToListAsync();

            var phi = await _db.Charges.Where(c => c.BookingId == id).ToListAsync();

            var giayTo = await _db.IdDocuments
                .Where(d => d.UserId == don.RenterId)
                .OrderByDescending(d => d.Id)
                .Select(d => d.Status)
                .FirstOrDefaultAsync();

            return new BookingDetailDto(
                don.ToDto(giayTo ?? "CHUA_NOP"),
                thanhToan is null ? null : new PaymentDto(
                    thanhToan.Id, thanhToan.BookingId, thanhToan.Amount, thanhToan.TransferCode,
                    thanhToan.QrUrl, thanhToan.Status, thanhToan.ReceivedAmount,
                    thanhToan.ConfirmedAt, thanhToan.BankNote),
                bienBan.Select(h => h.ToDto()).ToList(),
                phi.Select(c => new ChargeDto(c.Id, c.Type, c.Amount, c.Note)).ToList());
        }

        // POST /bookings/{id}/cancel — nhả lịch, ghi lý do
        [HttpPost("{id:long}/cancel")]
        public async Task<ActionResult<BookingDto>> Cancel(long id, CancelBookingRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Reason))
                throw new BizException("INVALID_INPUT", "Phải ghi lý do huỷ");

            var don = await LayDonLienQuan(id);

            if (TrangThaiDon.DaDong.Contains(don.Status))
                throw new BizException("WRONG_STATE", "Đơn đã đóng");

            // G4: xe đã ra khỏi tay chủ thì phải đi qua đường trả xe, không huỷ được nữa.
            var coBienBanGiao = await _db.Handovers
                .AnyAsync(h => h.BookingId == id && h.Kind == LoaiBienBan.Giao);
            if (coBienBanGiao)
                throw new BizException("WRONG_STATE", "Đã có biên bản giao, phải đi qua đường trả xe");

            don.Status = TrangThaiDon.DaHuy;
            don.CancelReason = req.Reason;
            don.HoldExpiresAt = null;

            var dong = await _db.CarAvailabilities.Where(a => a.BookingId == id).ToListAsync();
            _db.CarAvailabilities.RemoveRange(dong);

            await _db.SaveChangesAsync();
            return don.ToDto();
        }

        // POST /bookings/{id}/handovers — bên lập nộp biên bản, vào trạng thái CHO_SOI
        [HttpPost("{id:long}/handovers")]
        public async Task<ActionResult<HandoverDto>> CreateHandover(long id, CreateHandoverRequest req)
        {
            var don = await LayDonLienQuan(id);
            var laChuXe = don.Car.OwnerId == UserId;

            if (req.Kind is not (LoaiBienBan.Giao or LoaiBienBan.Tra))
                throw new BizException("INVALID_INPUT", "Loại biên bản phải là GIAO hoặc TRA");

            // Chủ xe lập biên bản giao, khách lập biên bản trả — không ai lập thay ai.
            if (req.Kind == LoaiBienBan.Giao && !laChuXe)
                throw new BizException("FORBIDDEN", "Biên bản giao do chủ xe lập");
            if (req.Kind == LoaiBienBan.Tra && laChuXe)
                throw new BizException("FORBIDDEN", "Biên bản trả do khách lập");

            var canTrangThai = req.Kind == LoaiBienBan.Giao
                ? TrangThaiDon.DaXacNhan
                : TrangThaiDon.DangThue;
            if (don.Status != canTrangThai)
                throw new BizException("WRONG_STATE", $"Đơn đang ở {don.Status}");

            if (await _db.Handovers.AnyAsync(h => h.BookingId == id && h.Kind == req.Kind))
                throw new BizException("WRONG_STATE", $"Biên bản {req.Kind} của đơn này đã lập rồi");

            // D7: thiếu ảnh hay thiếu chữ ký thì chỉ đúng ô còn thiếu, không xoá cái đã nhập.
            var anh = req.Photos ?? new List<HandoverPhotoRequest>();
            var thieu = KhungAnh.BatBuoc.Where(k => anh.All(a => a.Slot != k)).ToList();
            if (thieu.Count > 0 || string.IsNullOrWhiteSpace(req.Signature))
                throw new BizException("HANDOVER_INCOMPLETE",
                    "Còn thiếu: " + (thieu.Count > 0 ? string.Join(", ", thieu) : "chữ ký"));

            if (req.FuelLevel is < 0 or > 8)
                throw new BizException("INVALID_INPUT", "Mức nhiên liệu nằm ngoài thang 0..8");

            var ben = laChuXe ? Ben.ChuXe : Ben.Khach;
            var bienBan = new Handover
            {
                BookingId = id,
                Kind = req.Kind,
                CreatedBy = ben,
                Odo = req.Odo,
                FuelLevel = req.FuelLevel,
                Note = req.Note,
                Status = TrangThaiBienBan.ChoSoi,
                SignCreator = req.Signature,
                CreatedAt = DateTimeOffset.UtcNow
            };
            foreach (var a in anh)
                bienBan.Photos.Add(new HandoverPhoto
                {
                    Slot = a.Slot,
                    Url = a.Url,
                    TakenBy = ben,
                    Note = a.Note
                });

            _db.Handovers.Add(bienBan);
            await _db.SaveChangesAsync();
            return bienBan.ToDto();
        }

        private async Task<Booking> LayDonLienQuan(long id)
        {
            var don = await _db.Bookings
                .Include(b => b.Car).ThenInclude(c => c.Photos)
                .Include(b => b.Renter)
                .FirstOrDefaultAsync(b => b.Id == id)
                ?? throw new BizException("WRONG_STATE", "Không tìm thấy đơn");

            if (don.Car.OwnerId != UserId && don.RenterId != UserId)
                throw new BizException("FORBIDDEN", "Đơn này không liên quan tới bạn");

            return don;
        }
    }
}
