namespace ThueXe.Models
{
    public class Payment
    {
        public long Id { get; set; }

        public long BookingId { get; set; }

        public long Amount { get; set; }

        public string TransferCode { get; set; } = null!;

        public string QrUrl { get; set; } = null!;

        public string Status { get; set; } = "CHO";

        public long? ReceivedAmount { get; set; }

        public long? ConfirmedBy { get; set; }

        public DateTimeOffset? ConfirmedAt { get; set; }

        public string? BankNote { get; set; }

        public Booking Booking { get; set; } = null!;
    }
}