using MediClinicTrack.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediClinicTrack.Controllers;

// US06: Administration. Every action here inherits the Administration policy (see Security/Policies.cs).
[Authorize(Policy = Policies.Administration)]
public class AdminController : Controller
{
    public IActionResult Index() => View();
}
