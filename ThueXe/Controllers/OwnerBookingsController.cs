using ThueXe.Helpers;

namespace ThueXe.Controllers
{
    [ApiController]
    [Route("owner/bookings")]
    [Authorize]
    public class OwnerBookingsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly QrService _qr;

        public OwnerBookingsController(ApplicationDbContext db, QrService qr)
        {
            _db = db;
            _qr = qr;
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
        [HttpPost("{id:long}/confirm")]
        public async Task<ActionResult<BookingDto>> Confirm(long id)
        {
            var don = await LayDonCuaXeToi(id);

            // Mọi phương thức đổi trạng thái mở đầu bằng một câu kiểm tra trạng thái hiện tại.
            if (don.Status != TrangThaiDon.ChoChuXe)
                throw new BizException("WRONG_STATE", $"Đơn đang ở {don.Status}, không nhận được");

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
            return (await KemTrangThaiGiayTo(new List<Booking> { don })).Value!.Single();
        }

        // POST /owner/bookings/{id}/reject — chủ xe từ chối đơn
        [HttpPost("{id:long}/reject")]
        public async Task<ActionResult<BookingDto>> Reject(long id, RejectBookingRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Reason))
                throw new BizException("INVALID_INPUT", "Phải ghi lý do từ chối");

            var don = await LayDonCuaXeToi(id);
            if (don.Status != TrangThaiDon.ChoChuXe)
                throw new BizException("WRONG_STATE", $"Đơn đang ở {don.Status}, không từ chối được");

            don.Status = TrangThaiDon.BiTuChoi;
            don.CancelReason = req.Reason;
            don.HoldExpiresAt = null;

            // Nhả lịch ngay. Chưa thu tiền nên không phải hoàn.
            await NhaLich(don.Id);

            await _db.SaveChangesAsync();
            return (await KemTrangThaiGiayTo(new List<Booking> { don })).Value!.Single();
        }

        private async Task NhaLich(long bookingId)
        {
            var dong = await _db.CarAvailabilities.Where(a => a.BookingId == bookingId).ToListAsync();
            _db.CarAvailabilities.RemoveRange(dong);
        }

        private async Task<Booking> LayDonCuaXeToi(long id)
        {
            var don = await _db.Bookings
                .Include(b => b.Car).ThenInclude(c => c.Photos)
                .Include(b => b.Renter)
                .FirstOrDefaultAsync(b => b.Id == id)
                ?? throw new BizException("WRONG_STATE", "Không tìm thấy đơn");

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
