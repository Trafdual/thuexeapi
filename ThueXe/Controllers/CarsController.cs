using ThueXe.Helpers;

namespace ThueXe.Controllers
{
    [ApiController]
    [Route("cars")]
    [Authorize]
    public class CarsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public CarsController(ApplicationDbContext db) => _db = db;

        private long UserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET /cars/{id} — chi tiết xe + ảnh
        [HttpGet("{id:long}")]
        public async Task<ActionResult<CarDto>> Get(long id)
        {
            var xe = await _db.Cars
                .Include(c => c.Photos)
                .Include(c => c.Documents)
                .FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new BizException("CAR_UNAVAILABLE", "Không tìm thấy xe");

            var dto = xe.ToDto();

            // Địa chỉ giao xe chỉ mở cho chủ xe, hoặc cho khách có đơn đã xác nhận.
            if (xe.OwnerId != UserId)
            {
                var duocXem = await _db.Bookings.AnyAsync(b =>
                    b.CarId == id && b.RenterId == UserId &&
                    !TrangThaiDon.DaDong.Contains(b.Status) &&
                    b.Status != TrangThaiDon.ChoChuXe &&
                    b.Status != TrangThaiDon.ChoThanhToan);

                if (!duocXem) dto = dto with { PickupAddress = null };
            }

            return dto;
        }

        // GET /cars/{id}/availability — ngày bận trong khoảng, để tô lịch
        [HttpGet("{id:long}/availability")]
        public async Task<ActionResult<AvailabilityDto>> Availability(
            long id, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
        {
            if (!await _db.Cars.AnyAsync(c => c.Id == id))
                throw new BizException("CAR_UNAVAILABLE", "Không tìm thấy xe");

            var tu = from ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var den = to ?? tu.AddDays(30);
            if (den < tu) throw new BizException("INVALID_INPUT", "Khoảng ngày không hợp lệ");

            var dong = await _db.CarAvailabilities
                .Where(a => a.CarId == id && a.Day >= tu && a.Day <= den)
                .ToListAsync();

            // Một bảng giữ cả hai loại: có booking_id là đơn giữ, null là chủ xe tự khoá.
            return new AvailabilityDto(
                id, tu, den,
                dong.Where(a => a.BookingId != null).Select(a => a.Day).OrderBy(d => d).ToList(),
                dong.Where(a => a.BookingId == null).Select(a => a.Day).OrderBy(d => d).ToList());
        }
    }
}
