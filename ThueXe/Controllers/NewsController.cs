using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.Caching.Memory;

namespace ThueXe.Controllers
{
    /// NGOÀI §04 — bảng API của kế hoạch không có GET /news.
    /// Lấy tin thật từ RSS mục Xe của VnExpress (chỉ tiêu đề, tóm tắt, ảnh, đường dẫn — đúng
    /// mục đích của RSS là để nơi khác đọc lại). App ghi rõ "Nguồn: VnExpress" và mở bài gốc
    /// khi bấm vào, không hiển thị toàn văn bài viết trong app. Có sẵn tin cứng để dùng khi
    /// không có mạng hoặc VnExpress đổi định dạng.
    [ApiController]
    [Route("news")]
    [AllowAnonymous]
    public class NewsController : ControllerBase
    {
        private const string RssUrl = "https://vnexpress.net/rss/oto-xe-may.rss";
        private const string CacheKey = "news:vnexpress";
        private static readonly TimeSpan ThoiGianCache = TimeSpan.FromMinutes(20);

        private readonly IHttpClientFactory _http;
        private readonly IMemoryCache _cache;
        private readonly ILogger<NewsController> _log;

        public NewsController(IHttpClientFactory http, IMemoryCache cache, ILogger<NewsController> log)
        {
            _http = http;
            _cache = cache;
            _log = log;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NewsItemDto>>> Get()
        {
            if (_cache.TryGetValue(CacheKey, out List<NewsItemDto>? cached) && cached is not null)
                return Ok(cached);

            var tin = await LayTuVnExpress() ?? TinDuPhong();
            _cache.Set(CacheKey, tin, ThoiGianCache);
            return Ok(tin);
        }

        private async Task<List<NewsItemDto>?> LayTuVnExpress()
        {
            try
            {
                var client = _http.CreateClient("vnexpress");
                var xml = await client.GetStringAsync(RssUrl);
                var doc = XDocument.Parse(xml);

                var tin = doc.Descendants("item").Select((item, i) =>
                {
                    var tieuDe = item.Element("title")?.Value.Trim() ?? "";
                    var link = item.Element("link")?.Value.Trim();
                    var moTaTho = item.Element("description")?.Value ?? "";

                    // <description> là HTML: một thẻ <a><img></a></br> rồi tới đoạn tóm tắt.
                    var anh = Regex.Match(moTaTho, "<img[^>]*src=\"([^\"]+)\"").Groups[1].Value;
                    var tomTat = Regex.Replace(moTaTho, "<[^>]+>", "").Trim();

                    DateOnly? ngay = DateTimeOffset.TryParse(item.Element("pubDate")?.Value, out var dt)
                        ? DateOnly.FromDateTime(dt.Date) : null;

                    return new NewsItemDto(
                        i + 1, "XE", tieuDe, tomTat,
                        string.IsNullOrWhiteSpace(anh) ? null : anh,
                        ngay, "VnExpress", link);
                })
                .Where(t => !string.IsNullOrWhiteSpace(t.Title))
                .Take(20)
                .ToList();

                return tin.Count > 0 ? tin : null;
            }
            catch (Exception ex)
            {
                // Mất mạng hoặc VnExpress đổi định dạng: dùng tin cứng, không để màn Tin tức trắng trơn.
                _log.LogWarning(ex, "Không lấy được RSS VnExpress, dùng tin dự phòng");
                return null;
            }
        }

        private static List<NewsItemDto> TinDuPhong()
        {
            var homNay = DateOnly.FromDateTime(DateTime.UtcNow);
            NewsItemDto[] tin =
            {
                new(1, "CHÍNH SÁCH", "Từ 01/10: chi trả cho chủ xe rút xuống trong 12 giờ",
                    "Sàn rút hạn chuyển tiền từ 24 giờ còn 12 giờ kể từ lúc chốt đơn. " +
                    "Kiểm tra lại số tài khoản trong phần Hồ sơ.", "/files/mau/tin-1.jpg", null),
                new(2, "HƯỚNG DẪN", "Sáu ảnh biên bản giao xe: chụp thế nào cho đủ bằng chứng",
                    "Tranh chấp hay gặp nhất không phải xe hỏng mà là vết này có từ trước. " +
                    "Chụp đúng sáu khung là hết đường cãi.", "/files/mau/tin-2.jpg", null),
                new(3, "NHẮC VIỆC", "Đăng kiểm còn 30 ngày là sàn bắt đầu nhắc",
                    "Còn 7 ngày mà chưa cập nhật thì tin xe bị ẩn khỏi kết quả tìm kiếm. " +
                    "Nộp lại ảnh đăng kiểm ngay trong màn sửa xe.", "/files/mau/tin-3.jpg", null),
                new(4, "MẸO", "Nhận đơn trong 30 phút để không tụt điểm phản hồi",
                    "Quá 30 phút đơn tự hết hạn, lịch nhả ra và điểm phản hồi bị hạ. " +
                    "Bật thông báo để không lỡ đơn.", "/files/mau/tin-4.jpg", null),
                new(5, "CHÍNH SÁCH", "Khách huỷ dưới 24 giờ: bạn vẫn nhận đủ tiền thuê",
                    "Bảng chính sách hoàn tiền mới: trước 7 ngày hoàn 100%, 3–7 ngày 70%, " +
                    "24–72 giờ 50%, dưới 24 giờ 0% tiền thuê.", "/files/mau/tin-5.jpg", null),
            };
            return tin.Select((t, i) => t with { PublishedAt = homNay.AddDays(-(i * 3 + 1)) }).ToList();
        }
    }
}
