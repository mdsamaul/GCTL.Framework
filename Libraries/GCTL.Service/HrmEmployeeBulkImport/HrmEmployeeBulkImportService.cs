using GCTL.Core.Data;
using GCTL.Core.Interfaces.Services;
using GCTL.Data.Models;
using Microsoft.AspNetCore.Http;
using NPOI.HSSF.UserModel;
using NPOI.HSSF.Util;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace GCTL.Service.Services
{
    // ─────────────────────────────────────────────────────────────────────────
    // Result DTO
    // ─────────────────────────────────────────────────────────────────────────
    public class BulkImportResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public int SuccessCount { get; set; }
        public int FailCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Service
    // ─────────────────────────────────────────────────────────────────────────
    public class HrmEmployeeBulkImportService : IHrmEmployeeBulkImportService
    {
        // ── Excel Column Indexes (0-based) ────────────────────────────────────
        private const int COL_EMPLOYEE_ID = 0;
        private const int COL_ATTENDANCE_ID = 1;
        private const int COL_NAME = 2;
        private const int COL_DESIGNATION = 3;
        private const int COL_DEPARTMENT = 4;
        private const int COL_DIVISION = 5;
        private const int COL_BRANCH = 6;
        private const int COL_COMPANY = 7;
        private const int COL_JOINING_DATE = 8;
        private const int COL_EMP_TYPE = 9;
        private const int COL_SHIFT = 10;
        private const int COL_EMP_NATURE = 11;
        private const int COL_GRADE = 12;
        private const int COL_GROSS_SALARY = 13;
        private const int COL_PAYMENT_PERIOD = 14;
        private const int COL_MODE_OF_PAYMENT = 15;
        private const int COL_PERCENT_IN_BANK = 16;
        private const int COL_SUPERVISOR_ID = 17;
        private const int COL_HOD_ID = 18;
        private const int COL_GENDER = 19;
        private const int COL_PHONE = 20;
        private const int COL_EMAIL = 21;
        private const int COL_NID = 22;
        private const int COL_FATHER_NAME = 23;
        private const int COL_MOTHER_NAME = 24;
        private const int COL_DATE_OF_BIRTH = 25;
        private const int COL_BLOOD_GROUP = 26;

        // ── Repositories ──────────────────────────────────────────────────────
        private readonly IRepository<HrmEmployee> _empRepository;
        private readonly IRepository<HrmEmployeeOfficialInfo> _officialInfoRepository;
        private readonly IRepository<CoreCompany> _companyRepository;
        private readonly IRepository<CoreBranch> _branchRepository;
        private readonly IRepository<HrmDefDivision> _divisionRepository;
        private readonly IRepository<HrmDefDepartment> _departmentRepository;
        private readonly IRepository<HrmDefDesignation> _designationRepository;
        private readonly IRepository<HrmDefEmpType> _empTypeRepository;
        private readonly IRepository<HrmDefGradeType> _gradeRepository;
        private readonly IRepository<HrmEisDefEmploymentNature> _employmentNatureRepository;
        private readonly IRepository<CorePeriodInfo> _paymentPeriodRepository;
        private readonly IRepository<HrmEisDefDisbursementMethod> _modeOfPaymentRepository;
        private readonly IRepository<HrmAtdShift> _shiftRepository;
        private readonly IRepository<HrmDefSex> _genderRepository;
        private readonly IRepository<HrmDefBloodGroup> _bloodGroupRepository;

        // ── Constructor ───────────────────────────────────────────────────────
        public HrmEmployeeBulkImportService(
            IRepository<HrmEmployee> empRepository,
            IRepository<HrmEmployeeOfficialInfo> officialInfoRepository,
            IRepository<CoreCompany> companyRepository,
            IRepository<CoreBranch> branchRepository,
            IRepository<HrmDefDivision> divisionRepository,
            IRepository<HrmDefDepartment> departmentRepository,
            IRepository<HrmDefDesignation> designationRepository,
            IRepository<HrmDefEmpType> empTypeRepository,
            IRepository<HrmDefGradeType> gradeRepository,
            IRepository<HrmEisDefEmploymentNature> employmentNatureRepository,
            IRepository<CorePeriodInfo> paymentPeriodRepository,
            IRepository<HrmEisDefDisbursementMethod> modeOfPaymentRepository,
            IRepository<HrmAtdShift> shiftRepository,
            IRepository<HrmDefSex> genderRepository,
            IRepository<HrmDefBloodGroup> bloodGroupRepository
            
            )
        {
            _empRepository = empRepository;
            _officialInfoRepository = officialInfoRepository;
            _companyRepository = companyRepository;
            _branchRepository = branchRepository;
            _divisionRepository = divisionRepository;
            _departmentRepository = departmentRepository;
            _designationRepository = designationRepository;
            _empTypeRepository = empTypeRepository;
            _gradeRepository = gradeRepository;
            _employmentNatureRepository = employmentNatureRepository;
            _paymentPeriodRepository = paymentPeriodRepository;
            _modeOfPaymentRepository = modeOfPaymentRepository;
            _shiftRepository = shiftRepository;
            _genderRepository = genderRepository;
            _bloodGroupRepository = bloodGroupRepository;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Public Methods
        // ─────────────────────────────────────────────────────────────────────

        public async Task<BulkImportResult> BulkImportFromExcelAsync(
            IFormFile file,
            string companyCode,
            string luser,
            string lip,
            string lmac)
        {
            var result = new BulkImportResult();

            try
            {
                var workbook = OpenWorkbook(file);
                if (workbook is null)
                {
                    result.Message = "Invalid file format. Only .xlsx or .xls allowed.";
                    return result;
                }

                var sheet = workbook.GetSheetAt(0);
                if (sheet is null)
                {
                    result.Message = "Excel sheet is empty.";
                    return result;
                }

                for (int rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
                {
                    var row = sheet.GetRow(rowIndex);
                    if (row is null) continue;

                    var employeeId = GetCellString(row, COL_EMPLOYEE_ID)?.Trim();
                    if (string.IsNullOrWhiteSpace(employeeId)) continue;

                    await ProcessRowAsync(row, employeeId, luser, lip, lmac, rowIndex, result);
                }

                result.IsSuccess = result.SuccessCount > 0;
                result.Message = result.SuccessCount > 0
                    ? $"{result.SuccessCount} employee(s) imported successfully."
                    : "No records were imported.";
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = $"Failed to process Excel: {ex.Message}";
            }

            return result;
        }

        public byte[] GenerateExcelTemplate()
        {
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("Sheet1");

            var headerStyle = CreateHeaderStyle(workbook);
            var sampleStyle = CreateSampleStyle(workbook);

            var headers = new[]
            {
                "Employee ID",               "Attendance ID",             "Name",
                "Designation",               "Department",                "Division",
                "Branch",                    "Company",                   "Joining Date (2024-01-30)",
                "Employee Type",             "Shift Name",                "Employment Nature",
                "Grade",                     "Gross Salary",              "Payment Period",
                "Mode of Payment",           "% In Bank",                 "Immediate\nSuperviser ID",
                "Head of the Department ID", "Gender",                    "Phone Number",
                "E-mail ID",                 "NID",                       "Father Name",
                "Mother Name",               "Date of Birth (2024-01-30)","Blood Group"
            };

            var headerRow = sheet.CreateRow(0);
            headerRow.HeightInPoints = 30;
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = headerRow.CreateCell(i);
                cell.SetCellValue(headers[i]);
                cell.CellStyle = headerStyle;
                sheet.SetColumnWidth(i, 22 * 256);
            }

            var sampleValues = new[]
            {
                "BG-0124-1001", "1241001",            "Employee Full Name",
                "Executive",    "HR & Admin",         "Division Name",
                "Head Office",  "Company Name",       "2024-01-30",
                "Full Time",    "G (HO), 9:30-18:30", "Permanent",
                "MG-05",        "20000",              "Monthly",
                "Bank",         "50",                 "",
                "",             "Male",               "01700000000",
                "email@example.com", "1234567890123", "Father Name",
                "Mother Name",  "2000-01-01",         "A+"
            };

            var sampleRow = sheet.CreateRow(1);
            for (int i = 0; i < sampleValues.Length; i++)
            {
                var cell = sampleRow.CreateCell(i);
                cell.SetCellValue(sampleValues[i]);
                cell.CellStyle = sampleStyle;
            }

            using var ms = new MemoryStream();
            workbook.Write(ms);
            return ms.ToArray();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private Helpers – Row Processing
        // ─────────────────────────────────────────────────────────────────────

        private async Task ProcessRowAsync(
            IRow row,
            string employeeId,
            string luser,
            string lip,
            string lmac,
            int rowIndex,
            BulkImportResult result)
        {
            try
            {
                await _empRepository.BeginTransactionAsync();

                var exists = _empRepository.All().Any(x => x.EmployeeId == employeeId);
                if (exists)
                {
                    await _empRepository.RollbackTransactionAsync();
                    return;
                }

                var codes = ResolveLookupCodes(row);

                await _empRepository.AddAsync(BuildEmployee(row, employeeId, codes, luser, lip, lmac));
                await _officialInfoRepository.AddAsync(BuildOfficialInfo(row, employeeId, codes, luser, lip, lmac));

                await _empRepository.CommitTransactionAsync();
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                await _empRepository.RollbackTransactionAsync();
                result.FailCount++;
                result.Errors.Add($"Row {rowIndex + 1} (EmployeeId: {employeeId}): {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private Helpers – Lookup Code Resolution
        // ─────────────────────────────────────────────────────────────────────
                        
        private LookupCodes ResolveLookupCodes(IRow row)
        {
            return new LookupCodes
            {
                CompanyCode = _companyRepository.All()
                    .FirstOrDefault(x => x.CompanyName == GetCellString(row, COL_COMPANY))?.CompanyCode ?? "",

                BranchCode = _branchRepository.All()
                    .FirstOrDefault(x => x.BranchName == GetCellString(row, COL_BRANCH))?.BranchCode ?? "",

                DivisionCode = _divisionRepository.All()
                    .FirstOrDefault(x => x.DivisionName == GetCellString(row, COL_DIVISION))?.DivisionCode ?? "",

                DepartmentCode = _departmentRepository.All()
                    .FirstOrDefault(x => x.DepartmentName == GetCellString(row, COL_DEPARTMENT))?.DepartmentCode ?? "",

                DesignationCode = _designationRepository.All()
                    .FirstOrDefault(x => x.DesignationName == GetCellString(row, COL_DESIGNATION))?.DesignationCode ?? "",

                EmpTypeCode = _empTypeRepository.All()
                    .FirstOrDefault(x => x.EmpTypeName == GetCellString(row, COL_EMP_TYPE))?.EmpTypeCode ?? "",

                GradeCode = _gradeRepository.All()
                    .FirstOrDefault(x => x.GradeType == GetCellString(row, COL_GRADE))?.GradeTypeId ?? "",

                EmploymentNatureId = _employmentNatureRepository.All()
                    .FirstOrDefault(x => x.EmploymentNature == GetCellString(row, COL_EMP_NATURE))?.EmploymentNatureId ?? "",

                PaymentPeriodId = _paymentPeriodRepository.All()
                    .FirstOrDefault(x => x.PeriodName == GetCellString(row, COL_PAYMENT_PERIOD))?.PeriodInfoId ?? "",

                DisbursementMethodId = _modeOfPaymentRepository.All()
                    .FirstOrDefault(x => x.DisbursementMethod == GetCellString(row, COL_MODE_OF_PAYMENT))?.DisbursementMethodId ?? "",

                ShiftCode = _shiftRepository.All()
                    .FirstOrDefault(x => x.ShiftName == GetCellString(row, COL_SHIFT))?.ShiftCode ?? "",

                GenderCode = _genderRepository.All()
                    .FirstOrDefault(x => x.Sex == GetCellString(row, COL_GENDER))?.SexCode ?? "",

                BloodGroupCode = _bloodGroupRepository.All()
                    .FirstOrDefault(x => x.BloodGroup == GetCellString(row, COL_BLOOD_GROUP))?.BloodGroupCode ?? ""
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private Helpers – Entity Builders
        // ─────────────────────────────────────────────────────────────────────

        private HrmEmployee BuildEmployee(
            IRow row,
            string employeeId,
            LookupCodes codes,
            string luser, string lip, string lmac)
        {
            var fullName = GetCellString(row, COL_NAME);
            var nameParts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var firstName = nameParts.Length > 0 ? nameParts[0] : fullName;
            var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

            return new HrmEmployee
            {
                EmployeeId = employeeId,
                FirstName = firstName,
                LastName = lastName,
                FatherName = GetCellString(row, COL_FATHER_NAME),
                MotherName = GetCellString(row, COL_MOTHER_NAME),
                DateOfBirthCertificate = null,                              // DateTime?
                DateOfBirthOrginal = GetCellDate(row, COL_DATE_OF_BIRTH),  // DateTime?
                BirthCertificateNo = "",
                PlaceOfBirth = "",
                SexCode = codes.GenderCode,
                BloodGroupCode = codes.BloodGroupCode,
                NationalityCode = "",
                NationalIdno = GetCellString(row, COL_NID),
                ReligionCode = "",
                MaritalStatusCode = "",
                MarriageDate = null,                                 // DateTime?
                NoOfSon = "",
                NoOfDaughters = "",
                CardNo = "",
                Luser = luser,
                Ldate = DateTime.Now,
                Lip = lip,
                Lmac = lmac,
                ModifyDate = null,                                 // DateTime?
                CompanyCode = codes.CompanyCode,
                UserInfoEmployeeId = "",
                PersonalEmail = GetCellString(row, COL_EMAIL),
                Telephone = GetCellString(row, COL_PHONE),
                TinNo = "",
                ExtraCurriActivities = "",
                Remarks = "",
                FirstNameBangla = "",
                LastNameBangla = "",
                BranchCode = codes.BranchCode,
                FathersOccupation = "",
                MothersOccupation = "",
                Spouse = "",
            };
        }

        private HrmEmployeeOfficialInfo BuildOfficialInfo(
            IRow row,
            string employeeId,
            LookupCodes codes,
            string luser, string lip, string lmac)
        {
            return new HrmEmployeeOfficialInfo
            {
                EmployeeId = employeeId,
                CompanyCode = codes.CompanyCode,
                BranchCode = codes.BranchCode,
                DivisionCode = codes.DivisionCode,
                DepartmentCode = codes.DepartmentCode,
                DesignationCode = codes.DesignationCode,
                EmpTypeCode = codes.EmpTypeCode,
                GradeCode = codes.GradeCode,
                EmploymentNatureId = codes.EmploymentNatureId,
                GrossSalary = GetCellDecimal(row, COL_GROSS_SALARY),
                CurrencyCode = "",
                PaymentPeriodId = codes.PaymentPeriodId,
                DisbursementMethodId = codes.DisbursementMethodId,
                ShiftCode = codes.ShiftCode,
                EmployeeStatus = "",
                ReportingTo = GetCellString(row, COL_SUPERVISOR_ID),
                Hod = GetCellString(row, COL_HOD_ID),
                MobileNo = "",
                Email = "",
                AppointmentLetterNo = "",
                AppointmentLetterDate = null,                           // DateTime?
                JoiningDate = GetCellDate(row, COL_JOINING_DATE),      // DateTime?
                JoiningSalary = 0,
                ProbationPeriodType = "",
                ProbationPeriod = "",
                ConfirmeDate = null,                                 // DateTime?
                Luser = luser,
                Ldate = DateTime.Now,
                Lip = lip,
                Lmac = lmac,
                ModifyDate = null,                                // DateTime?
                CompanyCodeSession = "",
                UserInfoEmployeeId = "",
                StepNoId = "",
                TecnicalSkillTypeId = "",
                SalaryScaleId = "",
                ContractEndDate = null,                         // DateTime?
                SectionCode = "",
                LineCode = "",
                AttendenceId = GetCellString(row, COL_ATTENDANCE_ID),
                IsExpatriate = "",
                ExpatriateBasicSalary = 0m,                               // decimal?
                ExpatriateHouseRent = 0m,                                // decimal?
                ExpatriateConveyance = 0m,                              // decimal?
                ExpatriateMedical = 0m,                                // decimal?
                Lfa = 0m,                                             // decimal?
                MobileAllowance = 0m,                                // decimal?
                ConfirmationRefNo = "",
                ProbationEffectDate = null,                                 // DateTime?
                ModeOfPaymentInBankPercentage = GetCellDecimalNullable(row, COL_PERCENT_IN_BANK),
                IsLunchBilEligible = "",
                IsOverTimeEligible = "",
                IsExtraDutyEligible = "",
                IsGovtHolidayEligible = "",
                IsAttendanceBonusEligible = "",
                PayId = "",
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private Helpers – Excel Template Styles
        // ─────────────────────────────────────────────────────────────────────

        private static ICellStyle CreateHeaderStyle(IWorkbook workbook)
        {
            var font = workbook.CreateFont();
            font.IsBold = true;
            font.FontHeightInPoints = 10;
            font.FontName = "Arial";

            var style = workbook.CreateCellStyle();
            style.SetFont(font);
            style.FillForegroundColor = HSSFColor.White.Index;
            style.FillPattern = FillPattern.SolidForeground;
            style.Alignment = HorizontalAlignment.Center;
            style.BorderBottom = BorderStyle.Thin;
            style.BorderTop = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderRight = BorderStyle.Thin;
            style.WrapText = true;
            return style;
        }

        private static ICellStyle CreateSampleStyle(IWorkbook workbook)
        {
            var style = workbook.CreateCellStyle();
            style.BorderBottom = BorderStyle.Thin;
            style.BorderTop = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderRight = BorderStyle.Thin;
            return style;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private Helpers – Workbook
        // ─────────────────────────────────────────────────────────────────────

        private static IWorkbook? OpenWorkbook(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            using var stream = file.OpenReadStream();

            return ext switch
            {
                ".xlsx" => new XSSFWorkbook(stream),
                ".xls" => (IWorkbook)new HSSFWorkbook(stream),
                _ => null
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private Helpers – Cell Reading
        // ─────────────────────────────────────────────────────────────────────

        private static string GetCellString(IRow row, int colIndex)
        {
            var cell = row.GetCell(colIndex);
            if (cell is null) return string.Empty;

            return cell.CellType switch
            {
                CellType.Numeric when DateUtil.IsCellDateFormatted(cell)
                    => cell.DateCellValue?.ToString("yyyy-MM-dd") ?? string.Empty,
                CellType.Numeric
                    => cell.NumericCellValue.ToString("G"),
                CellType.Boolean
                    => cell.BooleanCellValue.ToString(),
                CellType.Formula when cell.CachedFormulaResultType == CellType.Numeric
                    => cell.NumericCellValue.ToString("G"),
                _
                    => cell.StringCellValue?.Trim() ?? string.Empty
            };
        }

        private static DateTime? GetCellDate(IRow row, int colIndex)
        {
            var cell = row.GetCell(colIndex);
            if (cell is null) return null;

            try
            {
                if (cell.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cell))
                    return cell.DateCellValue;

                var str = cell.StringCellValue?.Trim();
                return string.IsNullOrEmpty(str) ? null
                    : DateTime.TryParse(str, out var dt) ? dt : null;
            }
            catch { return null; }
        }

        private static decimal GetCellDecimal(IRow row, int colIndex)
        {
            var cell = row.GetCell(colIndex);
            return cell?.CellType == CellType.Numeric
                ? (decimal)cell.NumericCellValue
                : 0m;
        }

        private static decimal? GetCellDecimalNullable(IRow row, int colIndex)
        {
            var cell = row.GetCell(colIndex);
            return cell?.CellType == CellType.Numeric
                ? (decimal?)cell.NumericCellValue
                : 0m;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private DTO – Resolved Lookup Codes
        // ─────────────────────────────────────────────────────────────────────

        private sealed class LookupCodes
        {
            public string CompanyCode { get; init; } = "";
            public string BranchCode { get; init; } = "";
            public string DivisionCode { get; init; } = "";
            public string DepartmentCode { get; init; } = "";
            public string DesignationCode { get; init; } = "";
            public string EmpTypeCode { get; init; } = "";
            public string GradeCode { get; init; } = "";
            public string EmploymentNatureId { get; init; } = "";
            public string PaymentPeriodId { get; init; } = "";
            public string DisbursementMethodId { get; init; } = "";
            public string ShiftCode { get; init; } = "";
            public string GenderCode { get; init; } = "";
            public string BloodGroupCode { get; init; } = "";
        }
    }
}