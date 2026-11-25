using System;
using System.ComponentModel.DataAnnotations;
using MediClinicTrack.Data; // This connects it to ApplicationUser

namespace MediClinicTrack.Models
{
    public class Patient
    {
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        public string IdType { get; set; }

        [Required]
        public string IdNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Phone]
        public string MobileNumber { get; set; }

        // One-to-one relationship with ApplicationUser
        public string ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }
    }
}