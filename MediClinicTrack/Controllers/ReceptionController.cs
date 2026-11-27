using MediClinicTrack.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediClinicTrack.Controllers;

// US06: Reception. Every action here inherits the Reception policy (see Security/Policies.cs).
[Authorize(Policy = Policies.Reception)]
public class ReceptionController : Controller
{
    public IActionResult Index() => View();
}
