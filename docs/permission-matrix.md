# Permission matrix (US06)

Who can open each part of MediClinicTrack. The code version is
[`Security/Policies.cs`](../MediClinicTrack/Security/Policies.cs), and
[`RoleAccessTests`](../MediClinicTrack.Tests/RoleAccessTests.cs) checks every row against every role.
Change all three together.

✅ = allowed. ⛔ = refused with HTTP 403 (and logged). 🔑 = sent to the login page.

| Screen | Route | Policy | Visitor (not signed in) | Patient | Receptionist | Clinician | Manager | Admin |
|---|---|---|---|---|---|---|---|---|
| Home, Privacy, error pages | `/`, `/Home/*` | public | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Login, Register, Forgot and Reset password, Logout | `/Identity/Account/*` | public | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Change password and other account settings | `/Identity/Account/Manage/*` | signed in | 🔑 | ✅ | ✅ | ✅ | ✅ | ✅ |
| My health portal | `/PatientPortal` | `PatientPortal` | 🔑 | ✅ | ⛔ | ⛔ | ⛔ | ⛔ |
| Reception (check-in, appointments) | `/Reception` | `Reception` | 🔑 | ⛔ | ✅ | ⛔ | ⛔ | ⛔ |
| Clinical (patient records) | `/Clinical` | `Clinical` | 🔑 | ⛔ | ⛔ | ✅ | ⛔ | ⛔ |
| Reports | `/Reports` | `Reports` | 🔑 | ⛔ | ⛔ | ⛔ | ✅ | ⛔ |
| Administration (staff accounts US08, audit log US13) | `/Admin` | `Administration` | 🔑 | ⛔ | ⛔ | ⛔ | ⛔ | ✅ |
| CSS, JavaScript, images | `/css`, `/js`, `/lib` | public | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

Each role gets only its own area (least privilege). In particular, Admin manages accounts but
can't read clinical records, and Manager sees reports but not individual patient records. If
the team wants a role to reach another area, add the role to that policy in `Policies.cs` and
update this table and the test.

## Rules for new screens

- **Put new screens in the controller for their area.** They inherit its policy automatically.
- **A new controller needs `[Authorize(Policy = Policies.X)]`.** If you forget, the page still
  requires a signed-in user (deny by default), and `Matrix_CoversEveryControllerAction` fails
  until you add a row to the matrix and the test.
- **Only mark something `[AllowAnonymous]` if a visitor really needs it** before signing in.
- **Hiding a link isn't protection.** The nav bar hides areas a user can't open, but the server
  check is what keeps them out.

## How a refusal works

1. A signed-in user requests a route outside their role.
2. `AccessDeniedLoggingHandler` records the user ID, their roles, the method, the path and the
   roles required, through `IAccessDeniedLog`. It records no email and no query string.
   For now that goes to the application log (event ID 4030). US13 replaces it with the audit table.
3. The response is HTTP 403 with an "Access denied" page.

A role change applies at the user's next request, without signing in again (US06-5), because the
user is re-checked against the database on every request.
