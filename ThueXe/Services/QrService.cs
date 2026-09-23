namespace ThueXe.Services
{
    /// <summary>
    /// Sinh URL ảnh mã QR động qua img.vietqr.io. Không cần thư viện, không cần khoá API —
    /// chỉ là một chuỗi URL, app nạp bằng Coil, web nạp bằng thẻ img.
    /// </summary>
    public class QrService
    {
        private readonly IConfiguration _cfg;

        public QrService(IConfiguration cfg) => _cfg = cfg;

        /// Số tiền là tiền thuê CỘNG tiền cọc — một lần chuyển duy nhất cho cả chuyến.
        /// Số tiền và nội dung nằm sẵn trong mã nên khách không phải gõ gì.
        public string TaoUrl(string maDon, long soTien)
        {
            var bank = _cfg.GetSection("Bank");
            return "https://img.vietqr.io/image/"
                 + $"{bank["Id"]}-{bank["Account"]}-compact2.png"
                 + $"?amount={soTien}"
                 + $"&addInfo={Uri.EscapeDataString(maDon)}"
                 + $"&accountName={Uri.EscapeDataString(bank["Name"] ?? "")}";
        }
    }
}
