using ThueXe.Helpers;

namespace ThueXe.Controllers
{
    // Khoá tay: gian lận, quấy rối, hoặc nợ quá hạn không chịu trả. Việc chặn ĐẶT ĐƠN MỚI khi
    // còn nợ đã tự động rồi (xem BookingsController.Create) — khoá ở đây là để người vận hành
    // chặn hẳn một tài khoản vì lý do khác con số, không tự phát hiện được bằng luật.
    [ApiController]
    [Route("admin/users")]
    [Authorize(Roles = Vai.VanHanh)]
    public class AdminUsersController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public AdminUsersController(ApplicationDbContext db) => _db = db;

        // POST /admin/users/{id}/lock
        [HttpPost("{id:long}/lock")]
        public async Task<ActionResult<NguoiDungTomTat>> Lock(long id, LockUserRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Reason))
                throw new BizException("INVALID_INPUT", "Phải ghi rõ lý do khoá");

            var u = await _db.AppUsers.FindAsync(id)
                ?? throw new BizException("NOT_FOUND", "Không tìm thấy người dùng");
            u.IsLocked = true;
            u.LockReason = req.Reason;
            await _db.SaveChangesAsync();
            return u.ToTomTat();
        }

        // POST /admin/users/{id}/unlock
        [HttpPost("{id:long}/unlock")]
        public async Task<ActionResult<NguoiDungTomTat>> Unlock(long id)
        {
            var u = await _db.AppUsers.FindAsync(id)
                ?? throw new BizException("NOT_FOUND", "Không tìm thấy người dùng");
            u.IsLocked = false;
            u.LockReason = null;
            await _db.SaveChangesAsync();
            return u.ToTomTat();
        }
    }
}
