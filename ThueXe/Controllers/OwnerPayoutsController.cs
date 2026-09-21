using ThueXe.Helpers;

namespace ThueXe.Controllers
{
    [ApiController]
    [Route("owner/payouts")]
    [Authorize]
    public class OwnerPayoutsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public OwnerPayoutsController(ApplicationDbContext db) => _db = db;

        private long UserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET /owner/payouts — tiền đã nhận và tiền đang chờ
        [HttpGet]
        public async Task<ActionResult<List<PayoutDto>>> MyPayouts()
        {
            var lenh = await _db.Payouts
                .Include(p => p.Booking)
                .Where(p => p.PayeeType == Ben.ChuXe && p.PayeeId == UserId)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            return lenh.Select(p => p.ToDto(p.Booking?.Code)).ToList();
        }
    }
}
