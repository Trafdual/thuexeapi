using ThueXe.Helpers;

namespace ThueXe.Controllers
{
    [ApiController]
    [Route("bookings")]
    [Authorize]
    public class BookingsController : ControllerBase
    {
        private const int HoaHongPhanTram = 15;

        private readonly ApplicationDbContext _db;

        public BookingsController(ApplicationDbContext db) => _db = db;

        private long UserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET /bookings — đơn của tôi, phía khách
        [HttpGet]
        public async Task<ActionResult<List<BookingDto>>> MyBookings([FromQuery] string? status)
        {
            var q = _db.Bookings
                .Include(b => b.Car).ThenInclude(c => c.Photos)
                .Include(b => b.Renter)
                .Where(b => b.RenterId == UserId);

            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(b => b.Status == status);

            var don = await q.OrderByDescending(b => b.CreatedAt).ToListAsync();
            return don.Select(b => b.ToDto()).ToList();
        }

        // POST /bookings/quote — tính giá, KHÔNG tạo đơn
        [HttpPost("quote")]
        public async Task<ActionResult<QuoteResult>> Quote(QuoteRequest req)
        {
            var xe = await LayXeDatDuoc(req.CarId);
            var soNgay = KiemNgay(req.StartDate, req.EndDate);

            var ban = await NgayDaBan(req.CarId, req.StartDate, req.EndDate);

            var tienThue = xe.PricePerDay * soNgay;
            return new QuoteResult(
                xe.Id, req.StartDate, req.EndDate, soNgay,
                xe.PricePerDay, tienThue, xe.Deposit,
                tienThue * HoaHongPhanTram / 100,
                tienThue + xe.Deposit,
                ban.Count == 0, ban);
        }

        // POST /bookings — tạo đơn, giữ lịch, chống hai người đặt trùng ngày
        [HttpPost]
        public async Task<ActionResult<BookingDto>> Create(CreateBookingRequest req)
        {
            var xe = await LayXeDatDuoc(req.CarId);
            var soNgay = KiemNgay(req.StartDate, req.EndDate);

            // B4: không thuê xe của chính mình.
            if (xe.OwnerId == UserId)
                throw new BizException("CAR_UNAVAILABLE", "Không thể thuê xe của chính bạn");

            // A1: chưa được duyệt giấy tờ thì không đặt được.
            var giayTo = await _db.IdDocuments
                .Where(d => d.UserId == UserId)
                .OrderByDescending(d => d.Id)
                .Select(d => d.Status)
                .FirstOrDefaultAsync();
            if (giayTo != TrangThaiGiayTo.Dat)
                throw new BizException("KYC_REQUIRED", "Cần nộp và được duyệt CCCD, GPLX trước");

            // A3: GPLX hết hạn thì chặn đặt đơn mới.
            var hanGplx = await _db.IdDocuments
                .Where(d => d.UserId == UserId && d.Status == TrangThaiGiayTo.Dat)
                .OrderByDescending(d => d.Id)
                .Select(d => d.GplxExpiry)
                .FirstOrDefaultAsync();
            if (hanGplx is not null && hanGplx < req.EndDate)
                throw new BizException("KYC_REQUIRED", "GPLX hết hạn trước ngày trả xe");

            var tienThue = xe.PricePerDay * soNgay;
            var don = new Booking
            {
                Code = MaDonService.Sinh(),
                CarId = xe.Id,
                RenterId = UserId,
                StartDate = req.StartDate,
                EndDate = req.EndDate,
                Days = soNgay,
                PricePerDay = xe.PricePerDay,
                RentTotal = tienThue,
                Deposit = xe.Deposit,
                Commission = tienThue * HoaHongPhanTram / 100,
                Status = TrangThaiDon.ChoChuXe,
                // Hẹn chủ xe 30 phút. CHƯA THU TIỀN ở bước này.
                HoldExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30)
            };

            await using var giaoDich = await _db.Database.BeginTransactionAsync();
            try
            {
                _db.Bookings.Add(don);
                await _db.SaveChangesAsync();   // lấy id trước khi giữ lịch

                // Đừng kiểm tra rồi mới ghi — giữa hai bước đó là chỗ đơn thứ hai chen vào.
                // Dùng thẳng khoá chính (car_id, day) làm trọng tài: ai chèn được thì thắng.
                // KHOÁ CẢ NGÀY TRẢ: thuê 12→15 là khoá bốn ngày 12, 13, 14, 15. Chỉ khoá tới
                // 14 thì người khác đặt được ngày 15 trong khi xe còn chưa về.
                for (var d = req.StartDate; d <= req.EndDate; d = d.AddDays(1))
                    _db.CarAvailabilities.Add(new CarAvailability
                    {
                        CarId = xe.Id,
                        Day = d,
                        BookingId = don.Id
                    });

                await _db.SaveChangesAsync();
                await giaoDich.CommitAsync();
            }
            catch (DbUpdateException)
            {
                // Đụng khoá trùng dù chỉ một ngày là cả lệnh đổ. Giao dịch cuộn lại,
                // đơn cũng biến mất — không để lại dòng mồ côi.
                await giaoDich.RollbackAsync();
                throw new BizException("SLOT_TAKEN",
                    "Những ngày này vừa có người khác đặt mất rồi");
            }

            await _db.Entry(don).Reference(b => b.Car).LoadAsync();
            await _db.Entry(don.Car).Collection(c => c.Photos).LoadAsync();
            await _db.Entry(don).Reference(b => b.Renter).LoadAsync();
            return don.ToDto();
        }

        private async Task<Car> LayXeDatDuoc(long carId)
        {
            var xe = await _db.Cars.FirstOrDefaultAsync(c => c.Id == carId)
                     ?? throw new BizException("CAR_UNAVAILABLE", "Không tìm thấy xe");

            // B7: xe bị gỡ hoặc ẩn giữa lúc khách đang đặt.
            if (xe.Status != TrangThaiXe.DangBan)
                throw new BizException("CAR_UNAVAILABLE", "Xe này đang không cho thuê");

            return xe;
        }

        /// B3: ngày quá khứ, hoặc ngày trả không sau ngày nhận → chặn ngay ở tầng dữ liệu vào.
        private static int KiemNgay(DateOnly batDau, DateOnly ketThuc)
        {
            var homNay = DateOnly.FromDateTime(DateTime.UtcNow);
            if (batDau < homNay)
                throw new BizException("INVALID_INPUT", "Không đặt được ngày trong quá khứ");
            if (ketThuc <= batDau)
                throw new BizException("INVALID_INPUT", "Ngày trả phải sau ngày nhận");

            // Ngày trả KHÔNG tính tiền: thuê 12→15 là ba ngày.
            return ketThuc.DayNumber - batDau.DayNumber;
        }

        private async Task<List<DateOnly>> NgayDaBan(long carId, DateOnly tu, DateOnly den) =>
            await _db.CarAvailabilities
                .Where(a => a.CarId == carId && a.Day >= tu && a.Day <= den)
                .Select(a => a.Day)
                .OrderBy(d => d)
                .ToListAsync();

        // GET /bookings/{id} — chi tiết đơn + thanh toán + biên bản
        [HttpGet("{khoa}")]
        public async Task<ActionResult<BookingDetailDto>> Get(string khoa)
        {
            var don = await LayDonLienQuan(khoa);

            var thanhToan = await _db.Payments
                .Where(p => p.BookingId == don.Id)
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();

            var bienBan = await _db.Handovers
                .Include(h => h.Photos)
                .Where(h => h.BookingId == don.Id)
                .ToListAsync();

            var phi = await _db.Charges.Where(c => c.BookingId == don.Id).ToListAsync();

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
        [HttpPost("{khoa}/cancel")]
        public async Task<ActionResult<BookingDto>> Cancel(string khoa, CancelBookingRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Reason))
                throw new BizException("INVALID_INPUT", "Phải ghi lý do huỷ");

            var don = await LayDonLienQuan(khoa);

            if (TrangThaiDon.DaDong.Contains(don.Status))
                throw new BizException("WRONG_STATE", "Đơn đã đóng");

            // G4: xe đã ra khỏi tay chủ thì phải đi qua đường trả xe, không huỷ được nữa.
            var coBienBanGiao = await _db.Handovers
                .AnyAsync(h => h.BookingId == don.Id && h.Kind == LoaiBienBan.Giao);
            if (coBienBanGiao)
                throw new BizException("WRONG_STATE", "Đã có biên bản giao, phải đi qua đường trả xe");

            don.Status = TrangThaiDon.DaHuy;
            don.CancelReason = req.Reason;
            don.HoldExpiresAt = null;

            var dong = await _db.CarAvailabilities.Where(a => a.BookingId == don.Id).ToListAsync();
            _db.CarAvailabilities.RemoveRange(dong);

            await _db.SaveChangesAsync();
            return don.ToDto();
        }

        // POST /bookings/{id}/handovers — bên lập nộp biên bản, vào trạng thái CHO_SOI
        [HttpPost("{khoa}/handovers")]
        public async Task<ActionResult<HandoverDto>> CreateHandover(string khoa, CreateHandoverRequest req)
        {
            var don = await LayDonLienQuan(khoa);
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

            if (await _db.Handovers.AnyAsync(h => h.BookingId == don.Id && h.Kind == req.Kind))
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
                BookingId = don.Id,
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

        private async Task<Booking> LayDonLienQuan(string khoa)
        {
            var don = await _db.Bookings
                .Include(b => b.Car).ThenInclude(c => c.Photos)
                .Include(b => b.Renter)
                .TheoKhoa(khoa)
                .FirstOrDefaultAsync();

            // Đơn không tồn tại và đơn của người khác phải trả về CÙNG MỘT phản hồi.
            // Phân biệt hai ca là biến API thành máy dò: gọi tuần tự /bookings/1,2,3…
            // rồi đếm xem ca nào trả FORBIDDEN là biết sàn có bao nhiêu đơn.
            if (don is null || (don.Car.OwnerId != UserId && don.RenterId != UserId))
                throw new BizException("NOT_FOUND", "Không tìm thấy đơn");

            return don;
        }
    }
}
