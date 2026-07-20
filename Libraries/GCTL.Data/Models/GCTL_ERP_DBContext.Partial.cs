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

            modelBuilder.Entity<HrmDefBloodGroup>(entity =>
            {
                entity.Property(p => p.AutoId)
               .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });


            modelBuilder.Entity<CaDefCurrency>(entity =>
            {
                entity.Property(p => p.Tc)
               .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });

            modelBuilder.Entity<HrmAtdCompanyWeekEnd>(entity =>
            {
                entity.Property(p => p.AutoId)
               .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });

            modelBuilder.Entity<CoreBranch>(entity =>
            {
                entity.Property(p => p.AutoId)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });

            modelBuilder.Entity<RmgDefSupplier>(entity =>
            {
                entity.Property(p => p.Tc)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });

            modelBuilder.Entity<SalesSupplierBankAccountTemp>(entity =>
            {
                entity.Property(p => p.AutoId)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });

            modelBuilder.Entity<InvDefSupplierCategory>(entity =>
            {
                entity.Property(p => p.SupplierCategoryCode)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });

            modelBuilder.Entity<InvDefSupplierOrigin>(entity =>
            {
                entity.Property(p => p.Tc)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });

            modelBuilder.Entity<InvDefSupplierType>(entity =>
            {
                entity.Property(p => p.SupplierTypeCode)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });

            modelBuilder.Entity<HrmDefNationality>(entity =>
            {
                entity.Property(p => p.AutoId)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });

            modelBuilder.Entity<SalesDefPaymentType>(entity =>
            {
                entity.Property(p => p.Tc)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });

            modelBuilder.Entity<CaDefCountry>(entity =>
            {
                entity.Property(p => p.Tc)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });

            modelBuilder.Entity<HrmAtdCompanyWeekEnd>(entity =>
            {
                entity.Property(p => p.AutoId)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });

            modelBuilder.Entity<SalesDefBankInfo>(entity =>
            {
                entity.Property(p => p.AutoId)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });

            modelBuilder.Entity<SalesDefBankBranchInfo>(entity =>
            {
                entity.Property(p => p.AutoId)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });

            modelBuilder.Entity<CoreBankAccountInformation>(entity =>
            {
                entity.Property(p => p.AutoId)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });

            modelBuilder.Entity<SalesDefPortType>(entity =>
            {
                entity.Property(p => p.Tc)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });

            modelBuilder.Entity<SalesDefPortName>(entity =>
            {
                entity.Property(p => p.Tc)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });

            modelBuilder.Entity<SalesDefVehicle>(entity =>
            {
                entity.Property(p => p.Tc)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });

            modelBuilder.Entity<SalesDefVehicleType>(entity =>
            {
                entity.Property(p => p.Tc)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });

            modelBuilder.Entity<CoreImporterInformation>(entity =>
            {
                entity.Property(p => p.Tc)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });

            modelBuilder.Entity<TransportModeType>(entity =>
            {
                entity.Property(p => p.Tc)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });

            modelBuilder.Entity<CoreExporterInformation>(entity =>
            {
                entity.Property(p => p.Tc)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });

            modelBuilder.Entity<SalesContactPerson>(entity =>
            {
                entity.Property(p => p.AutoId)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });

            modelBuilder.Entity<CoreVendorInformation>(entity =>
            {
                entity.Property(p => p.Tc)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });
        }
    }
}
