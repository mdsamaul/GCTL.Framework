using GCTL.Data.Identity;
using GCTL.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace GCTL.Data.Context;

/// <summary>
/// Identity-backed DbContext. This handles Sign In / Sign Up / Roles.
///
/// HOW TO ADD YOUR REVERSE-ENGINEERED TABLES:
/// When you run EF Core Power Tools ("Reverse Engineer" from your SQL Server
/// database), point it at THIS project (GCTL.Data) and it will generate:
///   - Entities/*.cs           (one class per table)
///   - A new DbContext, e.g. GctlHrmContext.cs
///
/// You have two clean options:
///   1. Keep two DbContexts (recommended, least error-prone): this
///      ApplicationDbContext for Identity/auth, and the scaffolded
///      GctlHrmContext for your business tables. Register both in
///      Program.cs (already done in this template).
///   2. Merge: move the generated DbSet&lt;T&gt; properties and OnModelCreating
///      configuration into this class if you want a single context.
///
/// Option 1 is simpler to keep in sync every time you re-scaffold after a
/// database schema change, since re-running Power Tools won't touch this file.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
       
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Keep Identity's default table names as AspNetUsers/AspNetRoles/etc.
        // If you prefer names consistent with your existing schema (e.g. "Users",
        // "Roles"), rename them here, for example:
        // builder.Entity<ApplicationUser>().ToTable("Users");
        // builder.Entity<ApplicationRole>().ToTable("Roles");
    }
}
