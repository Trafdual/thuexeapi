namespace ThueXe.Models
{
    public class CarAvailability
    {
        public long CarId { get; set; }

        public DateOnly Day { get; set; }

        public long? BookingId { get; set; }

        public Car Car { get; set; } = null!;
    }
}