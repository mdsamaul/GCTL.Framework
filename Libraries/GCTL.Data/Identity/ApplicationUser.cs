using Microsoft.AspNetCore.Identity;

namespace GCTL.Data.Identity;

/// <summary>
/// Extends the built-in Identity user with fields that map to your
/// existing AccessCode / Employee-linked login pattern. Add more
/// properties here as needed once you scaffold your real Employee table.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }

    /// <summary>
    /// Mirrors your existing AccessCode column (0001 Admin, 0002/0003 Team Leader, 0005 User).
    /// Kept alongside ASP.NET Core Identity's Role system so your existing
    /// row-level filtering logic in stored procedures/services keeps working unchanged.
    /// </summary>
    public string? AccessCode { get; set; }

    /// <summary>
    /// Optional FK to your real Employee table once scaffolded via EF Power Tools.
    /// Left as a plain nullable int/string here; adjust the type to match
    /// your actual EmployeeId column after reverse engineering.
    /// </summary>
    public string? EmployeeId { get; set; }
    //public string? CompanyCode { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Extends IdentityRole in case you want extra metadata per role later.
/// </summary>
public class ApplicationRole : IdentityRole
{
    public string? Description { get; set; }
}
