namespace ThueXe.Common
{
    /// <summary>
    /// Việt Nam không đổi giờ mùa, lệch UTC cố định +7 — dùng offset thẳng, khỏi phụ thuộc
    /// TimeZoneInfo của máy chủ (Windows và Linux đặt tên múi giờ khác nhau, máy chủ container
    /// thường mặc định UTC).
    ///
    /// Chỉ dùng chỗ nào một mốc UTC bị đem so với DateOnly (ngày lịch khách nhìn thấy):
    /// "hôm nay", "còn 7 ngày nữa", "hạn chót ngày X". Log kỹ thuật hay cột CreatedAt thì cứ để
    /// UtcNow — DateTimeOffset đã mang đúng thời điểm bất kể hiển thị theo offset nào.
    /// </summary>
    public static class GioVN
    {
        private static readonly TimeSpan Lech = TimeSpan.FromHours(7);

        public static DateTimeOffset BayGio => DateTimeOffset.UtcNow.ToOffset(Lech);

        public static DateOnly HomNay => DateOnly.FromDateTime(BayGio.DateTime);

        /// Mốc DateTimeOffset đúng 00:00 giờ Việt Nam của một ngày lịch — dùng để tính hạn chót
        /// kiểu "hết ngày trả xe", tức nửa đêm bước sang ngày hôm sau.
        public static DateTimeOffset LucNuaDem(DateOnly ngay) =>
            new(ngay.ToDateTime(TimeOnly.MinValue), Lech);
    }
}
