using ThueXe.Helpers;

namespace ThueXe.Controllers
{
    [ApiController]
    [Route("owner/agreement")]
    [Authorize]
    public class OwnerAgreementController : ControllerBase
    {
        // Bản điều khoản hiện hành. Đổi nội dung thì tăng số bản, chủ xe phải ký lại bản mới.
        private const string BanHienHanh = "1.0";

        private const string NoiDung = """
            CAM KẾT CHỦ XE — bản 1.0

            1. Xe đăng lên sàn phải thuộc quyền sở hữu của bạn, hoặc có giấy uỷ quyền hợp lệ.
            Tên trên đăng ký xe khác CCCD mà không có uỷ quyền thì sàn từ chối duyệt.

            2. Đăng kiểm và bảo hiểm phải còn hiệu lực. Còn 7 ngày là sàn tự ẩn tin xe.

            3. Nhận đơn trong 30 phút. Quá hạn im lặng, đơn tự huỷ và điểm phản hồi của bạn bị hạ.

            4. Huỷ đơn sau khi khách đã chuyển tiền sẽ bị ghi vi phạm; tái phạm thì bị khoá đăng tin.

            5. Sàn giữ toàn bộ tiền thuê và tiền cọc cho tới khi trả xe xong, thu hoa hồng 15%
            tiền thuê, chuyển phần còn lại về tài khoản bạn khai dưới đây trong 24 giờ sau khi
            chốt đơn.

            6. Bạn phải lập biên bản giao xe có đủ 6 ảnh và số ODO trước khi giao chìa khoá.
            Không có biên bản thì mọi tranh chấp về sau sàn không bảo vệ được bạn.
            """;

        private readonly ApplicationDbContext _db;

        public OwnerAgreementController(ApplicationDbContext db) => _db = db;

        private long UserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET /owner/agreement — bản điều khoản hiện hành + đã ký hay chưa
        [HttpGet]
        public async Task<ActionResult<AgreementInfoDto>> Get()
        {
            var daKy = await _db.OwnerAgreements
                .FirstOrDefaultAsync(a => a.OwnerId == UserId && a.Version == BanHienHanh);

            return new AgreementInfoDto(BanHienHanh, NoiDung, daKy is not null, daKy?.AcceptedAt);
        }

        // POST /owner/agreement/accept — ký cam kết
        [HttpPost("accept")]
        public async Task<ActionResult<AgreementInfoDto>> Accept(AcceptAgreementRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.CccdNo) ||
                string.IsNullOrWhiteSpace(req.BankAccount) ||
                string.IsNullOrWhiteSpace(req.BankName) ||
                string.IsNullOrWhiteSpace(req.SignatureUrl))
                throw new BizException("INVALID_INPUT", "Thiếu CCCD, tài khoản ngân hàng hoặc chữ ký");

            var cu = await _db.OwnerAgreements
                .FirstOrDefaultAsync(a => a.OwnerId == UserId && a.Version == BanHienHanh);

            if (cu is not null)
                return new AgreementInfoDto(BanHienHanh, NoiDung, true, cu.AcceptedAt);

            var ban = new OwnerAgreement
            {
                OwnerId = UserId,
                Version = BanHienHanh,
                CccdNo = req.CccdNo,
                BankAccount = req.BankAccount,
                BankName = req.BankName,
                SignatureUrl = req.SignatureUrl,
                Ip = HttpContext.Connection.RemoteIpAddress?.ToString(),
                AcceptedAt = DateTimeOffset.UtcNow
            };
            _db.OwnerAgreements.Add(ban);

            // Ký cam kết là chính thức thành chủ xe.
            var user = await _db.AppUsers.FindAsync(UserId);
            if (user is not null) user.IsOwner = true;

            await _db.SaveChangesAsync();
            return new AgreementInfoDto(BanHienHanh, NoiDung, true, ban.AcceptedAt);
        }
    }
}
