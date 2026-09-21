namespace ThueXe.Controllers
{
    /// NGOÀI §04 — bảng API của kế hoạch không có GET /news.
    /// App chủ xe đang gọi để dựng màn Tin tức. Nhóm chốt lại: hoặc bổ sung vào docs/api.md
    /// rồi làm bảng news trong CSDL, hoặc bỏ màn Tin tức và xoá tệp này.
    [ApiController]
    [Route("news")]
    [AllowAnonymous]
    public class NewsController : ControllerBase
    {
        // Nội dung cắm cứng cho tới khi có bảng thật. Không đụng CSDL.
        private static readonly NewsItemDto[] Tin =
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

        [HttpGet]
        public ActionResult<IEnumerable<NewsItemDto>> Get()
        {
            var homNay = DateOnly.FromDateTime(DateTime.UtcNow);
            return Ok(Tin.Select((t, i) => t with { PublishedAt = homNay.AddDays(-(i * 3 + 1)) }));
        }
    }
}
