using System.Text.RegularExpressions;

namespace ThueXe.Helpers
{
    /// <summary>
    /// Đường dẫn công khai dùng MÃ ĐƠN thay cho id chạy số. Id chạy số cho người ngoài
    /// đếm được sàn có bao nhiêu đơn và tăng bao nhiêu mỗi ngày; mã đơn 31^6 ≈ 887 triệu
    /// tổ hợp thì không dò nổi.
    ///
    /// Vẫn nhận id số để bộ Postman và nhóm web đang chạy dở không gãy ngang.
    /// </summary>
    public static class MaDon
    {
        private static readonly Regex Mau =
            new(@"^KNM[ABCDEFGHJKMNPQRSTUVWXYZ23456789]{6}$", RegexOptions.Compiled);

        public static bool LaMa(string khoa) => Mau.IsMatch(khoa.ToUpperInvariant());

        /// Dò mã đơn trong nội dung chuyển khoản. Ngân hàng hay viết hoa và cắt bớt
        /// nội dung nên phải tìm theo mẫu, không so bằng. Dùng chung cho webhook và
        /// cho màn xác nhận tay — hai đường phải khớp cùng một luật.
        private static readonly Regex MauTrongCau =
            new(@"KNM[ABCDEFGHJKMNPQRSTUVWXYZ23456789]{6}", RegexOptions.Compiled);

        public static string? TimTrong(string? noiDung)
        {
            if (string.IsNullOrWhiteSpace(noiDung)) return null;
            var m = MauTrongCau.Match(noiDung.ToUpperInvariant());
            return m.Success ? m.Value : null;
        }

        /// Lọc theo mã nếu khoá đúng dạng mã, ngược lại theo id. Dùng trong LINQ nên
        /// phải viết thành biểu thức EF dịch được.
        public static IQueryable<Models.Booking> TheoKhoa(this IQueryable<Models.Booking> q, string khoa)
        {
            if (LaMa(khoa))
            {
                var ma = khoa.ToUpperInvariant();
                return q.Where(b => b.Code == ma);
            }
            return long.TryParse(khoa, out var id)
                ? q.Where(b => b.Id == id)
                : q.Where(b => false);
        }
    }
}
