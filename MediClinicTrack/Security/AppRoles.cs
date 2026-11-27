namespace MediClinicTrack.Security;

// US06: the five roles. Use these constants instead of typing role names as strings.
public static class AppRoles
{
    public const string Patient = "Patient";
    public const string Receptionist = "Receptionist";
    public const string Clinician = "Clinician";
    public const string Manager = "Manager";
    public const string Admin = "Admin";

    public static readonly string[] All = { Patient, Receptionist, Clinician, Manager, Admin };
}
