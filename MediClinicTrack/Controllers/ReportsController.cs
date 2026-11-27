using MediClinicTrack.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediClinicTrack.Controllers;

// US06: Reports. Every action here inherits the Reports policy (see Security/Policies.cs).
[Authorize(Policy = Policies.Reports)]
public class ReportsController : Controller
{
    public IActionResult Index() => View();
}
