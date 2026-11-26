# MediClinicTrack

Clinic appointment management system (CET3007 group project).

## What you need
- .NET SDK 10.0 (the version in MediClinicTrack.csproj)
- MySQL Community Server 8.4 LTS
- Git
- The dotnet-ef tool: `dotnet tool install --global dotnet-ef`

## Database setup
Run `db/setup.sql` in MySQL. Use your own password in place of YOUR_PASSWORD.

## Connection string
Run these from the MediClinicTrack project folder. Passwords are never committed to Git.

    dotnet user-secrets set "ConnectionStrings:DefaultConnection" "server=localhost;port=3306;database=mediclinictrack;user=mct_app;password=YOUR_PASSWORD"
    dotnet user-secrets set "SeedAdmin:Email" "your-admin-email"
    dotnet user-secrets set "SeedAdmin:Password" "your-admin-password"

## Run it
From the repository folder:

    cd MediClinicTrack
    dotnet ef database update
    dotnet run

## Run the tests
From the repository folder:

    dotnet test

## How we work
- Branches: `feature/US01-registration`, `fix/login-error-message`
- Commit messages start with the story ID, e.g. `US01: add ID type to registration form`
- One branch per task, merged through a pull request with 1 review
- Never push straight to main
- No passwords or connection strings in Git. Use user secrets.
- Pull main before starting each task

## Problems people hit
(Add here as teammates report them.)