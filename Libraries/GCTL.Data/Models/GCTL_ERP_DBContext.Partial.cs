using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GCTL.Data.Models
{
    public partial class GCTL_ERP_DBContext
    {
        partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HrmDefDesignation>(entity =>
            {
                entity.Property(e => e.AutoId)
                    .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });

            modelBuilder.Entity<HrmDefDepartment>(entity =>
            {
                entity.Property(e => e.AutoId)
                    .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });

            modelBuilder.Entity<HrmDefReligion>(entity =>
            {
                entity.Property(e => e.AutoId)
                    .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });
        }
    }
}
