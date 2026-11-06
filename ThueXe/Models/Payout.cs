namespace ThueXe.Models
{
    public class Payout
    {
        public long Id { get; set; }

        public long BookingId { get; set; }

        public string PayeeType { get; set; } = null!;

        public long PayeeId { get; set; }

        public string BankAccount { get; set; } = null!;

        public string BankName { get; set; } = null!;

        public long Amount { get; set; }

        public string Status { get; set; } = "CHO";

        public string? TransferRef { get; set; }

        /// Lý do khi người vận hành đánh dấu LOI — tài khoản sai, ngân hàng từ chối… Sửa xong
        /// thì vẫn gọi lại /paid bình thường, không cần lệnh chi mới.
        public string? FailNote { get; set; }

        public long? PaidBy { get; set; }

        public DateTimeOffset? PaidAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
            = DateTimeOffset.UtcNow;

        public Booking Booking { get; set; } = null!;
    }
}