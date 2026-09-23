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

        // GET /cars — tìm xe có lọc, phân trang
        [HttpGet]
        public async Task<ActionResult<Paged<CarSearchItemDto>>> Search(
            [FromQuery] string? district,
            [FromQuery] DateOnly? from,
            [FromQuery] DateOnly? to,
            [FromQuery] int? seats,
            [FromQuery] string? transmission,
            [FromQuery] long? maxPrice,
            [FromQuery] int page = 1,
            [FromQuery] int size = 20)
        {
            page = Math.Max(1, page);
            size = Math.Clamp(size, 1, 50);

            // Chỉ xe đang bán mới hiện. Xe nháp, chờ duyệt hay bị ẩn thì khách không thấy.
            var q = _db.Cars
                .Include(c => c.Photos)
                .Where(c => c.Status == TrangThaiXe.DangBan);

            if (!string.IsNullOrWhiteSpace(district)) q = q.Where(c => c.District == district);
            if (seats is > 0) q = q.Where(c => c.Seats >= seats);
            if (!string.IsNullOrWhiteSpace(transmission)) q = q.Where(c => c.Transmission == transmission);
            if (maxPrice is > 0) q = q.Where(c => c.PricePerDay <= maxPrice);

            // Lọc theo khoảng ngày: bỏ hết xe đã bận dù chỉ một ngày trong khoảng.
            if (from is not null && to is not null)
            {
                if (to <= from)
                    throw new BizException("INVALID_INPUT", "Ngày trả phải sau ngày nhận");

                var ban = _db.CarAvailabilities
                    .Where(a => a.Day >= from && a.Day <= to)
                    .Select(a => a.CarId);
                q = q.Where(c => !ban.Contains(c.Id));
            }

            var tong = await q.CountAsync();

            // Include ảnh rồi mới phân trang để tránh bệnh N+1: một truy vấn, không phải
            // một truy vấn cho mỗi xe.
            var xe = await q
                .OrderBy(c => c.PricePerDay).ThenBy(c => c.Id)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();

            var items = xe.Select(c => new CarSearchItemDto(
                c.Id, c.Plate, c.Brand, c.Model, c.Year, c.Seats,
                c.Transmission, c.Fuel, c.District,
                c.PricePerDay, c.MaxKmDay, c.Deposit,
                c.Photos.OrderBy(p => p.SortOrder).Select(p => p.Url).FirstOrDefault())).ToList();

            return new Paged<CarSearchItemDto>(
                items, page, size, tong, (int)Math.Ceiling(tong / (double)size));
        }

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
