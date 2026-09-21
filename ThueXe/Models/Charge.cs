namespace ThueXe.Models
{
    public class Charge
    {
        public long Id { get; set; }

        public long BookingId { get; set; }

        public string Type { get; set; } = null!;

        public long Amount { get; set; }

        public string? Note { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
            = DateTimeOffset.UtcNow;

        public Booking Booking { get; set; } = null!;
    }
}