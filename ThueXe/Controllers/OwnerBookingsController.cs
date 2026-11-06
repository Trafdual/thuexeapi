using ThueXe.Helpers;

namespace ThueXe.Controllers
{
    [ApiController]
    [Route("owner/bookings")]
    [Authorize]
    public class OwnerBookingsController : ControllerBase
    {
        /// Cùng tỷ lệ với BookingsController.Create — đơn gốc và phần gia hạn tính hoa hồng như nhau.
        private const int HoaHongPhanTram = 15;

        private readonly ApplicationDbContext _db;
        private readonly QrService _qr;

        private readonly ThongBaoService _tb;

        public OwnerBookingsController(ApplicationDbContext db, QrService qr, ThongBaoService tb)
        {
            _db = db;
            _qr = qr;
            _tb = tb;
        }

        private long UserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET /owner/bookings — đơn của xe tôi
        [HttpGet]
        public async Task<ActionResult<List<BookingDto>>> MyBookings([FromQuery] string? status)
        {
            var q = _db.Bookings
                .Include(b => b.Car).ThenInclude(c => c.Photos)
                .Include(b => b.Renter)
                .Where(b => b.Car.OwnerId == UserId);

            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(b => b.Status == status);

            var don = await q.OrderByDescending(b => b.CreatedAt).ToListAsync();
            return await KemTrangThaiGiayTo(don);
        }

        // POST /owner/bookings/{id}/confirm — chủ xe nhận đơn
        [HttpPost("{khoa}/confirm")]
        public async Task<ActionResult<BookingDto>> Confirm(string khoa)
        {
            var don = await LayDonCuaXeToi(khoa);

            // Mọi phương thức đổi trạng thái mở đầu bằng một câu kiểm tra trạng thái hiện tại.
            if (don.Status != TrangThaiDon.ChoChuXe)
                throw new BizException("WRONG_STATE", $"Đơn đang ở {don.Status}, không nhận được");

            // Giấy tờ khách phải được duyệt trước khi chủ xe nhận, vì nhận đơn là mở luồng thu tiền.
            var giayTo = await _db.IdDocuments
                .Where(d => d.UserId == don.RenterId)
                .OrderByDescending(d => d.Id)
                .Select(d => d.Status)
                .FirstOrDefaultAsync();
            if (giayTo != TrangThaiGiayTo.Dat)
                throw new BizException("KYC_REQUIRED",
                    "Khách chưa được sàn duyệt CCCD và GPLX. Chờ sàn duyệt rồi nhận đơn, hoặc từ chối nếu không kịp.");

            don.Status = TrangThaiDon.ChoThanhToan;
            // Hẹn tiếp 30 phút cho khách chuyển tiền.
            don.HoldExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30);

            // Sinh phiếu thu kèm mã QR: khách trả TIỀN THUÊ CỘNG TIỀN CỌC trong một lần chuyển.
            // Nội dung chuyển khoản chính là mã đơn, để người vận hành đối chiếu sao kê.
            var soTien = don.RentTotal + don.Deposit;
            if (!await _db.Payments.AnyAsync(p => p.BookingId == don.Id
                                              && p.Status == TrangThaiThanhToan.Cho))
            {
                _db.Payments.Add(new Payment
                {
                    BookingId = don.Id,
                    Amount = soTien,
                    TransferCode = don.Code,
                    QrUrl = _qr.TaoUrl(don.Code, soTien),
                    Status = TrangThaiThanhToan.Cho
                });
            }

            await _db.SaveChangesAsync();
            await _tb.Gui(don.RenterId, "Chủ xe đã nhận đơn",
                $"Đơn {don.Code}: chuyển {soTien:N0}đ (thuê + cọc) trong 30 phút để giữ xe.", don.Code);
            return (await KemTrangThaiGiayTo(new List<Booking> { don })).Value!.Single();
        }

        // POST /owner/bookings/{id}/reject — chủ xe từ chối đơn
        [HttpPost("{khoa}/reject")]
        public async Task<ActionResult<BookingDto>> Reject(string khoa, RejectBookingRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Reason))
                throw new BizException("INVALID_INPUT", "Phải ghi lý do từ chối");

            var don = await LayDonCuaXeToi(khoa);
            if (don.Status != TrangThaiDon.ChoChuXe)
                throw new BizException("WRONG_STATE", $"Đơn đang ở {don.Status}, không từ chối được");

            don.Status = TrangThaiDon.BiTuChoi;
            don.CancelReason = req.Reason;
            don.HoldExpiresAt = null;

            // Nhả lịch ngay. Chưa thu tiền nên không phải hoàn.
            await NhaLich(don.Id);

            await _db.SaveChangesAsync();
            await _tb.Gui(don.RenterId, "Chủ xe từ chối đơn", $"Đơn {don.Code}: {req.Reason}", don.Code);
            return (await KemTrangThaiGiayTo(new List<Booking> { don })).Value!.Single();
        }

        // POST /owner/bookings/{id}/extend-approve — duyệt gia hạn: khoá thêm ngày, cộng tiền
        // thuê và ghi thành nợ khách (cùng cơ chế phí vượt cọc, ép trả trước khi đặt chuyến mới).
        [HttpPost("{khoa}/extend-approve")]
        public async Task<ActionResult<BookingDto>> ExtendApprove(string khoa)
        {
            var don = await LayDonCuaXeToi(khoa);
            if (don.ExtendRequestedEndDate is not { } ngayMoi)
                throw new BizException("WRONG_STATE", "Đơn này không có yêu cầu gia hạn nào đang chờ");

            var ngayCu = don.EndDate;
            var soNgayThem = ngayMoi.DayNumber - ngayCu.DayNumber;

            await using var giaoDich = await _db.Database.BeginTransactionAsync();
            try
            {
                // Ngày cũ (EndDate) đã khoá riêng cho đơn này rồi, chỉ cần khoá thêm từ hôm sau.
                for (var d = ngayCu.AddDays(1); d <= ngayMoi; d = d.AddDays(1))
                    _db.CarAvailabilities.Add(new CarAvailability { CarId = don.CarId, Day = d, BookingId = don.Id });
                await _db.SaveChangesAsync();
                await giaoDich.CommitAsync();
            }
            catch (DbUpdateException)
            {
                await giaoDich.RollbackAsync();
                throw new BizException("SLOT_TAKEN", "Những ngày gia hạn thêm vừa có người khác đặt mất rồi");
            }

            var tienThem = soNgayThem * don.PricePerDay;
            don.EndDate = ngayMoi;
            don.Days += soNgayThem;
            don.RentTotal += tienThem;
            don.Commission += tienThem * HoaHongPhanTram / 100;
            don.DebtAmount += tienThem;
            don.DebtPaidAt = null;
            don.ExtendRequestedEndDate = null;
            don.ExtendRequestedAt = null;
            await _db.SaveChangesAsync();

            await _tb.Gui(don.RenterId, "Chủ xe đã duyệt gia hạn",
                $"Đơn {don.Code}: trả xe {ngayMoi:dd/MM}, phát sinh thêm {tienThem:N0}đ. " +
                "Thanh toán trước khi đặt chuyến mới.", don.Code);
            return (await KemTrangThaiGiayTo(new List<Booking> { don })).Value!.Single();
        }

        // POST /owner/bookings/{id}/extend-reject — từ chối gia hạn
        [HttpPost("{khoa}/extend-reject")]
        public async Task<ActionResult<BookingDto>> ExtendReject(string khoa)
        {
            var don = await LayDonCuaXeToi(khoa);
            if (don.ExtendRequestedEndDate is null)
                throw new BizException("WRONG_STATE", "Đơn này không có yêu cầu gia hạn nào đang chờ");

            don.ExtendRequestedEndDate = null;
            don.ExtendRequestedAt = null;
            await _db.SaveChangesAsync();

            await _tb.Gui(don.RenterId, "Chủ xe từ chối gia hạn",
                $"Đơn {don.Code}: chủ xe không đồng ý gia hạn, vẫn trả xe {don.EndDate:dd/MM}.", don.Code);
            return (await KemTrangThaiGiayTo(new List<Booking> { don })).Value!.Single();
        }

        private async Task NhaLich(long bookingId)
        {
            var dong = await _db.CarAvailabilities.Where(a => a.BookingId == bookingId).ToListAsync();
            _db.CarAvailabilities.RemoveRange(dong);
        }

        private async Task<Booking> LayDonCuaXeToi(string khoa)
        {
            var don = await _db.Bookings
                .Include(b => b.Car).ThenInclude(c => c.Photos)
                .Include(b => b.Renter)
                .TheoKhoa(khoa)
                .FirstOrDefaultAsync()
                ?? throw new BizException("NOT_FOUND", "Không tìm thấy đơn");

            if (don.Car.OwnerId != UserId)
                throw new BizException("FORBIDDEN", "Đơn này không phải của xe bạn");

            return don;
        }

        /// Lấy trạng thái giấy tờ của từng khách trong một lượt, tránh N+1.
        private async Task<ActionResult<List<BookingDto>>> KemTrangThaiGiayTo(List<Booking> don)
        {
            var khachId = don.Select(b => b.RenterId).Distinct().ToList();

            var giayTo = await _db.IdDocuments
                .Where(d => khachId.Contains(d.UserId))
                .GroupBy(d => d.UserId)
                .Select(g => new { UserId = g.Key, Status = g.OrderByDescending(x => x.Id).First().Status })
                .ToDictionaryAsync(x => x.UserId, x => x.Status);

            return don.Select(b => b.ToDto(giayTo.GetValueOrDefault(b.RenterId, "CHUA_NOP"))).ToList();
        }
    }
}
