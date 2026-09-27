using ThueXe.Helpers;

namespace ThueXe.Controllers
{
    [ApiController]
    [Route("admin/cars")]
    [Authorize(Roles = Vai.VanHanh)]
    public class AdminCarsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        private readonly ThongBaoService _tb;
        private readonly FileStorageService _files;

        public AdminCarsController(ApplicationDbContext db, ThongBaoService tb, FileStorageService files)
        {
            _db = db;
            _tb = tb;
            _files = files;
        }

        // GET /admin/cars/{id}/document/{docId} — ảnh giấy tờ xe, chỉ người vận hành đã đăng nhập.
        [HttpGet("{id:long}/document/{docId:long}")]
        public async Task<IActionResult> AnhGiayTo(long id, long docId)
        {
            var d = await _db.CarDocuments.FirstOrDefaultAsync(x => x.Id == docId && x.CarId == id)
                    ?? throw new BizException("NOT_FOUND", "Không tìm thấy giấy tờ");
            var duongDan = _files.DuongDanGiayTo(d.Url)
                           ?? throw new BizException("NOT_FOUND", "Không có ảnh này");
            var loai = Path.GetExtension(duongDan).ToLowerInvariant() switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };
            Response.Headers.CacheControl = "no-store, private";
            return PhysicalFile(duongDan, loai);
        }

        // GET /admin/cars?status=CHO_DUYET — hàng chờ duyệt xe
        [HttpGet]
        public async Task<ActionResult<List<CarAdminDto>>> Queue([FromQuery] string? status)
        {
            var q = _db.Cars
                .Include(c => c.Photos)
                .Include(c => c.Documents)
                .Include(c => c.Owner)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(c => c.Status == status);

            var xe = await q.OrderBy(c => c.Id).ToListAsync();

            // A5: người vận hành cần CCCD chủ xe để đối chiếu với tên trên đăng ký xe.
            // Lấy một lượt cho cả danh sách, tránh gọi CSDL theo từng dòng.
            var chuXeId = xe.Select(c => c.OwnerId).Distinct().ToList();
            var cccd = await _db.IdDocuments
                .Where(d => chuXeId.Contains(d.UserId) && d.CccdNo != null)
                .GroupBy(d => d.UserId)
                .Select(g => new { g.Key, Cccd = g.OrderByDescending(x => x.Id).First().CccdNo })
                .ToDictionaryAsync(x => x.Key, x => x.Cccd);

            return xe.Select(c => c.ToAdminDto(c.Owner, cccd.GetValueOrDefault(c.OwnerId))).ToList();
        }

        // POST /admin/cars/{id}/review — { approved, reason }
        [HttpPost("{id:long}/review")]
        public async Task<ActionResult<CarAdminDto>> Review(long id, ReviewRequest req)
        {
            if (!req.Approved && string.IsNullOrWhiteSpace(req.Reason))
                throw new BizException("INVALID_INPUT", "Từ chối thì phải ghi lý do");

            var xe = await _db.Cars
                .Include(c => c.Photos)
                .Include(c => c.Documents)
                .Include(c => c.Owner)
                .FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new BizException("CAR_UNAVAILABLE", "Không tìm thấy xe");

            if (xe.Status != TrangThaiXe.ChoDuyet)
                throw new BizException("WRONG_STATE", $"Xe đang ở {xe.Status}");

            // Duyệt đạt thì xe bắt đầu hiện trong kết quả tìm kiếm; từ chối thì ẩn đi.
            xe.Status = req.Approved ? TrangThaiXe.DangBan : TrangThaiXe.An;
            xe.RejectReason = req.Approved ? null : req.Reason;

            await _db.SaveChangesAsync();

            await _tb.Gui(xe.OwnerId,
                req.Approved ? "Xe đã được duyệt" : "Xe chưa được duyệt",
                req.Approved ? $"{xe.Brand} {xe.Model} đã hiện trên sàn cho người thuê."
                             : $"{xe.Brand} {xe.Model}: {req.Reason}");

            var cccd = await _db.IdDocuments
                .Where(d => d.UserId == xe.OwnerId && d.CccdNo != null)
                .OrderByDescending(d => d.Id)
                .Select(d => d.CccdNo)
                .FirstOrDefaultAsync();

            return xe.ToAdminDto(xe.Owner, cccd);
        }
    }
}
