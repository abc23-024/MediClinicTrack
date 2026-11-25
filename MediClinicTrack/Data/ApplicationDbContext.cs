using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MediClinicTrack.Models; // Patient o koo

namespace MediClinicTrack.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        
        public DbSet<Patient> Patients { get; set; }

        //Patient table rules: IdType + IdNumber must be unique, one-to-one relationship with ApplicationUser
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            
            builder.Entity<Patient>()
                .HasIndex(p => new { p.IdType, p.IdNumber })
                .IsUnique();

            // Rule: One-to-one relationship
            builder.Entity<Patient>()
                .HasOne(p => p.ApplicationUser)
                .WithOne()
                .HasForeignKey<Patient>(p => p.ApplicationUserId);
        }
    }
}
