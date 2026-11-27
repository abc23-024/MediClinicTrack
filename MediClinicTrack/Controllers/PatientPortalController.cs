using MediClinicTrack.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediClinicTrack.Controllers;

// US06: My health portal. Every action here inherits the PatientPortal policy (see Security/Policies.cs).
[Authorize(Policy = Policies.PatientPortal)]
public class PatientPortalController : Controller
{
    public IActionResult Index() => View();
}
