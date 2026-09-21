using System.Runtime.ConstrainedExecution;

namespace ThueXe.Models
{
    public class AppUser
    {
        public long Id { get; set; }

        public string Phone { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public string FullName { get; set; } = null!;

        public string? Email { get; set; }

        public bool IsOwner { get; set; }

        public string Status { get; set; } = "HOAT_DONG";

        public DateTimeOffset CreatedAt { get; set; }
            = DateTimeOffset.UtcNow;

        public ICollection<IdDocument> Documents { get; set; }
            = new List<IdDocument>();

        public ICollection<Car> Cars { get; set; }
            = new List<Car>();
        public ICollection<Booking> Bookings { get; set; }
    }
}