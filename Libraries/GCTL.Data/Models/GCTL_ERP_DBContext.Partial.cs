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

            modelBuilder.Entity<HrmDefSex>(entity =>
            {
                entity.Property(p => p.AutoId)
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

            modelBuilder.Entity<SalesDefPaymentMode>(entity =>
            {
                entity.Property(p => p.AutoId)
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

            modelBuilder.Entity<VendorBranchInfo>(entity =>
            {
                entity.Property(p => p.Tc)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });

            modelBuilder.Entity<CfDefVendorPrefix>(entity =>
            {
                entity.Property(p => p.Tc)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });
            modelBuilder.Entity<HrmEmployee>(entity =>
            {
                entity.Property(p => p.AutoId)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });
            modelBuilder.Entity<HrmEmployeeContactInfo>(entity =>
            {
                entity.Property(p => p.AutoId)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });
            modelBuilder.Entity<HrmDefRelationship>(entity =>
            {
                entity.Property(p => p.AutoId)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });
            modelBuilder.Entity<HrmDefDistrict>(entity =>
            {
                entity.Property(p => p.AutoId)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });
            modelBuilder.Entity<HrmClientInfo>(entity =>
            {
                entity.Property(p => p.AutoId)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });
            modelBuilder.Entity<SalesDefCustomerType>(entity =>
            {
                entity.Property(p => p.AutoId)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });
            modelBuilder.Entity<HrmDefEmpType>(entity =>
            {
                entity.Property(p => p.AutoId)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            });

            modelBuilder.Entity<CfPaymentReceiveEntry>(entity =>
            {
                entity.HasKey(e => e.Tc);
                entity.ToTable("CF_PaymentReceiveEntry");
                entity.Property(e => e.Tc)
                    .ValueGeneratedOnAdd()
                    .HasColumnType("numeric(18, 0)")
                    .HasColumnName("TC");
                entity.Property(e => e.PaymentReceiveId)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnName("PaymentReceiveID");
                entity.Property(e => e.ReceiveDate).HasColumnType("datetime");
                entity.Property(e => e.CustomerId)
                    .HasMaxLength(50)
                    .HasColumnName("CustomerID");
                entity.Property(e => e.JobNo).HasMaxLength(50);
                entity.Property(e => e.BillNo).HasMaxLength(50);
                entity.Property(e => e.PaymentModeCode).HasMaxLength(50);
                entity.Property(e => e.BankId)
                    .HasMaxLength(50)
                    .HasColumnName("BankID");
                entity.Property(e => e.BranchName).HasMaxLength(150);
                entity.Property(e => e.ChequeNo).HasMaxLength(100);
                entity.Property(e => e.ChequeDate).HasColumnType("datetime");
                entity.Property(e => e.BillAmount).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.ReceivedAmount).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.DueAmount).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.Remarks).HasMaxLength(500);
                entity.Property(e => e.CompanyCode).HasMaxLength(50);
                entity.Property(e => e.EmployeeId)
                    .HasMaxLength(50)
                    .HasColumnName("EmployeeID");
                entity.Property(e => e.Luser)
                    .HasMaxLength(50)
                    .HasColumnName("LUser");
                entity.Property(e => e.Ldate)
                    .HasColumnType("smalldatetime")
                    .HasColumnName("LDate");
                entity.Property(e => e.Lip)
                    .HasMaxLength(50)
                    .HasColumnName("LIP");
                entity.Property(e => e.Lmac)
                    .HasMaxLength(50)
                    .HasColumnName("LMAC");
                entity.Property(e => e.ModifyDate).HasColumnType("smalldatetime");
            });
        }

    }
}

