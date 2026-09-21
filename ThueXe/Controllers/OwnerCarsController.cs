using ThueXe.Helpers;

namespace ThueXe.Controllers
{
    [ApiController]
    [Route("owner/cars")]
    [Authorize]
    public class OwnerCarsController : ControllerBase
    {
        private const string BanCamKetHienHanh = "1.0";

        private readonly ApplicationDbContext _db;

        public OwnerCarsController(ApplicationDbContext db) => _db = db;

        private long UserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET /owner/cars — xe của tôi
        [HttpGet]
        public async Task<ActionResult<List<CarDto>>> MyCars()
        {
            var xe = await _db.Cars
                .Include(c => c.Photos)
                .Include(c => c.Documents)
                .Where(c => c.OwnerId == UserId)
                .OrderByDescending(c => c.Id)
                .ToListAsync();

            return xe.Select(c => c.ToDto()).ToList();
        }

        // POST /owner/cars — đăng xe mới, vào trạng thái chờ duyệt
        [HttpPost]
        public async Task<ActionResult<CarDto>> Create(CarUpsertRequest req)
        {
            // A4: chưa ký cam kết thì không có đường nào đăng được xe.
            var daKy = await _db.OwnerAgreements
                .AnyAsync(a => a.OwnerId == UserId && a.Version == BanCamKetHienHanh);
            if (!daKy)
                throw new BizException("OWNER_AGREEMENT_REQUIRED", "Phải ký cam kết chủ xe trước");

            var bien = (req.Plate ?? "").Trim().ToUpperInvariant();
            if (bien.Length == 0) throw new BizException("INVALID_INPUT", "Thiếu biển số");

            // A7: biển số trùng bị chặn ngay, không đợi ràng buộc CSDL ném ra.
            if (await _db.Cars.AnyAsync(c => c.Plate == bien))
                throw new BizException("PLATE_TAKEN", "Xe này đã có người đăng");

            var xe = new Car
            {
                OwnerId = UserId,
                Plate = bien,
                Brand = req.Brand,
                Model = req.Model,
                Year = req.Year,
                Seats = req.Seats,
                Transmission = req.Transmission,
                Fuel = req.Fuel,
                Odo = req.Odo,
                District = req.District,
                PickupAddress = req.PickupAddress,
                PricePerDay = req.PricePerDay,
                MaxKmDay = req.MaxKmDay == 0 ? 300 : req.MaxKmDay,
                Deposit = req.Deposit == 0 ? 3_000_000 : req.Deposit,
                Description = req.Description,
                Status = TrangThaiXe.ChoDuyet,
                CreatedAt = DateTimeOffset.UtcNow
            };

            GanAnhVaGiayTo(xe, req);

            _db.Cars.Add(xe);
            await _db.SaveChangesAsync();
            return xe.ToDto();
        }

        // PUT /owner/cars/{id} — sửa giá, mô tả, ảnh
        [HttpPut("{id:long}")]
        public async Task<ActionResult<CarDto>> Update(long id, CarUpsertRequest req)
        {
            var xe = await LayXeCuaToi(id);

            xe.Brand = req.Brand;
            xe.Model = req.Model;
            xe.Year = req.Year;
            xe.Seats = req.Seats;
            xe.Transmission = req.Transmission;
            xe.Fuel = req.Fuel;
            xe.Odo = req.Odo;
            xe.District = req.District;
            xe.PickupAddress = req.PickupAddress;
            xe.PricePerDay = req.PricePerDay;
            if (req.MaxKmDay > 0) xe.MaxKmDay = req.MaxKmDay;
            if (req.Deposit > 0) xe.Deposit = req.Deposit;
            xe.Description = req.Description;

            if (req.PhotoUrls is { Count: > 0 })
            {
                _db.CarPhotos.RemoveRange(xe.Photos);
                xe.Photos.Clear();
            }
            if (req.Documents is { Count: > 0 })
            {
                _db.CarDocuments.RemoveRange(xe.Documents);
                xe.Documents.Clear();
            }
            GanAnhVaGiayTo(xe, req);

            // Sửa xe đang bán thì phải duyệt lại — giá và ảnh là thứ khách nhìn để đặt.
            if (xe.Status == TrangThaiXe.DangBan) xe.Status = TrangThaiXe.ChoDuyet;

            await _db.SaveChangesAsync();
            return xe.ToDto();
        }

        // PUT /owner/cars/{id}/calendar — mở hoặc khoá ngày
        [HttpPut("{id:long}/calendar")]
        public async Task<IActionResult> Calendar(long id, CalendarUpdateRequest req)
        {
            await LayXeCuaToi(id);
            var ngay = (req.Days ?? new List<DateOnly>()).Distinct().ToList();
            if (ngay.Count == 0) return Ok();

            var dangCo = await _db.CarAvailabilities
                .Where(a => a.CarId == id && ngay.Contains(a.Day))
                .ToListAsync();

            if (req.Blocked)
            {
                // Ngày đã có đơn giữ thì không khoá đè lên được, phải huỷ đơn trước.
                var vuongDon = dangCo.Where(a => a.BookingId != null).Select(a => a.Day).ToList();
                if (vuongDon.Count > 0)
                    throw new BizException("SLOT_TAKEN",
                        "Những ngày này đang có đơn: " + string.Join(", ", vuongDon));

                var daKhoa = dangCo.Select(a => a.Day).ToHashSet();
                foreach (var d in ngay.Where(d => !daKhoa.Contains(d)))
                    _db.CarAvailabilities.Add(new CarAvailability { CarId = id, Day = d });
            }
            else
            {
                // Chỉ mở lại ngày do chủ xe tự khoá, không đụng vào ngày đơn đang giữ.
                _db.CarAvailabilities.RemoveRange(dangCo.Where(a => a.BookingId == null));
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        private void GanAnhVaGiayTo(Car xe, CarUpsertRequest req)
        {
            var anh = req.PhotoUrls ?? new List<string>();
            for (var i = 0; i < anh.Count; i++)
                xe.Photos.Add(new CarPhoto { Url = anh[i], SortOrder = i });

            foreach (var g in req.Documents ?? new List<CarDocumentRequest>())
                xe.Documents.Add(new CarDocument { Type = g.Type, Url = g.Url, ExpiryDate = g.ExpiryDate });
        }

        private async Task<Car> LayXeCuaToi(long id)
        {
            var xe = await _db.Cars
                .Include(c => c.Photos)
                .Include(c => c.Documents)
                .FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new BizException("CAR_UNAVAILABLE", "Không tìm thấy xe");

            if (xe.OwnerId != UserId)
                throw new BizException("FORBIDDEN", "Xe này không phải của bạn");

            return xe;
        }
    }
}
