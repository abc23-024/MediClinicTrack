using System;
using System.ComponentModel.DataAnnotations;
using MediClinicTrack.Data; // This connects it to ApplicationUser

namespace MediClinicTrack.Models
{
    public class Patient
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string IdType { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string IdNumber { get; set; } = string.Empty;

        // DateOnly maps to a MySQL DATE column, so no time or timezone can shift the day.
        [DataType(DataType.Date)]
        public DateOnly DateOfBirth { get; set; }

        [Phone]
        [StringLength(20)]
        public string? MobileNumber { get; set; }

        // One-to-one relationship with ApplicationUser
        public string ApplicationUserId { get; set; } = string.Empty;
        public ApplicationUser ApplicationUser { get; set; } = null!;
    }
}