using Kursserver.Models;
using Microsoft.EntityFrameworkCore;

namespace Kursserver.Utils
{
    public class ApplicationDbContext : DbContext
    {

        public DbSet<User> Users { get; set; }
        public DbSet<Post> Posts { get; set; }

        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Permission> Permissions { get; set; }

        public DbSet<Project> Projects { get; set; }
        public DbSet<Exercise> Exercises { get; set; }

        public DbSet<NoClass> NoClasses { get; set; }

        public DbSet<ExerciseHistory> ExerciseHistories { get; set; }
        public DbSet<ProjectHistory> ProjectHistories { get; set; }

        public DbSet<Models.Thread> Threads { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Models.ThreadView> ThreadViews { get; set; }
        public DbSet<BugReport> BugReports { get; set; }

        public DbSet<AdminAvailability> AdminAvailabilities { get; set; }
        public DbSet<Booking> Bookings { get; set; }

        public DbSet<RecurringEvent> RecurringEvents { get; set; }
        public DbSet<RecurringEventException> RecurringEventExceptions { get; set; }
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


            modelBuilder.Entity<Post>()
                .HasOne(i => i.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Attendance>()
                .HasOne(i => i.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExerciseHistory>()
                .HasOne(i => i.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExerciseHistory>()
                .HasIndex(e => new { e.UserId, e.Topic, e.Language });

            modelBuilder.Entity<ProjectHistory>()
                .HasOne(i => i.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectHistory>()
                .HasIndex(p => new { p.UserId, p.TechStack });

            // Thread relationships
            modelBuilder.Entity<Models.Thread>()
                .HasOne(t => t.User1)
                .WithMany()
                .HasForeignKey(t => t.User1Id)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Models.Thread>()
                .HasOne(t => t.User2)
                .WithMany()
                .HasForeignKey(t => t.User2Id)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Models.Thread>()
                .HasOne(t => t.StudentContext)
                .WithMany()
                .HasForeignKey(t => t.StudentContextId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Models.Thread>()
                .Property<int>("StudentContextIdForUnique")
                .HasComputedColumnSql("COALESCE(StudentContextId, 0)");

            modelBuilder.Entity<Models.Thread>()
                .HasIndex("User1Id", "User2Id", "StudentContextIdForUnique")
                .IsUnique();

            // Message relationships
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Thread)
                .WithMany()
                .HasForeignKey(m => m.ThreadId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.NoAction);

            // ThreadView relationships
            modelBuilder.Entity<Models.ThreadView>()
                .HasOne(v => v.Thread)
                .WithMany()
                .HasForeignKey(v => v.ThreadId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.ThreadView>()
                .HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.ThreadView>()
                .HasIndex(v => new { v.UserId, v.ThreadId })
                .IsUnique();

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
                .HasOne(b => b.AdminAvailability)
                .WithMany()
                .HasForeignKey(b => b.AdminAvailabilityId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<RecurringEvent>()
                .HasOne(r => r.Admin)
                .WithMany()
                .HasForeignKey(r => r.AdminId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<RecurringEventException>()
                .HasOne(e => e.RecurringEvent)
                .WithMany()
                .HasForeignKey(e => e.RecurringEventId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
