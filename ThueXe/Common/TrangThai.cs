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
