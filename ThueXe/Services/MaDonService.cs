namespace ThueXe.Services
{
    /// <summary>
    /// Sinh mã đơn. Khách phải gõ mã này vào nội dung chuyển khoản nên bỏ hết ký tự
    /// dễ nhầm khi nhìn: 0 O 1 I L. Còn 31 ký tự.
    /// </summary>
    public static class MaDonService
    {
        private const string BangChu = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        private const int SoKyTu = 6;

        public static string Sinh()
        {
            var sb = new System.Text.StringBuilder("KNM");
            for (var i = 0; i < SoKyTu; i++)
                sb.Append(BangChu[Random.Shared.Next(BangChu.Length)]);
            return sb.ToString();
        }
    }
}
