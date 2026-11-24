using Microsoft.AspNetCore.Identity;

namespace MediClinicTrack.Data;

public class ApplicationUser : IdentityUser
{
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

