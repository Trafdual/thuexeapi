namespace ThueXe.Models
{
    public class OwnerAgreement
    {
        public long Id { get; set; }

        public long OwnerId { get; set; }

        public string Version { get; set; } = "1.0";

        public string CccdNo { get; set; } = null!;

        public string BankAccount { get; set; } = null!;

        public string BankName { get; set; } = null!;

        public string SignatureUrl { get; set; } = null!;

        public string? Ip { get; set; }

        public DateTimeOffset AcceptedAt { get; set; }
            = DateTimeOffset.UtcNow;

        public AppUser Owner { get; set; } = null!;
    }
}