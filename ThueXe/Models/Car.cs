namespace ThueXe.Models
{
    public class Car
    {
        public long Id { get; set; }

        public long OwnerId { get; set; }

        public string Plate { get; set; } = null!;

        public string Brand { get; set; } = null!;

        public string Model { get; set; } = null!;

        public int Year { get; set; }

        public int Seats { get; set; }

        public string Transmission { get; set; } = null!;

        public string Fuel { get; set; } = null!;

        public int Odo { get; set; }

        public string District { get; set; } = null!;

        public string PickupAddress { get; set; } = null!;

        public long PricePerDay { get; set; }

        public int MaxKmDay { get; set; } = 300;

        public long Deposit { get; set; } = 3_000_000;

        public string? Description { get; set; }

        public string Status { get; set; } = "NHAP";

        public string? RejectReason { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
            = DateTimeOffset.UtcNow;

        public AppUser Owner { get; set; } = null!;

        public ICollection<CarPhoto> Photos { get; set; }
            = new List<CarPhoto>();

        public ICollection<CarDocument> Documents { get; set; }
            = new List<CarDocument>();

        public ICollection<CarAvailability> Availability { get; set; }
            = new List<CarAvailability>();
        public ICollection<Booking> Bookings { get; set; }
    }
}