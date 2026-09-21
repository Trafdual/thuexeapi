using ThueXe.Common;
using ThueXe.Data;
using ThueXe.Dtos;
using ThueXe.Models;
using ThueXe.Services;

namespace ThueXe.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly OtpService _otp;
        private readonly JwtService _jwt;

        public AuthController(ApplicationDbContext db, OtpService otp, JwtService jwt)
        {
            _db = db;
            _otp = otp;
            _jwt = jwt;
        }

        // POST /auth/register — tạo tài khoản, gửi OTP (dev: in ra log)
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Phone) || string.IsNullOrWhiteSpace(req.Password))
                throw new BizException("INVALID_INPUT");

            if (await _db.AppUsers.AnyAsync(u => u.Phone == req.Phone))
                throw new BizException("PHONE_TAKEN");

            var user = new AppUser
            {
                Phone = req.Phone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                FullName = req.FullName,
                Email = req.Email,
                IsOwner = false,
                Status = "HOAT_DONG",
                CreatedAt = DateTimeOffset.UtcNow
            };
            _db.AppUsers.Add(user);
            await _db.SaveChangesAsync();

            _otp.Generate(req.Phone);
            return Ok(new { message = "Đã gửi OTP, kiểm tra log ở môi trường dev" });
        }

        // POST /auth/verify-otp — JWT + hồ sơ
        [HttpPost("verify-otp")]
        public async Task<ActionResult<AuthResponse>> VerifyOtp(VerifyOtpRequest req)
        {
            if (!_otp.Verify(req.Phone, req.Otp))
                throw new BizException("OTP_INVALID");

            var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Phone == req.Phone)
                       ?? throw new BizException("USER_NOT_FOUND");

            return Ok(await BuildAuthResponse(user));
        }

        // POST /auth/login — JWT
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
        {
            var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Phone == req.Phone)
                       ?? throw new BizException("LOGIN_FAILED");

            if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
                throw new BizException("LOGIN_FAILED");

            if (user.Status == "KHOA")
                throw new BizException("ACCOUNT_LOCKED");

            return Ok(await BuildAuthResponse(user));
        }

        private async Task<AuthResponse> BuildAuthResponse(AppUser user)
        {
            var doc = await _db.IdDocuments
                .Where(d => d.UserId == user.Id)
                .OrderByDescending(d => d.Id)
                .FirstOrDefaultAsync();

            var profile = new MeResponse(user.Id, user.Phone, user.FullName, user.Email,
                user.IsOwner, user.Status, doc?.Status ?? "CHUA_NOP");

            return new AuthResponse(_jwt.Issue(user), profile);
        }
    }
}
