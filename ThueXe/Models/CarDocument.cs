namespace ThueXe.Models
{
    public class CarDocument
    {
        public long Id { get; set; }

        public long CarId { get; set; }

        public string Type { get; set; } = null!;

        public string Url { get; set; } = null!;

        public DateOnly? ExpiryDate { get; set; }

        public Car Car { get; set; } = null!;
    }
}