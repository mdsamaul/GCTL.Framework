using Microsoft.EntityFrameworkCore;

namespace GCTL.Data.Context;

/// <summary>
/// PLACEHOLDER for your reverse-engineered business database context.
///
/// After running EF Core Power Tools ("Reverse Engineer" -> pick your
/// SQL Server connection -> select tables/views/procedures), DELETE this
/// placeholder file entirely; Power Tools will generate a real
/// GctlHrmContext.cs (or whatever name you choose during scaffolding)
/// plus one class per table under Entities/.
///
/// EF Power Tools command-line equivalent, if you prefer the CLI instead
/// of the Visual Studio extension:
///
///   dotnet tool install --global dotnet-ef
///   cd Libraries/GCTL.Data
///   dotnet ef dbcontext scaffold "Server=YOUR_SERVER;Database=YOUR_DB;Trusted_Connection=True;TrustServerCertificate=True;" ^
///       Microsoft.EntityFrameworkCore.SqlServer ^
///       --context GctlHrmContext ^
///       --output-dir Entities ^
///       --context-dir Context ^
///       --namespace GCTL.Data.Entities ^
///       --context-namespace GCTL.Data.Context ^
///       --data-annotations ^
///       --force
///
/// (Replace YOUR_SERVER / YOUR_DB with your real values. On Linux/Mac use
/// plain "\" line continuation is not needed if you put it on one line.)
/// </summary>
public class GctlHrmContext : DbContext
{
    public GctlHrmContext(DbContextOptions<GctlHrmContext> options)
        : base(options)
    {
    }

    // DbSet<YourTable> properties will be generated here by EF Power Tools.
}
