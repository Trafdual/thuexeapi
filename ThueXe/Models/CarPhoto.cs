namespace ThueXe.Models
{
    public class CarPhoto
    {
        public long Id { get; set; }

        public long CarId { get; set; }

        public string Url { get; set; } = null!;

        public int SortOrder { get; set; }

        public Car Car { get; set; } = null!;
    }
}