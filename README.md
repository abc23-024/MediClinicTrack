# MediClinicTrack

Clinic appointment management system (CET3007 group project).

## What you need
- .NET SDK 9.0 (the version in MediClinicTrack.csproj)
- MySQL Community Server 8.4 LTS
- Git
- The dotnet-ef tool: `dotnet tool install --global dotnet-ef --version 9.0.10`

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
    dotnet ef database
