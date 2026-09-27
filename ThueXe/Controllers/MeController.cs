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

        private readonly FileStorageService _files;

        public MeController(ApplicationDbContext db, FileStorageService files)
        {
            _db = db;
            _files = files;
        }

        private long UserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public record DeviceTokenRequest(string Token, string? Platform);

        // POST /me/device-token — app đăng ký mã FCM sau khi đăng nhập
        [HttpPost("device-token")]
        public async Task<IActionResult> DangKyThietBi(DeviceTokenRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Token))
                throw new BizException("INVALID_INPUT", "Thiếu token");

            // Một máy chỉ thuộc về một người: đăng nhập tài khoản khác trên cùng máy thì chuyển chủ.
            // Hai lần gọi cùng lúc cùng một token thì lần chèn thứ hai đụng chỉ mục duy nhất:
            // coi như đã đăng ký, không phải lỗi.
            for (var lan = 0; lan < 2; lan++)
            {
                var dong = await _db.DeviceTokens.FirstOrDefaultAsync(d => d.Token == req.Token);
                if (dong is null)
                    _db.DeviceTokens.Add(new DeviceToken
                    {
                        UserId = UserId, Token = req.Token,
                        Platform = req.Platform ?? "android", UpdatedAt = DateTimeOffset.UtcNow
                    });
                else
                {
                    dong.UserId = UserId;
                    dong.UpdatedAt = DateTimeOffset.UtcNow;
                }
                try
                {
                    await _db.SaveChangesAsync();
                    break;
                }
                catch (DbUpdateException) when (lan == 0)
                {
                    _db.ChangeTracker.Clear();
                }
            }
            return Ok(new { registered = true });
        }

        // DELETE /me/device-token — gọi khi đăng xuất để máy này thôi nhận thông báo của tài khoản
        [HttpDelete("device-token")]
        public async Task<IActionResult> HuyThietBi([FromQuery] string token)
        {
            var dong = await _db.DeviceTokens.FirstOrDefaultAsync(d => d.Token == token && d.UserId == UserId);
            if (dong is not null)
            {
                _db.DeviceTokens.Remove(dong);
                await _db.SaveChangesAsync();
            }
            return Ok(new { registered = false });
        }

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
                user.IsOwner, user.Status, doc?.Status ?? "CHUA_NOP", user.BankAccount, user.BankName);
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
                // Giấy tờ tuỳ thân chuyển sang kho riêng, chỉ người vận hành đã đăng nhập mới xem được.
                FrontUrl = _files.ChuyenSangRiengTu(req.FrontUrl),
                BackUrl = _files.ChuyenSangRiengTu(req.BackUrl),
                SelfieUrl = string.IsNullOrWhiteSpace(req.SelfieUrl) ? null : _files.ChuyenSangRiengTu(req.SelfieUrl),
                Status = "CHO_DUYET"
            };
            _db.IdDocuments.Add(doc);
            await _db.SaveChangesAsync();

            return Ok(new { id = doc.Id, status = doc.Status });
        }
    }
}
