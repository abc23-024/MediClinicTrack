using MediClinicTrack.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediClinicTrack.Controllers;

// US06: Clinical. Every action here inherits the Clinical policy (see Security/Policies.cs).
[Authorize(Policy = Policies.Clinical)]
public class ClinicalController : Controller
{
    public IActionResult Index() => View();
}
