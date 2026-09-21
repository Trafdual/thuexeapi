namespace ThueXe.Models
{
    public class LedgerEntry
    {
        public long Id { get; set; }

        public long BookingId { get; set; }

        public string Account { get; set; } = null!;

        public string Direction { get; set; } = null!;

        public long Amount { get; set; }

        public string RefType { get; set; } = null!;

        public long RefId { get; set; }

        public DateTimeOffset OccurredAt { get; set; }
            = DateTimeOffset.UtcNow;

        public Booking Booking { get; set; } = null!;
    }
}