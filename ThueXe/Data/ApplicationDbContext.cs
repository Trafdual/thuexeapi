using Microsoft.EntityFrameworkCore;
using ThueXe.Models;

namespace ThueXe.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<IdDocument> IdDocuments { get; set; }
        public DbSet<OwnerAgreement> OwnerAgreements { get; set; }

        public DbSet<Car> Cars { get; set; }
        public DbSet<CarPhoto> CarPhotos { get; set; }
        public DbSet<CarDocument> CarDocuments { get; set; }
        public DbSet<CarAvailability> CarAvailabilities { get; set; }

        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Payout> Payouts { get; set; }
        public DbSet<LedgerEntry> LedgerEntries { get; set; }

        public DbSet<Handover> Handovers { get; set; }
        public DbSet<HandoverPhoto> HandoverPhotos { get; set; }
        public DbSet<Charge> Charges { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================
            // UNIQUE
            // =========================

            modelBuilder.Entity<AppUser>()
                .HasIndex(x => x.Phone)
                .IsUnique();

            modelBuilder.Entity<Car>()
                .HasIndex(x => x.Plate)
                .IsUnique();

            modelBuilder.Entity<CarAvailability>()
                .HasKey(x => new
                {
                    x.CarId,
                    x.Day
                });

            modelBuilder.Entity<OwnerAgreement>()
                .HasIndex(x => new
                {
                    x.OwnerId,
                    x.Version
                })
                .IsUnique();

            modelBuilder.Entity<Handover>()
                .HasIndex(x => new
                {
                    x.BookingId,
                    x.Kind
                })
                .IsUnique();

            modelBuilder.Entity<Booking>()
                .HasIndex(x => x.Code)
                .IsUnique();


            // =========================
            // BOOKING
            // =========================

            modelBuilder.Entity<Booking>()
                .ToTable("booking", tb =>
                    tb.HasCheckConstraint(
                        "CK_booking_dates",
                        "[EndDate] > [StartDate]"));


            // =========================
            // LEDGER
            // =========================

            modelBuilder.Entity<LedgerEntry>()
                .ToTable("ledger_entry", tb =>
                    tb.HasCheckConstraint(
                        "CK_ledger_amount",
                        "[Amount] > 0"));


            // =========================
            // FOREIGN KEY
            // KHÔNG CASCADE
            // =========================

            // Car -> Owner
            modelBuilder.Entity<Car>()
                .HasOne(x => x.Owner)
                .WithMany(x => x.Cars)
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.NoAction);


            // Booking -> Car
            modelBuilder.Entity<Booking>()
                .HasOne(x => x.Car)
                .WithMany(x => x.Bookings)
                .HasForeignKey(x => x.CarId)
                .OnDelete(DeleteBehavior.NoAction);


            // Booking -> Renter
            modelBuilder.Entity<Booking>()
                .HasOne(x => x.Renter)
                .WithMany(x => x.Bookings)
                .HasForeignKey(x => x.RenterId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}