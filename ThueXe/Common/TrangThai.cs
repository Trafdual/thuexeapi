namespace ThueXe.Common
{
    /// Các chuỗi trạng thái dùng chung. Gõ tay ở mỗi controller là sớm muộn lệch một ký tự.
    public static class TrangThaiDon
    {
        public const string ChoChuXe = "CHO_CHU_XE";
        public const string ChoThanhToan = "CHO_THANH_TOAN";
        public const string DaXacNhan = "DA_XAC_NHAN";
        public const string DangThue = "DANG_THUE";
        public const string ChoQuyetToan = "CHO_QUYET_TOAN";
        public const string ChoChiTra = "CHO_CHI_TRA";
        public const string HoanTat = "HOAN_TAT";
        public const string BiTuChoi = "BI_TU_CHOI";
        public const string HetHan = "HET_HAN";
        public const string DaHuy = "DA_HUY";

        public static readonly string[] DaDong =
            { HoanTat, BiTuChoi, HetHan, DaHuy };
    }

    public static class TrangThaiXe
    {
        public const string Nhap = "NHAP";
        public const string ChoDuyet = "CHO_DUYET";
        public const string DangBan = "DANG_BAN";
        public const string An = "AN";
    }

    public static class LoaiBienBan
    {
        public const string Giao = "GIAO";
        public const string Tra = "TRA";
    }

    public static class TrangThaiBienBan
    {
        public const string ChoSoi = "CHO_SOI";
        public const string DaKy = "DA_KY";
        public const string PhanDoi = "PHAN_DOI";
    }

    public static class Ben
    {
        public const string ChuXe = "CHU_XE";
        public const string Khach = "KHACH";
    }

    public static class KhungAnh
    {
        public static readonly string[] BatBuoc =
            { "TRUOC", "SAU", "TRAI", "PHAI", "TAPLO", "ODO" };
    }
}

namespace ThueXe.Common
{
    public static class Vai
    {
        public const string NguoiDung = "NGUOI_DUNG";
        public const string VanHanh = "VAN_HANH";
    }

    public static class TrangThaiGiayTo
    {
        public const string ChuaNop = "CHUA_NOP";
        public const string ChoDuyet = "CHO_DUYET";
        public const string Dat = "DAT";
        public const string TuChoi = "TU_CHOI";
    }

    public static class TrangThaiThanhToan
    {
        public const string Cho = "CHO";
        public const string DaNhan = "DA_NHAN";
        public const string HetHan = "HET_HAN";
    }

    public static class TrangThaiChiTra
    {
        public const string Cho = "CHO";
        public const string DaChi = "DA_CHI";
        public const string Loi = "LOI";
    }

    /// Bốn tài khoản của sổ cái. Số dư là tổng bút toán, không lưu thành cột.
    public static class TaiKhoanSoCai
    {
        public const string Khach = "KHACH";
        public const string ChuXe = "CHU_XE";
        public const string San = "SAN";
        public const string ViTreo = "VI_TREO";
    }

    public static class Chieu
    {
        public const string No = "NO";
        public const string Co = "CO";
    }

    public static class LoaiChungTu
    {
        public const string ThanhToan = "PAYMENT";
        public const string ChiTra = "PAYOUT";
        public const string Phi = "CHARGE";
    }

    public static class LoaiPhi
    {
        public const string QuaGio = "QUA_GIO";
        public const string QuaKm = "QUA_KM";
        public const string NhienLieu = "NHIEN_LIEU";
    }
}

namespace ThueXe.Common
{
    public static class ChiTra
    {
        /// Sàn chưa thu số tài khoản của khách. Người vận hành phải hỏi trước khi chuyển.
        /// Đây là lỗ hổng đã biết của lược đồ: bảng booking không có chỗ lưu tài khoản khách.
        public const string ChuaCoSoTaiKhoan = "CHUA_CO";
    }
}
