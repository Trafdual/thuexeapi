using ThueXe.Common;
using ThueXe.Data;
using ThueXe.Dtos;
using ThueXe.Models;

namespace ThueXe.Controllers
{
    [ApiController]
    [Route("me")]
    [Authorize]
    public class MeController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public MeController(ApplicationDbContext db) => _db = db;

        private long UserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET /me — hồ sơ + trạng thái duyệt giấy tờ
        [HttpGet]
        public async Task<ActionResult<MeResponse>> Get()
        {
            var user = await _db.AppUsers.FindAsync(UserId) ?? throw new BizException("USER_NOT_FOUND");
            var doc = await _db.IdDocuments
                .Where(d => d.UserId == UserId)
                .OrderByDescending(d => d.Id)
                .FirstOrDefaultAsync();

            return new MeResponse(user.Id, user.Phone, user.FullName, user.Email,
                user.IsOwner, user.Status, doc?.Status ?? "CHUA_NOP");
        }

        // POST /me/documents — nộp CCCD + GPLX, chuyển sang chờ duyệt
        /// PUT /me/bank-account — nơi nhận tiền hoàn cọc.
        /// Chủ xe đã khai trong bản cam kết; khách thì trước đây không có chỗ nào khai,
        /// nên lệnh hoàn phải ghi CHUA_CO rồi người vận hành đi hỏi từng người.
        [HttpPut("bank-account")]
        public async Task<ActionResult<MeResponse>> CapNhatTaiKhoan(CapNhatTaiKhoanRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.BankAccount) || string.IsNullOrWhiteSpace(req.BankName))
                throw new BizException("INVALID_INPUT", "Phải có cả số tài khoản và tên ngân hàng");

            var nguoi = await _db.AppUsers.FindAsync(UserId)
                        ?? throw new BizException("NOT_FOUND", "Không tìm thấy người dùng");

            nguoi.BankAccount = req.BankAccount.Trim();
            nguoi.BankName = req.BankName.Trim();
            await _db.SaveChangesAsync();
            return await Get();
        }

        [HttpPost("documents")]
        public async Task<IActionResult> SubmitDocuments(SubmitDocumentsRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.FrontUrl) || string.IsNullOrWhiteSpace(req.BackUrl))
                throw new BizException("INVALID_INPUT", "Thiếu ảnh mặt trước hoặc mặt sau");

            var doc = new IdDocument
            {
                UserId = UserId,
                CccdNo = req.CccdNo,
                GplxNo = req.GplxNo,
                GplxClass = req.GplxClass,
                GplxExpiry = req.GplxExpiry,
                FrontUrl = req.FrontUrl,
                BackUrl = req.BackUrl,
                SelfieUrl = req.SelfieUrl,
                Status = "CHO_DUYET"
            };
            _db.IdDocuments.Add(doc);
            await _db.SaveChangesAsync();

            return Ok(new { id = doc.Id, status = doc.Status });
        }
    }
}
