using System;
using System.ComponentModel.DataAnnotations;
using MediClinicTrack.Data; // This connects it to ApplicationUser

namespace MediClinicTrack.Models
{
    public class Patient
    {
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string IdType { get; set; } = string.Empty;

        [Required]
        public string IdNumber { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Phone]
        public string? MobileNumber { get; set; }

        // One-to-one relationship with ApplicationUser
        public string ApplicationUserId { get; set; } = string.Empty;
        public ApplicationUser ApplicationUser { get; set; } = null!;
    }
}