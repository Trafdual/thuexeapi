namespace ThueXe.Models
{
    /// Báo sự cố giữa chuyến (tai nạn, hỏng hóc, va quẹt…) — chỉ là bằng chứng ghi lại tại lúc
    /// xảy ra, KHÔNG tự đổi trạng thái đơn. Đa số việc nhỏ để dành tính phí lúc quyết toán;
    /// việc nghiêm trọng thì bên liên quan tự mở tranh chấp qua BookingsController.OpenDispute.
    public class Incident
    {
        public long Id { get; set; }

        public long BookingId { get; set; }

        public string ReportedBy { get; set; } = null!;

        /// VA_CHAM | KHAC — xem Common.LoaiSuCo. VA_CHAM thì thông báo gửi vận hành có kèm
        /// gợi ý xem giấy bảo hiểm TNDS của xe để hỗ trợ chủ xe làm bồi thường.
        public string Type { get; set; } = LoaiSuCo.Khac;

        public string Note { get; set; } = null!;

        public DateTimeOffset CreatedAt { get; set; }
            = DateTimeOffset.UtcNow;

        public Booking Booking { get; set; } = null!;

        public ICollection<IncidentPhoto> Photos { get; set; }
            = new List<IncidentPhoto>();
    }

    public class IncidentPhoto
    {
        public long Id { get; set; }

        public long IncidentId { get; set; }

        public string Url { get; set; } = null!;

        public Incident Incident { get; set; } = null!;
    }
}
