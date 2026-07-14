using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GCTL.Data.Migrations.GCTL_ERP_DB
{
    /// <inheritdoc />
    public partial class addeduserinfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AccessCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmployeeId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false),
                    BranchCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepartmentCode = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CA_Def_Currency",
                columns: table => new
                {
                    TC = table.Column<decimal>(type: "numeric(18,0)", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CurrencyId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CurrencyName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ShortName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DecimalPlaces = table.Column<int>(type: "int", nullable: true),
                    NegativeFormat = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LDate = table.Column<DateTime>(type: "smalldatetime", nullable: true),
                    LIP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LMAC = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "smalldatetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CA_Def_Currency_TC", x => x.TC);
                });

            migrationBuilder.CreateTable(
                name: "CF_DailyJobUpdateEntry",
                columns: table => new
                {
                    TC = table.Column<decimal>(type: "numeric(18,0)", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DailyJobUpdateEntryID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UpdateStatus = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdateRemarks = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UpdateDateTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    JobNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    LIP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LMAC = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CompanyCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EmployeeID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CF_DailyJobUpdateEntry", x => x.TC);
                });

            migrationBuilder.CreateTable(
                name: "CF_Def_ExpenseType",
                columns: table => new
                {
                    TC = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpenseTypeID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExpenseType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ShortName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    LIP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LMAC = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ExpenseLedgerCodeNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RevenueLedgerCodeNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AdvanceLedgerCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CF_Def_ExpenseType_TC", x => x.TC);
                });

            migrationBuilder.CreateTable(
                name: "CF_Def_PlaceOfLoading",
                columns: table => new
                {
                    AutoID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlaceOfLoadingID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PlaceOfLoadingName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ShortName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    LIP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LMAC = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CF_Def_PlaceOfLoading", x => x.AutoID);
                });

            migrationBuilder.CreateTable(
                name: "CF_Def_ShedYard",
                columns: table => new
                {
                    AutoID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShedYardID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ShedYardName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ShortName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    LIP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LMAC = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CF_Def_ShedYard", x => x.AutoID);
                });

            migrationBuilder.CreateTable(
                name: "Core_AccessCode",
                columns: table => new
                {
                    AccessId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccessCodeId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MenuId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AccessCodeName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TitleCheck = table.Column<bool>(type: "bit", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    PageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CheckAdd = table.Column<bool>(type: "bit", nullable: false),
                    CheckEdit = table.Column<bool>(type: "bit", nullable: false),
                    CheckDelete = table.Column<bool>(type: "bit", nullable: false),
                    CheckPrint = table.Column<bool>(type: "bit", nullable: false),
                    ParentId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OrderBy = table.Column<int>(type: "int", nullable: false, defaultValue: 1)
                        .Annotation("Relational:DefaultConstraintName", "DF_Core_AccessCode_OrderBy"),
                    MenuText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ControllerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ViewName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Core_Acc__4130D05F6FE386D9", x => x.AccessId);
                });

            migrationBuilder.CreateTable(
                name: "Core_Company",
                columns: table => new
                {
                    CompanyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CompanyShortName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ImgTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ZipCode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    State = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone1 = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Phone2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Fax = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HotLine = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    URL = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TIN = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    BackImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BaseCurrency = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    LUser = table.Column<string>(type: "nchar(100)", fixedLength: true, maxLength: 100, nullable: true),
                    LDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    LIP = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LMAC = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CompanyBanglaName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AddressBangla = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Core_Com__11A0134A4B9B764B", x => x.CompanyCode);
                });

            migrationBuilder.CreateTable(
                name: "Core_MenuTab",
                columns: table => new
                {
                    AutoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MenuId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ParentId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OrderBy = table.Column<int>(type: "int", nullable: false, defaultValue: 1)
                        .Annotation("Relational:DefaultConstraintName", "DF_Core_MenuTab2_OrderBy"),
                    ControllerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ViewName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TableName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Core_MenuTab", x => x.AutoId);
                });

            migrationBuilder.CreateTable(
                name: "Country",
                columns: table => new
                {
                    CountryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CountryCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CountryName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LIP = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LMAC = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(NULL)"),
                    DeletedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Country__10D160BFC8D37DA6", x => x.CountryID);
                });

            migrationBuilder.CreateTable(
                name: "DeleteHistory",
                columns: table => new
                {
                    AutoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DHID = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    TableName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    LUser = table.Column<string>(type: "nvarchar(90)", maxLength: 90, nullable: true),
                    LDate = table.Column<DateTime>(type: "smalldatetime", nullable: true),
                    LIP = table.Column<string>(type: "nvarchar(90)", maxLength: 90, nullable: true),
                    LMAC = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CompanyCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserInfoEmployeeID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Field1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field3 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field4 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field5 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field6 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field7 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field8 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field9 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field10 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field11 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field12 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field13 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field14 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field15 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field16 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field17 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field18 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field19 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field20 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field21 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field22 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field23 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field24 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field25 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field26 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field27 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field28 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field29 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field30 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field31 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field32 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field33 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field34 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field35 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field36 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field37 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field38 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field39 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field40 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field41 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field42 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field43 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field44 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field45 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field46 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field47 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field48 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field49 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field50 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field51 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field52 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field53 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field54 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field55 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field56 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field57 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field58 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field59 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field60 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field61 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field62 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field63 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field64 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field65 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field66 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field67 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field68 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field69 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field70 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field71 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field72 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field73 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field74 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field75 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field76 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field77 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field78 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field79 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field80 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field81 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field82 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field83 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field84 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field85 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field86 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field87 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field88 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field89 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field90 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field91 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field92 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field93 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field94 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field95 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field96 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field97 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field98 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field99 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Field100 = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__DeleteHi__6B2329055CA6EA80", x => x.AutoId);
                });

            migrationBuilder.CreateTable(
                name: "Holidays",
                columns: table => new
                {
                    HolidayID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HolidayTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HolidayDescription = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    TotalDays = table.Column<int>(type: "int", nullable: true),
                    StatusID = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                    OrganizationID = table.Column<int>(type: "int", nullable: true),
                    OrganizationBranchID = table.Column<int>(type: "int", nullable: true),
                    LIP = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LMAC = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Holidays__2D35D59A61CC9CA4", x => x.HolidayID);
                });

            migrationBuilder.CreateTable(
                name: "HRM_ATD_CompanyWeekEnd",
                columns: table => new
                {
                    CompanyWeekEndCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    autoId = table.Column<decimal>(type: "numeric(18,0)", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Weekend = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    LUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LDate = table.Column<DateTime>(type: "smalldatetime", nullable: true),
                    LIP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LMAC = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "smalldatetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__HRM_ATD___319EAF56C366F996", x => x.CompanyWeekEndCode);
                });

            migrationBuilder.CreateTable(
                name: "HRM_Def_Nationality",
                columns: table => new
                {
                    NationalityCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    autoId = table.Column<decimal>(type: "numeric(18,0)", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nationality = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    LUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LDate = table.Column<DateTime>(type: "smalldatetime", nullable: true),
                    LIP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LMAC = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "smalldatetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__HRM_Def___1E433F0CB51C8E0D", x => x.NationalityCode);
                });

            migrationBuilder.CreateTable(
                name: "Inv_Def_SupplierCategory",
                columns: table => new
                {
                    SupplierCategoryID = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    SupplierCategoryCode = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierCategory = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    LDate = table.Column<DateTime>(type: "smalldatetime", nullable: true),
                    LUser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LIP = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LMAC = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "smalldatetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Inv_Def___50E4495020A3E1C1", x => x.SupplierCategoryID);
                });

            migrationBuilder.CreateTable(
                name: "Inv_Def_SupplierOrigin",
                columns: table => new
                {
                    SupplierOriginID = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    TC = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierOrigin = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    LDate = table.Column<DateTime>(type: "smalldatetime", nullable: true),
                    LUser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LIP = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LMAC = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "smalldatetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Inv_Def___B904B42DDF63A2BA", x => x.SupplierOriginID);
                });

            migrationBuilder.CreateTable(
                name: "Inv_Def_SupplierType",
                columns: table => new
                {
                    SupplierTypeID = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    SupplierTypeCode = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierTypeName = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    LDate = table.Column<DateTime>(type: "smalldatetime", nullable: true),
                    LUser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LIP = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LMAC = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "smalldatetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Inv_Def___27DA8AF3C32FB6AB", x => x.SupplierTypeID);
                });

            migrationBuilder.CreateTable(
                name: "RMG_Prod_Def_UnitType",
                columns: table => new
                {
                    TC = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnitTypID = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    UnitTypeName = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    ShortName = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    DecimalPlaces = table.Column<int>(type: "int", nullable: true),
                    LUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LDate = table.Column<DateTime>(type: "smalldatetime", nullable: true),
                    LIP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LMAC = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "smalldatetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RMG_Prod_Def_UnitType_TC", x => x.TC);
                });

            migrationBuilder.CreateTable(
                name: "Sales_ContactPerson",
                columns: table => new
                {
                    AutoId = table.Column<decimal>(type: "numeric(18,0)", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CPID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContactPersonName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DesignationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ContactPersonMobile = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContactPersonEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    LIP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LMAC = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CompanyCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EmployeeID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sales_ContactPerson", x => x.AutoId);
                });

            migrationBuilder.CreateTable(
                name: "Sales_Customer",
                columns: table => new
                {
                    CustomerID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerCompany = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CustomerAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ContatPerson1 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Designation1 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    phone1 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ContatPerson2 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Designation2 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    phone2 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ContatPerson3 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Designation3 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    phone3 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreditLImit = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FAX = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    URL = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Email1 = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Email2 = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Email3 = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CustomerType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SalesPersonID = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    BIN = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    LIP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LMAC = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CompanyCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EmployeeID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CountryId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    City = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StateOrProvince = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ZipCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    VatRegNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Tin = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ContactPerson = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DevitOrCredit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OpeningDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ShortName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("TC", x => x.CustomerID);
                });

            migrationBuilder.CreateTable(
                name: "Sales_Def_ImporterInfo",
                columns: table => new
                {
                    TC = table.Column<decimal>(type: "numeric(18,0)", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImporterID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ShortName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Designation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContactPerson2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Designation2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone2 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email2 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LDate = table.Column<DateTime>(type: "smalldatetime", nullable: true),
                    LIP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LMAC = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "smalldatetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sales_Def_ImporterInfo", x => x.TC);
                });

            migrationBuilder.CreateTable(
                name: "Sales_Def_PortName",
                columns: table => new
                {
                    TC = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PortNameId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PortName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ShortName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PortId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    LIP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LMAC = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sales_Def_PortName", x => x.TC);
                });

            migrationBuilder.CreateTable(
                name: "Sales_DeliveryLocation",
                columns: table => new
                {
                    AutoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeliveryLocationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LocationAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContactPerson = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    DesignationCode = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CountryId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    city = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    StateOrProvince = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ZipCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DeliveryCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sales_DeliveryLocation_AutoId", x => x.AutoId);
                });

            migrationBuilder.CreateTable(
                name: "Sep_Documentation",
                columns: table => new
                {
                    TC = table.Column<decimal>(type: "numeric(18,0)", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PrifixId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime", nullable: true),
                    LCNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LCValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IPNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IPDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    UndertakingNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    InvoiceNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BillNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PONo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ContainerNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaterialDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quntity1 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Quntity2 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FWDR_ShippingAgent = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Vessel_RottNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ETA = table.Column<DateTime>(type: "datetime", nullable: true),
                    ETB = table.Column<DateTime>(type: "datetime", nullable: true),
                    BENo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BEDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    Un_StuffingLocationDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    DateOfClearence = table.Column<DateTime>(type: "datetime", nullable: true),
                    ShipmentStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LUser = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    LIP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LMAC = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CompanyCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EmployeeID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Unit1 = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Unit2 = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SupplierId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PortId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PortNameId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CustomerID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CustomerAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocReceivedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ATA = table.Column<DateTime>(type: "datetime", nullable: true),
                    ATB = table.Column<DateTime>(type: "datetime", nullable: true),
                    TransportCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    VehicleID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Vehicle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Vendor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DocumentationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CurrencyId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ConsoleNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExpenseTypeID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsEdit = table.Column<bool>(type: "bit", nullable: true),
                    BDTRate = table.Column<decimal>(type: "decimal(18,5)", nullable: true),
                    MinimumAgencyCom = table.Column<decimal>(type: "decimal(18,5)", nullable: true),
                    CommOnInvoiceValue = table.Column<decimal>(type: "decimal(18,5)", nullable: true),
                    DeliveryLocationCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BLDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    InvoiceDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    LCDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    DocumentTypeID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    HAWB = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MAWB = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LCANo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Dischargedate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ContainerSize = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsCustomJobNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PlaceOfLoadingID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ShedYardID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ETDDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    FreightCharge = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CustomerID_Importer = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DocumentHandoverDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CargoHandoverDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    DepotID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BookingConfirmationReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ImporterID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExporterID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PackingQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PackingUnitType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ETACTGPORT = table.Column<DateTime>(type: "datetime", nullable: true),
                    SHIPARRIVEDDATE = table.Column<DateTime>(type: "datetime", nullable: true),
                    InvoiceValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NameOfFreightForwarder = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    AssessmentNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ClientStationwiseSLNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExpNo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ExpDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    AssessableRate = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AssessableValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    InvoiceCurrencyID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sep_Documentation_TC", x => x.TC);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "([NormalizedName] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "([NormalizedUserName] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "UQ__Country__E056F20171F05A26",
                table: "Country",
                column: "CountryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Country__E056F201AAE41B2C",
                table: "Country",
                column: "CountryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Sep_Docu__056568B0A3253BCB",
                table: "Sep_Documentation",
                column: "JobNo",
                unique: true,
                filter: "[JobNo] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "CA_Def_Currency");

            migrationBuilder.DropTable(
                name: "CF_DailyJobUpdateEntry");

            migrationBuilder.DropTable(
                name: "CF_Def_ExpenseType");

            migrationBuilder.DropTable(
                name: "CF_Def_PlaceOfLoading");

            migrationBuilder.DropTable(
                name: "CF_Def_ShedYard");

            migrationBuilder.DropTable(
                name: "Core_AccessCode");

            migrationBuilder.DropTable(
                name: "Core_Company");

            migrationBuilder.DropTable(
                name: "Core_MenuTab");

            migrationBuilder.DropTable(
                name: "Country");

            migrationBuilder.DropTable(
                name: "DeleteHistory");

            migrationBuilder.DropTable(
                name: "Holidays");

            migrationBuilder.DropTable(
                name: "HRM_ATD_CompanyWeekEnd");

            migrationBuilder.DropTable(
                name: "HRM_Def_Nationality");

            migrationBuilder.DropTable(
                name: "Inv_Def_SupplierCategory");

            migrationBuilder.DropTable(
                name: "Inv_Def_SupplierOrigin");

            migrationBuilder.DropTable(
                name: "Inv_Def_SupplierType");

            migrationBuilder.DropTable(
                name: "RMG_Prod_Def_UnitType");

            migrationBuilder.DropTable(
                name: "Sales_ContactPerson");

            migrationBuilder.DropTable(
                name: "Sales_Customer");

            migrationBuilder.DropTable(
                name: "Sales_Def_ImporterInfo");

            migrationBuilder.DropTable(
                name: "Sales_Def_PortName");

            migrationBuilder.DropTable(
                name: "Sales_DeliveryLocation");

            migrationBuilder.DropTable(
                name: "Sep_Documentation");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
