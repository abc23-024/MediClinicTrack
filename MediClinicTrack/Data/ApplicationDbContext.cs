using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MediClinicTrack.Models;

namespace MediClinicTrack.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Patient> Patients { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Your Patient Table Rules
            builder.Entity<Patient>()
                .HasIndex(p => new { p.IdType, p.IdNumber })
                .IsUnique();

            builder.Entity<Patient>()
                .HasOne(p => p.ApplicationUser)
                .WithOne()
                .HasForeignKey<Patient>(p => p.ApplicationUserId)
                // Deleting a user must not silently delete their patient record (US08-3:
                // accounts with history are deactivated, not deleted).
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}