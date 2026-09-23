using ThueXe.Helpers;

namespace ThueXe.Controllers
{
    [ApiController]
    [Route("admin/ledger")]
    [Authorize(Roles = Vai.VanHanh)]
    public class AdminLedgerController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public AdminLedgerController(ApplicationDbContext db) => _db = db;

        // GET /admin/ledger?bookingId= — tra sổ cái một đơn, dùng khi đối soát lệch
        [HttpGet]
        public async Task<ActionResult<LedgerResult>> Get([FromQuery] long bookingId)
        {
            if (bookingId <= 0)
                throw new BizException("INVALID_INPUT", "Thiếu bookingId");

            if (!await _db.Bookings.AnyAsync(b => b.Id == bookingId))
                throw new BizException("NOT_FOUND", "Không tìm thấy đơn");

            var but = await _db.LedgerEntries
                .Where(e => e.BookingId == bookingId)
                .OrderBy(e => e.Id)
                .ToListAsync();

            var tongNo = but.Where(e => e.Direction == Chieu.No).Sum(e => e.Amount);
            var tongCo = but.Where(e => e.Direction == Chieu.Co).Sum(e => e.Amount);

            // Bút toán kép thì hai vế luôn bằng nhau. Lệch một đồng cũng không bỏ qua —
            // web phải tô đỏ khi CanBang là false.
            return new LedgerResult(
                bookingId, but.Select(e => e.ToDto()).ToList(),
                tongNo, tongCo, tongNo == tongCo);
        }
    }
}
