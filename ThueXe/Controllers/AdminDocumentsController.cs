using ThueXe.Helpers;

namespace ThueXe.Controllers
{
    [ApiController]
    [Route("admin/documents")]
    [Authorize(Roles = Vai.VanHanh)]
    public class AdminDocumentsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public AdminDocumentsController(ApplicationDbContext db) => _db = db;

        private long UserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET /admin/documents?status=CHO_DUYET — hàng chờ duyệt giấy tờ
        [HttpGet]
        public async Task<ActionResult<List<IdDocumentAdminDto>>> Queue([FromQuery] string? status)
        {
            var q = _db.IdDocuments.Include(d => d.User).AsQueryable();
            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(d => d.Status == status);

            var ds = await q.OrderBy(d => d.Id).ToListAsync();
            return ds.Select(d => d.ToAdminDto(d.User)).ToList();
        }

        // POST /admin/documents/{id}/review — { approved, reason }
        [HttpPost("{id:long}/review")]
        public async Task<ActionResult<ReviewResult>> Review(long id, ReviewRequest req)
        {
            // A2: từ chối thì phải ghi lý do, để khách biết sửa gì mà nộp lại.
            if (!req.Approved && string.IsNullOrWhiteSpace(req.Reason))
                throw new BizException("INVALID_INPUT", "Từ chối thì phải ghi lý do");

            var giayTo = await _db.IdDocuments.FindAsync(id)
                         ?? throw new BizException("NOT_FOUND", "Không tìm thấy giấy tờ");

            if (giayTo.Status != TrangThaiGiayTo.ChoDuyet)
                throw new BizException("WRONG_STATE", $"Giấy tờ đang ở {giayTo.Status}");

            giayTo.Status = req.Approved ? TrangThaiGiayTo.Dat : TrangThaiGiayTo.TuChoi;
            giayTo.RejectReason = req.Approved ? null : req.Reason;
            giayTo.ReviewedBy = UserId;
            giayTo.ReviewedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync();
            return new ReviewResult(giayTo.Id, giayTo.Status, giayTo.RejectReason, giayTo.ReviewedAt);
        }
    }
}
