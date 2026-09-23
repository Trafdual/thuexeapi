namespace ThueXe.Services
{
    /// Kết quả tính phí. Tiền là số nguyên đồng, không bao giờ là số thực.
    public record PhiPhatSinh(long QuaGio, long QuaKm, long NhienLieu)
    {
        public long Tong => QuaGio + QuaKm + NhienLieu;
    }

    /// <summary>
    /// Hàm thuần, KHÔNG đụng CSDL — để viết unit test được ngay cả khi backend chưa chạy.
    /// Mọi tham số truyền vào, không đọc gì từ ngoài.
    /// </summary>
    public static class PhiPhatSinhService
    {
        private const long TienMotGioTre = 100_000;
        private const int SoGioTreThanhMotNgay = 5;
        private const long TienMotKmVuot = 5_000;
        private const long TienMotNacNhienLieu = 120_000;
        private const long PhiDoHo = 100_000;

        public static PhiPhatSinh Tinh(
            long giaMotNgay, int soNgay, int gioiHanKmMotNgay,
            int odoLucGiao, int odoLucTra,
            int nhienLieuLucGiao, int nhienLieuLucTra,
            int soGioTraMuon)
        {
            // Quá 5 tiếng thì tính trọn thêm một ngày, không cộng dồn từng giờ nữa.
            long quaGio = soGioTraMuon <= 0
                ? 0
                : soGioTraMuon >= SoGioTreThanhMotNgay
                    ? giaMotNgay
                    : soGioTraMuon * TienMotGioTre;

            var gioiHan = (long)gioiHanKmMotNgay * soNgay;
            var daChay = Math.Max(0, odoLucTra - odoLucGiao);
            var kmVuot = Math.Max(0, daChay - gioiHan);
            var quaKm = kmVuot * TienMotKmVuot;

            // Thang 8 nấc. Trả đầy hơn lúc giao thì không hoàn tiền, chỉ không tính phí.
            var nacThieu = Math.Max(0, nhienLieuLucGiao - nhienLieuLucTra);
            long nhienLieu = nacThieu == 0
                ? 0
                : nacThieu * TienMotNacNhienLieu + PhiDoHo;

            return new PhiPhatSinh(quaGio, quaKm, nhienLieu);
        }
    }
}
