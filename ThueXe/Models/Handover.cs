namespace ThueXe.Models
{
    public class Handover
    {
        public long Id { get; set; }

        public long BookingId { get; set; }

        public string Kind { get; set; } = null!;

        public string CreatedBy { get; set; } = null!;

        public int Odo { get; set; }

        public int FuelLevel { get; set; }

        public string? Note { get; set; }

        public string Status { get; set; } = "CHO_SOI";

        public string SignCreator { get; set; } = null!;

        public string? SignReviewer { get; set; }

        public string? Objection { get; set; }

        public DateTimeOffset? ReviewedAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
            = DateTimeOffset.UtcNow;

        public Booking Booking { get; set; } = null!;

        public ICollection<HandoverPhoto> Photos { get; set; }
            = new List<HandoverPhoto>();
    }
}