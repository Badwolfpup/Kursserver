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

        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TicketReply> TicketReplies { get; set; }
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

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Sender)
                .WithMany()
                .HasForeignKey(t => t.SenderId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Recipient)
                .WithMany()
                .HasForeignKey(t => t.RecipientId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<TicketReply>()
                .HasOne(r => r.Ticket)
                .WithMany()
                .HasForeignKey(r => r.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TicketReply>()
                .HasOne(r => r.Sender)
                .WithMany()
                .HasForeignKey(r => r.SenderId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
