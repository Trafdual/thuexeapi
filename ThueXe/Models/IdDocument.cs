namespace ThueXe.Models
{
    public class IdDocument
    {
        public long Id { get; set; }

        public long UserId { get; set; }

        public string? CccdNo { get; set; }

        public string? GplxNo { get; set; }

        public string? GplxClass { get; set; }

        public DateOnly? GplxExpiry { get; set; }

        public string FrontUrl { get; set; } = null!;

        public string BackUrl { get; set; } = null!;

        public string? SelfieUrl { get; set; }

        public string Status { get; set; } = "CHO_DUYET";

        public string? RejectReason { get; set; }

        public long? ReviewedBy { get; set; }

        public DateTimeOffset? ReviewedAt { get; set; }

        public AppUser User { get; set; } = null!;
    }
}