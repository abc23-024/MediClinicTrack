#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using MediClinicTrack.Data;
using MediClinicTrack.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MediClinicTrack.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _context;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        // Options for the ID type dropdown (confirm the exact values with the team)
        public List<SelectListItem> IdTypes { get; } = new()
        {
            new SelectListItem("National ID", "National ID"),
            new SelectListItem("Passport", "Passport")
        };

        public class InputModel
        {
            [Required(ErrorMessage = "Enter your full name.")]
            [StringLength(100, ErrorMessage = "Full name must be 100 characters or fewer.")]
            [Display(Name = "Full name")]
            public string FullName { get; set; }

            [Required(ErrorMessage = "Choose an ID type.")]
            [Display(Name = "ID type")]
            public string IdType { get; set; }

            [Required(ErrorMessage = "Enter your ID number.")]
            [StringLength(30, ErrorMessage = "ID number must be 30 characters or fewer.")]
            [Display(Name = "ID number")]
            public string IdNumber { get; set; }

            // Nullable so that an empty field is caught by [Required]
            [Required(ErrorMessage = "Enter your date of birth.")]
            [DataType(DataType.Date)]
            [ValidDateOfBirth]
            [Display(Name = "Date of birth")]
            public DateTime? DateOfBirth { get; set; }

            [RegularExpression(@"^\+?[0-9\s]{7,15}$",
                ErrorMessage = "Enter a valid mobile number, for example 71234567 or +267 71234567.")]
            [Display(Name = "Mobile number")]
            public string MobileNumber { get; set; }

            [Required(ErrorMessage = "Enter your email address.")]
            [EmailAddress(ErrorMessage = "Enter a valid email address.")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Enter a password.")]
            [StringLength(100, ErrorMessage = "The password must be at least {2} characters long.", MinimumLength = 10)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var idType = Input.IdType;
                var idNumber = Input.IdNumber.Trim().ToUpperInvariant();

                // Friendly duplicate check, done BEFORE creating the user
                var alreadyRegistered = await _context.Patients
                    .AnyAsync(p => p.IdType == idType && p.IdNumber == idNumber);
                if (alreadyRegistered)
                {
                    ModelState.AddModelError("Input.IdNumber",
                        "A patient is already registered with this ID type and number.");
                    return Page();
                }

                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    // Save the patient details linked to the new user
                    var patient = new Patient
                    {
                        FullName = Input.FullName.Trim(),
                        IdType = idType,
                        IdNumber = idNumber,
                        DateOfBirth = DateOnly.FromDateTime(Input.DateOfBirth.Value),
                        MobileNumber = string.IsNullOrWhiteSpace(Input.MobileNumber)
                            ? null
                            : Input.MobileNumber.Trim(),
                        ApplicationUserId = user.Id
                    };

                    try
                    {
                        _context.Patients.Add(patient);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException ex)
                    {
                        // Don't leave a user account with no patient record behind
                        _logger.LogError(ex, "Could not save patient details; removing the new user.");
                        _context.Entry(patient).State = EntityState.Detached;
                        await _userManager.DeleteAsync(user);
                        ModelState.AddModelError(string.Empty,
                            "We could not complete your registration. Please try again.");
                        return Page();
                    }

                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor.");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }

    // Rejects future dates and dates before 1900
    public class ValidDateOfBirthAttribute : ValidationAttribute
    {
        public ValidDateOfBirthAttribute()
            : base("Enter a valid date of birth that is not in the future.") { }

        public override bool IsValid(object value) =>
            value is not DateTime d || (d.Date <= DateTime.Today && d.Year >= 1900);
    }
}