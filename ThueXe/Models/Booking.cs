using System.Runtime.InteropServices;

namespace ThueXe.Models
{
    public class Booking
    {
        public long Id { get; set; }

        public string Code { get; set; } = null!;

        public long CarId { get; set; }

        public long RenterId { get; set; }

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public int Days { get; set; }

        public long PricePerDay { get; set; }

        public long RentTotal { get; set; }

        public long Deposit { get; set; }

        public long Commission { get; set; }

        public string Status { get; set; } = null!;

        public DateTimeOffset? HoldExpiresAt { get; set; }

        public string? CancelReason { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
            = DateTimeOffset.UtcNow;

        public Car Car { get; set; } = null!;

        public AppUser Renter { get; set; } = null!;

        public ICollection<Payment> Payments { get; set; }
            = new List<Payment>();

        public ICollection<Handover> Handovers { get; set; }
            = new List<Handover>();

        public ICollection<Charge> Charges { get; set; }
            = new List<Charge>();
    }
}