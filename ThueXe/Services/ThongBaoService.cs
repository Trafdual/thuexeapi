using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using ThueXe.Data;

namespace ThueXe.Services
{
    /// Khởi tạo Firebase một lần. Thiếu tệp khoá dịch vụ thì tắt hẳn việc gửi (chỉ ghi log),
    /// để máy nào chưa cấu hình vẫn chạy được backend bình thường.
    public class FcmSender
    {
        private readonly FirebaseMessaging? _fcm;
        private readonly ILogger<FcmSender> _log;

        public bool DangBat => _fcm is not null;

        public FcmSender(IConfiguration cfg, IWebHostEnvironment env, ILogger<FcmSender> log)
        {
            _log = log;
            var duongDan = cfg["Firebase:ServiceAccountPath"] ?? "firebase-service-account.json";
            if (!Path.IsPathRooted(duongDan)) duongDan = Path.Combine(env.ContentRootPath, duongDan);
            if (!File.Exists(duongDan))
            {
                _log.LogWarning("FCM tắt: không thấy {Path}. Tải khoá dịch vụ từ Firebase Console rồi đặt vào đó.", duongDan);
                return;
            }
            try
            {
                var app = FirebaseApp.DefaultInstance ?? FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(duongDan)
                });
                _fcm = FirebaseMessaging.GetMessaging(app);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Không khởi tạo được Firebase");
            }
        }

        /// Trả về danh sách token không còn hợp lệ để dọn khỏi CSDL.
        public async Task<List<string>> Gui(List<string> tokens, string tieuDe, string noiDung,
                                            Dictionary<string, string> data)
        {
            var hong = new List<string>();
            if (_fcm is null || tokens.Count == 0) return hong;

            var tinNhan = tokens.Select(t => new Message
            {
                Token = t,
                Notification = new Notification { Title = tieuDe, Body = noiDung },
                Data = data,
                Android = new AndroidConfig
                {
                    Priority = Priority.High,
                    Notification = new AndroidNotification { ChannelId = "thongbao" }
                }
            }).ToList();

            var kq = await _fcm.SendEachAsync(tinNhan);
            for (var i = 0; i < kq.Responses.Count; i++)
            {
                var r = kq.Responses[i];
                if (r.IsSuccess) continue;
                var ma = r.Exception?.MessagingErrorCode;
                _log.LogWarning("FCM từ chối token …{Duoi}: {Ma} — {Err}",
                    tokens[i][^8..], ma, r.Exception?.Message);
                if (ma is MessagingErrorCode.Unregistered or MessagingErrorCode.InvalidArgument)
                    hong.Add(tokens[i]);
                else
                    _log.LogWarning("FCM lỗi: {Err}", r.Exception?.Message);
            }
            return hong;
        }
    }

    /// Gửi thông báo đẩy cho một người dùng theo id. Lỗi gửi không bao giờ làm hỏng
    /// nghiệp vụ đang chạy: nuốt lỗi và ghi log.
    public class ThongBaoService
    {
        private readonly ApplicationDbContext _db;
        private readonly FcmSender _fcm;
        private readonly ILogger<ThongBaoService> _log;

        public ThongBaoService(ApplicationDbContext db, FcmSender fcm, ILogger<ThongBaoService> log)
        {
            _db = db;
            _fcm = fcm;
            _log = log;
        }

        /// <param name="donCode">Mã đơn để app mở đúng màn khi bấm vào thông báo (có thể null).</param>
        public async Task Gui(long userId, string tieuDe, string noiDung, string? donCode = null)
        {
            try
            {
                if (!_fcm.DangBat) return;
                var tokens = await _db.DeviceTokens.Where(d => d.UserId == userId)
                    .Select(d => d.Token).ToListAsync();
                if (tokens.Count == 0) return;

                var data = new Dictionary<string, string>();
                if (donCode is not null) data["bookingCode"] = donCode;

                var hong = await _fcm.Gui(tokens, tieuDe, noiDung, data);
                if (hong.Count > 0)
                {
                    _db.DeviceTokens.RemoveRange(_db.DeviceTokens.Where(d => hong.Contains(d.Token)));
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Gửi thông báo cho user {User} thất bại", userId);
            }
        }
    }
}
