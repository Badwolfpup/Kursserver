using Kursserver.Models;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.EntityFrameworkCore;

namespace Kursserver.Utils
{
    public class ApplicationDbContext: DbContext
    {
        //string connectionString = $"Server=localhost;Database=Kurshemsida;User Id=kursDB;Password=Hudiksvall2025!;TrustServerCertificate=True;Encrypt=True";

        public DbSet<User> Users { get; set; }
        public DbSet<Post> Posts { get; set; }

        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Permission> Permissions { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    optionsBuilder.UseSqlServer(connectionString);
        //}

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
                .WithMany(u => u.AttendedDays)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
