using Microsoft.EntityFrameworkCore;
using GCTL.Data.Models;

namespace GCTL.Data.Context;

public class GctlHrmContext : GCTL_ERP_DBContext
{
    public GctlHrmContext(DbContextOptions<GCTL_ERP_DBContext> options)
        : base(options)
    {
    }
}
