using Microsoft.AspNetCore.Identity;
using System;

namespace MediClinicTrack.Data
{
    public class ApplicationUser : IdentityUser
    {
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // True for the seeded admin: they must set their own password before using the app.
        public bool MustChangePassword { get; set; }
    }
}