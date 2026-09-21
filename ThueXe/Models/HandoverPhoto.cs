namespace ThueXe.Models
{
    public class HandoverPhoto
    {
        public long Id { get; set; }

        public long HandoverId { get; set; }

        public string Slot { get; set; } = null!;

        public string Url { get; set; } = null!;

        public string TakenBy { get; set; } = null!;

        public string? Note { get; set; }

        public Handover Handover { get; set; } = null!;
    }
}