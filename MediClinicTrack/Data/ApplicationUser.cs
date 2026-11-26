using Microsoft.AspNetCore.Identity;
using System;

namespace MediClinicTrack.Data
{
    public class ApplicationUser : IdentityUser
    {
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}