using ThueXe.Helpers;

namespace ThueXe.Controllers
{
    [ApiController]
    [Route("admin/payouts")]
    [Authorize(Roles = Vai.VanHanh)]
    public class AdminPayoutsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public AdminPayoutsController(ApplicationDbContext db) => _db = db;

        private long UserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET /admin/payouts?status=CHO — danh sách phải chuyển khoản, việc mỗi tối
        [HttpGet]
        public async Task<ActionResult<List<PayoutAdminDto>>> Queue([FromQuery] string? status)
        {
            var q = _db.Payouts.Include(p => p.Booking).AsQueryable();
            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(p => p.Status == status);

            var ds = await q.OrderBy(p => p.Id).ToListAsync();

            var nguoiNhanId = ds.Select(p => p.PayeeId).Distinct().ToList();
            var nguoiNhan = await _db.AppUsers
                .Where(u => nguoiNhanId.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            return ds
                .Where(p => nguoiNhan.ContainsKey(p.PayeeId))
                .Select(p => p.ToAdminDto(p.Booking?.Code, nguoiNhan[p.PayeeId]))
                .ToList();
        }

        // POST /admin/payouts/{id}/paid — ghi nhận đã chuyển, dán mã giao dịch trên sao kê
        [HttpPost("{id:long}/paid")]
        public async Task<ActionResult<PayoutDto>> MarkPaid(long id, MarkPaidRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.TransferRef))
                throw new BizException("INVALID_INPUT", "Phải dán mã giao dịch trên sao kê");

            var lenh = await _db.Payouts
                .Include(p => p.Booking)
                .FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new BizException("NOT_FOUND", "Không tìm thấy lệnh chi");

            if (lenh.Status == TrangThaiChiTra.DaChi)
                throw new BizException("WRONG_STATE", "Lệnh chi này đã chuyển rồi");

            lenh.Status = TrangThaiChiTra.DaChi;
            lenh.TransferRef = req.TransferRef;
            lenh.PaidBy = UserId;
            lenh.PaidAt = DateTimeOffset.UtcNow;

            // F5: còn một lệnh chờ thì đơn CHƯA đóng. Chỉ khi cả hai lệnh xong mới HOAN_TAT.
            var don = lenh.Booking;
            if (don is not null && don.Status == TrangThaiDon.ChoChiTra)
            {
                var conCho = await _db.Payouts
                    .AnyAsync(p => p.BookingId == don.Id
                                && p.Id != lenh.Id
                                && p.Status != TrangThaiChiTra.DaChi);
                if (!conCho) don.Status = TrangThaiDon.HoanTat;
            }

            await _db.SaveChangesAsync();
            return lenh.ToDto(don?.Code);
        }
    }
}
