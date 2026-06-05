using Kursserver.Models;
using Microsoft.EntityFrameworkCore;

namespace Kursserver.Utils
{
    public class ApplicationDbContext : DbContext
    {

        public DbSet<User> Users { get; set; }

        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Permission> Permissions { get; set; }

        public DbSet<NoClass> NoClasses { get; set; }

        public DbSet<BugReport> BugReports { get; set; }

        public DbSet<AdminAvailability> AdminAvailabilities { get; set; }
        public DbSet<Booking> Bookings { get; set; }

        public DbSet<RecurringEvent> RecurringEvents { get; set; }
        public DbSet<RecurringEventException> RecurringEventExceptions { get; set; }
        public DbSet<BusyTime> BusyTimes { get; set; }
        public DbSet<SeatingAssignment> SeatingAssignments { get; set; }
        public DbSet<Computer> Computers { get; set; }
        public DbSet<ComputerAssignment> ComputerAssignments { get; set; }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasOne(i => i.Coach)
                .WithMany()
                .HasForeignKey(i => i.CoachId)
                .OnDelete(DeleteBehavior.NoAction);


            modelBuilder.Entity<Permission>()
                .HasOne(i => i.User)
                .WithOne()
                .HasForeignKey<Permission>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<Attendance>()
                .HasOne(i => i.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // BugReport relationships
            modelBuilder.Entity<BugReport>()
                .HasOne(b => b.Sender)
                .WithMany()
                .HasForeignKey(b => b.SenderId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<AdminAvailability>()
                .HasOne(a => a.Admin)
                .WithMany()
                .HasForeignKey(a => a.AdminId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Admin)
                .WithMany()
                .HasForeignKey(b => b.AdminId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Coach)
                .WithMany()
                .HasForeignKey(b => b.CoachId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Student)
                .WithMany()
                .HasForeignKey(b => b.StudentId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Booking>()
                .Property(b => b.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Booking>()
                .Property(b => b.MeetingType)
                .HasConversion<string>();

            modelBuilder.Entity<Booking>()
                .Property(b => b.CreatedByRole)
                .HasConversion<string>();

            modelBuilder.Entity<Booking>()
                .Property(b => b.RescheduledBy)
                .HasConversion<string>();

            modelBuilder.Entity<RecurringEvent>()
                .Property(r => r.Frequency)
                .HasConversion<string>();

            modelBuilder.Entity<RecurringEvent>()
                .HasOne(r => r.Admin)
                .WithMany()
                .HasForeignKey(r => r.AdminId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<BusyTime>()
                .HasOne(b => b.Admin)
                .WithMany()
                .HasForeignKey(b => b.AdminId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SeatingAssignment>()
                .HasOne(s => s.Student)
                .WithMany()
                .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SeatingAssignment>()
                .HasIndex(s => new { s.ClassroomId, s.DayOfWeek, s.Period, s.Row, s.Column })
                .IsUnique();

            modelBuilder.Entity<Computer>()
                .HasIndex(c => c.Number)
                .IsUnique();

            modelBuilder.Entity<Computer>()
                .HasOne(c => c.OwnerStudent)
                .WithMany()
                .HasForeignKey(c => c.OwnerStudentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ComputerAssignment>()
                .HasOne(a => a.Computer)
                .WithMany()
                .HasForeignKey(a => a.ComputerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ComputerAssignment>()
                .HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ComputerAssignment>()
                .HasIndex(a => new { a.ComputerId, a.DayOfWeek, a.Period })
                .IsUnique();

            modelBuilder.Entity<RecurringEventException>()
                .HasOne(e => e.RecurringEvent)
                .WithMany()
                .HasForeignKey(e => e.RecurringEventId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
