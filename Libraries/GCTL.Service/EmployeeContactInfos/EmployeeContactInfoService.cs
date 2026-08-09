using GCTL.Core.Data;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.EmployeeContactInfos;
using GCTL.Data.Models;
using GCTL.Service.DeleteHistories;
using Microsoft.EntityFrameworkCore;

namespace GCTL.Service.EmployeeContactInfos
{
    public class EmployeeContactInfoService : AppService<HrmEmployeeContactInfo>, IEmployeeContactInfoService
    {
        #region Service & Rep
        private readonly IRepository<HrmEmployeeContactInfo> employeeContactInfoRepository;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly IRepository<CoreBranch> coreBranchRepository;
        private readonly IRepository<CoreCompany> coreCompanyRepository;
        private readonly IRepository<HrmDefRelationship> reletionshipRepository;
        private readonly IRepository<HrmDefDistrict> districtRepository;
        private readonly IRepository<HrmDefDepartment> departmentRepository;
        private readonly IRepository<HrmDefDesignation> designationRepository;
        private readonly IRepository<HrmEmployeeOfficialInfo> hrmEmpOffialInfo;
        private readonly IRepository<HrmEmployee> hrmEmployee;
        private readonly IDeleteHistoryService deleteHistoryService;

        string strMaxNO = string.Empty;
        private const string TableName = "HRM_EmployeeContactInfo";
        private const string ColumnName = "EmpContactID";

        public EmployeeContactInfoService(
            IRepository<HrmEmployeeContactInfo> employeeContactInfoRepository, 
            IRepository<HrmEmployeeOfficialInfo> hrmEmpOffialInfo, 
            IRepository<CoreAccessCode> accessCodeRepository, 
            IRepository<CoreBranch> coreBranchRepository, 
            IRepository<CoreCompany> coreCompanyRepository, 
            IRepository<HrmEmployee> hrmEmployee, 
            IRepository<HrmDefRelationship> reletionshipRepository, 
            IRepository<HrmDefDistrict> districtRepository, 
            IRepository<HrmDefDepartment> departmentRepository, 
            IRepository<HrmDefDesignation> designationRepository,
            IDeleteHistoryService deleteHistoryService

            )
    : base(employeeContactInfoRepository)
        {
            this.employeeContactInfoRepository = employeeContactInfoRepository;
            this.accessCodeRepository = accessCodeRepository;
            this.coreBranchRepository = coreBranchRepository;
            this.coreCompanyRepository = coreCompanyRepository;
            this.reletionshipRepository = reletionshipRepository;
            this.districtRepository = districtRepository;
            this.departmentRepository = departmentRepository;
            this.designationRepository = designationRepository;
            this.hrmEmpOffialInfo = hrmEmpOffialInfo;
            this.hrmEmployee = hrmEmployee;
            this.deleteHistoryService = deleteHistoryService;
        }

        #endregion

        #region GetAllAsync

        public async Task<List<EmployeeContactInfosSetupViewModel>> GetAllAsync()
        {
            var data = (from emp in employeeContactInfoRepository.All().AsNoTracking()

                        join Hrmemp in coreBranchRepository.All().AsNoTracking()
                        on emp.BranchCode equals Hrmemp.BranchCode into empBranchNameJoin
                        from Hrmemp in empBranchNameJoin.DefaultIfEmpty()

                        join hrmEmp in hrmEmployee.All().AsNoTracking()
                        on emp.EmployeeId equals hrmEmp.EmployeeId into empEmployeeJoin
                        from hrmEmp in empEmployeeJoin.DefaultIfEmpty()

                        join hrmDef in reletionshipRepository.All().AsNoTracking()
                        on emp.EmContactRelation1 equals hrmDef.RelationshipCode into empEmContactRelation1Join
                        from hrmDef in empEmContactRelation1Join.DefaultIfEmpty()

                        join hrmdef in reletionshipRepository.All().AsNoTracking()
                        on emp.EmContactRelation2 equals hrmdef.RelationshipCode into empEmContactRelation2Join
                        from hrmdef in empEmContactRelation2Join.DefaultIfEmpty()

                        join HrmDef in districtRepository.All().AsNoTracking()
                        on emp.ParmanentDistrict equals HrmDef.DistrictId into empParmanentDistrictJoin
                        from HrmDef in empParmanentDistrictJoin.DefaultIfEmpty()

                        join hrmdep in districtRepository.All().AsNoTracking()
                        on emp.PresentDistrict equals hrmdep.DistrictId into empPresentDistrictJoin
                        from hrmdep in empPresentDistrictJoin.DefaultIfEmpty()

                        join ofEmp in hrmEmpOffialInfo.All().AsNoTracking()
                        on emp.EmployeeId equals ofEmp.EmployeeId into eduOffJoin
                        from ofEmp in eduOffJoin.DefaultIfEmpty()

                        join desi in designationRepository.All().AsNoTracking()
                        on ofEmp.DesignationCode equals desi.DesignationCode into edudesiJoin
                        from desi in edudesiJoin.DefaultIfEmpty()

                        join dept in departmentRepository.All().AsNoTracking()
                        on ofEmp.DepartmentCode equals dept.DepartmentCode into eduDeptJoin
                        from dept in eduDeptJoin.DefaultIfEmpty()

                        select new EmployeeContactInfosSetupViewModel
                        {
                            EmployeeId = emp.EmployeeId,
                            EmployeeName = $"{hrmEmp.FirstName} {hrmEmp.LastName}",
                            DistrictName = HrmDef.District,
                            EmpContactId = emp.EmpContactId,
                            ParmanentAddress = emp.ParmanentAddress,
                            ParmanentPostOffice = emp.ParmanentPostOffice,
                            ParmanentThana = emp.ParmanentThana

                        }).ToList();

            //  return data;
            return await Task.FromResult(data);
        }

        public async Task<List<EmployeeContactInfosSetupViewModel>> GetByEmployeeAsync(string employeeId)
        {
            var data = await (from emp in employeeContactInfoRepository.All().AsNoTracking()
                              .Where(x => x.EmployeeId == employeeId)

                              join Hrmemp in coreBranchRepository.All().AsNoTracking()
                              on emp.BranchCode equals Hrmemp.BranchCode into empBranchNameJoin
                              from Hrmemp in empBranchNameJoin.DefaultIfEmpty()

                              join hrmEmp in hrmEmployee.All().AsNoTracking()
                              on emp.EmployeeId equals hrmEmp.EmployeeId into empEmployeeJoin
                              from hrmEmp in empEmployeeJoin.DefaultIfEmpty()

                              join hrmDef in reletionshipRepository.All().AsNoTracking()
                              on emp.EmContactRelation1 equals hrmDef.RelationshipCode into empEmContactRelation1Join
                              from hrmDef in empEmContactRelation1Join.DefaultIfEmpty()

                              join hrmdef in reletionshipRepository.All().AsNoTracking()
                              on emp.EmContactRelation2 equals hrmdef.RelationshipCode into empEmContactRelation2Join
                              from hrmdef in empEmContactRelation2Join.DefaultIfEmpty()

                              join HrmDef in districtRepository.All().AsNoTracking()
                              on emp.ParmanentDistrict equals HrmDef.DistrictId into empParmanentDistrictJoin
                              from HrmDef in empParmanentDistrictJoin.DefaultIfEmpty()

                              join hrmdep in districtRepository.All().AsNoTracking()
                              on emp.PresentDistrict equals hrmdep.DistrictId into empPresentDistrictJoin
                              from hrmdep in empPresentDistrictJoin.DefaultIfEmpty()

                              join ofEmp in hrmEmpOffialInfo.All().AsNoTracking()
                              on emp.EmployeeId equals ofEmp.EmployeeId into eduOffJoin
                              from ofEmp in eduOffJoin.DefaultIfEmpty()

                              join desi in designationRepository.All().AsNoTracking()
                              on ofEmp.DesignationCode equals desi.DesignationCode into edudesiJoin
                              from desi in edudesiJoin.DefaultIfEmpty()

                              join dept in departmentRepository.All().AsNoTracking()
                              on ofEmp.DepartmentCode equals dept.DepartmentCode into eduDeptJoin
                              from dept in eduDeptJoin.DefaultIfEmpty()

                              select new EmployeeContactInfosSetupViewModel
                              {
                                  EmployeeId = emp.EmployeeId,
                                  EmployeeName = $"{hrmEmp.FirstName} {hrmEmp.LastName}",
                                  DistrictName = HrmDef != null ? HrmDef.District : " ",
                                  EmpContactId = emp.EmpContactId,
                                  ParmanentAddress = emp.ParmanentAddress,
                                  ParmanentPostOffice = emp.ParmanentPostOffice,
                                  ParmanentThana = emp.ParmanentThana

                              }).ToListAsync();

            return data;
        }

        #endregion

        #region GetByIdAsync

        public async Task<EmployeeContactInfosSetupViewModel> GetByIdAsync(string id)
        {
            var data = await (from emp in employeeContactInfoRepository.All().AsNoTracking()

                              join Hrmemp in coreBranchRepository.All().AsNoTracking()
                              on emp.BranchCode equals Hrmemp.BranchCode into empBranchNameJoin
                              from Hrmemp in empBranchNameJoin.DefaultIfEmpty()

                              join hrmEmp in hrmEmployee.All().AsNoTracking()
                              on emp.EmployeeId equals hrmEmp.EmployeeId into empEmployeeJoin
                              from hrmEmp in empEmployeeJoin.DefaultIfEmpty()


                              join hrmDef in reletionshipRepository.All().AsNoTracking()
                              on emp.EmContactRelation1 equals hrmDef.RelationshipCode into empEmContactRelation1Join
                              from hrmDef in empEmContactRelation1Join.DefaultIfEmpty()

                              join hrmdef in reletionshipRepository.All().AsNoTracking()
                              on emp.EmContactRelation2 equals hrmdef.RelationshipCode into empEmContactRelation2Join
                              from hrmdef in empEmContactRelation2Join.DefaultIfEmpty()

                              join HrmDef in districtRepository.All().AsNoTracking()
                              on emp.ParmanentDistrict equals HrmDef.DistrictId into empParmanentDistrictJoin
                              from HrmDef in empParmanentDistrictJoin.DefaultIfEmpty()

                              join hrmdep in districtRepository.All().AsNoTracking()
                              on emp.PresentDistrict equals hrmdep.DistrictId into empPresentDistrictJoin
                              from hrmdep in empPresentDistrictJoin.DefaultIfEmpty()

                              join empComp in coreCompanyRepository.All().AsNoTracking()
                              on emp.CompanyCode equals empComp.CompanyCode into empComJoin
                              from empComp in empComJoin.DefaultIfEmpty()

                              join ofEmp in hrmEmpOffialInfo.All().AsNoTracking()
                              on emp.EmployeeId equals ofEmp.EmployeeId into eduOffJoin
                              from ofEmp in eduOffJoin.DefaultIfEmpty()

                              join desi in designationRepository.All().AsNoTracking()
                              on ofEmp.DesignationCode equals desi.DesignationCode into edudesiJoin
                              from desi in edudesiJoin.DefaultIfEmpty()

                              join dept in departmentRepository.All().AsNoTracking()
                              on ofEmp.DepartmentCode equals dept.DepartmentCode into eduDeptJoin
                              from dept in eduDeptJoin.DefaultIfEmpty()

                              where emp.EmpContactId == id

                              select new EmployeeContactInfosSetupViewModel
                              {
                                  AutoId = emp.AutoId,
                                  EmpContactId = emp.EmpContactId,
                                  EmployeeId = emp.EmployeeId,
                                  EmployeeName = $"{hrmEmp.FirstName}{hrmEmp.LastName}",
                                  DepartmentName = dept.DepartmentName ?? "",
                                  DesignationName = desi.DesignationName ?? "",
                                //  Relation = hrmDef.Relationship,
                                  BranchCode = emp.BranchCode,
                                  BranchName = Hrmemp.BranchName,
                                  CompanyCode = emp.CompanyCode,
                                  CompanyName = empComp.CompanyName,
                                  ParmanentAddress = emp.ParmanentAddress,
                                  ParmanentAddressBangla = emp.ParmanentAddressBangla,
                                  ParmanentPostOffice = emp.ParmanentPostOffice,
                                  ParmanentThana = emp.ParmanentThana,
                                  ParmanentPostCode = emp.ParmanentPostCode,
                                  ParmanentDistrict = emp.ParmanentDistrict,
                                  ParmanentPhone = emp.ParmanentPhone,
                                  PresentAddress = emp.PresentAddress,
                                  PresentAddressBangla = emp.PresentAddressBangla,
                                  PresentPostOffice = emp.PresentPostOffice,
                                  PresentThana = emp.PresentThana,
                                  PresentPostCode = emp.PresentPostCode,
                                  PresentDistrict = emp.PresentDistrict,
                                  PresentMobile = emp.PresentMobile,
                                  PresentPhone = emp.PresentPhone,
                                  PresentFax = emp.PresentFax,
                                  PresentEmail = emp.PresentEmail,
                                  EmContactName1 = emp.EmContactName1,
                                  EmContactRelation1 = emp.EmContactRelation1,
                                  EmContactAddress1 = emp.EmContactAddress1,
                                  EmContactPhone1 = emp.EmContactPhone1,
                                  EmContactMobile1 = emp.EmContactMobile1,
                                  EmContactFax1 = emp.EmContactFax1,
                                  EmContactEmail = emp.EmContactEmail,
                                  EmContactName2 = emp.EmContactName2,
                                  EmContactRelation2 = emp.EmContactRelation2,
                                  EmContactAddress2 = emp.EmContactAddress2,
                                  EmContactPhone2 = emp.EmContactPhone2,
                                  EmContactMobile2 = emp.EmContactMobile2,
                                  EmContactFax2 = emp.EmContactFax2,
                                  EmContactEmai2 = emp.EmContactEmai2,
                                  Ldate = emp.Ldate,
                                  ModifyDate = emp.ModifyDate

                              }).FirstOrDefaultAsync();

            return data;
        }

        #endregion

        #region SaveAsync

        public async Task<bool> SaveAsync(EmployeeContactInfosSetupViewModel entityVM, string CompanyCode)
        {
            await employeeContactInfoRepository.BeginTransactionAsync();
            try
            {
                HrmEmployeeContactInfo entity = new HrmEmployeeContactInfo();

                entity.EmpContactId = await GenerateNextCode();
                entity.EmployeeId = entityVM.EmployeeId;
                entity.CompanyCode = entityVM.CompanyCode ?? string.Empty;
                entity.BranchCode = entityVM.BranchCode;
                entity.ParmanentAddress = entityVM.ParmanentAddress;
                entity.ParmanentAddressBangla = entityVM.ParmanentAddressBangla;
                entity.ParmanentPostOffice = entityVM.ParmanentPostOffice;
                entity.ParmanentThana = entityVM.ParmanentThana;
                entity.ParmanentPostCode = entityVM.ParmanentPostCode;
                entity.ParmanentDistrict = entityVM.ParmanentDistrict;
                entity.ParmanentPhone = entityVM.ParmanentPhone;
                entity.PresentAddress = entityVM.PresentAddress;
                entity.PresentAddressBangla = entityVM.PresentAddressBangla;
                entity.PresentPostOffice = entityVM.PresentPostOffice;
                entity.PresentThana = entityVM.PresentThana;
                entity.PresentPostCode = entityVM.PresentPostCode;
                entity.PresentDistrict = entityVM.PresentDistrict;
                entity.PresentMobile = entityVM.PresentMobile;
                entity.PresentPhone = entityVM.PresentPhone;
                entity.PresentFax = entityVM.PresentFax;
                entity.PresentEmail = entityVM.PresentEmail;
                entity.PresentEmail = entityVM.PresentEmail;
                entity.EmContactName1 = entityVM.EmContactName1;
                entity.EmContactRelation1 = entityVM.EmContactRelation1;
                entity.EmContactAddress1 = entityVM.EmContactAddress1;
                entity.EmContactPhone1 = entityVM.EmContactPhone1;
                entity.EmContactMobile1 = entityVM.EmContactMobile1;
                entity.EmContactFax1 = entityVM.EmContactFax1;
                entity.EmContactEmail = entityVM.EmContactEmail;
                entity.EmContactName2 = entityVM.EmContactName2;
                entity.EmContactRelation2 = entityVM.EmContactRelation2;
                entity.EmContactAddress2 = entityVM.EmContactAddress2;
                entity.EmContactPhone2 = entityVM.EmContactPhone2;
                entity.EmContactMobile2 = entityVM.EmContactMobile2;
                entity.EmContactFax2 = entityVM.EmContactFax2;
                entity.EmContactEmai2 = entityVM.EmContactEmai2;
                entity.CompanyCode = CompanyCode;
                entity.UserInfoEmployeeId = entityVM.UserInfoEmployeeId ?? string.Empty;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac;
                entity.Ldate = DateTime.Now;

                await employeeContactInfoRepository.AddAsync(entity);
                
                await employeeContactInfoRepository.CommitTransactionAsync();
                return true;

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error" + ex.Message);
                await employeeContactInfoRepository.RollbackTransactionAsync();
                return false;
            }

        }

        #endregion

        #region UpdateAsync

        public async Task<bool> UpdateAsync(EmployeeContactInfosSetupViewModel entityVM)
        {
            await employeeContactInfoRepository.BeginTransactionAsync();
            try
            {

                var entity = await employeeContactInfoRepository.GetByIdAsync(entityVM.EmpContactId);
                if (entity == null)
                {
                    await employeeContactInfoRepository.RollbackTransactionAsync();
                    return false;
                }
                entity.AutoId = (decimal)entityVM.AutoId;
                entity.EmpContactId = entityVM.EmpContactId;
                entity.EmployeeId = entityVM.EmployeeId;
                entity.CompanyCode = entityVM.CompanyCode ?? string.Empty;
                entity.BranchCode = entityVM.BranchCode;
                entity.ParmanentAddress = entityVM.ParmanentAddress;
                entity.ParmanentAddressBangla = entityVM.ParmanentAddressBangla;
                entity.ParmanentPostOffice = entityVM.ParmanentPostOffice;
                entity.ParmanentThana = entityVM.ParmanentThana;
                entity.ParmanentPostCode = entityVM.ParmanentPostCode;
                entity.ParmanentDistrict = entityVM.ParmanentDistrict;
                entity.ParmanentPhone = entityVM.ParmanentPhone;
                entity.PresentAddress = entityVM.PresentAddress;
                entity.PresentAddressBangla = entityVM.PresentAddressBangla;
                entity.PresentPostOffice = entityVM.PresentPostOffice;
                entity.PresentThana = entityVM.PresentThana;
                entity.PresentPostCode = entityVM.PresentPostCode;
                entity.PresentDistrict = entityVM.PresentDistrict;
                entity.PresentMobile = entityVM.PresentMobile;
                entity.PresentPhone = entityVM.PresentPhone;
                entity.PresentFax = entityVM.PresentFax;
                entity.PresentEmail = entityVM.PresentEmail;
                entity.PresentEmail = entityVM.PresentEmail;
                entity.EmContactName1 = entityVM.EmContactName1;
                entity.EmContactRelation1 = entityVM.EmContactRelation1;
                entity.EmContactAddress1 = entityVM.EmContactAddress1;
                entity.EmContactPhone1 = entityVM.EmContactPhone1;
                entity.EmContactMobile1 = entityVM.EmContactMobile1;
                entity.EmContactFax1 = entityVM.EmContactFax1;
                entity.EmContactEmail = entityVM.EmContactEmail;
                entity.EmContactName2 = entityVM.EmContactName2;
                entity.EmContactRelation2 = entityVM.EmContactRelation2;
                entity.EmContactAddress2 = entityVM.EmContactAddress2;
                entity.EmContactPhone2 = entityVM.EmContactPhone2;
                entity.EmContactMobile2 = entityVM.EmContactMobile2;
                entity.EmContactFax2 = entityVM.EmContactFax2;
                entity.EmContactEmai2 = entityVM.EmContactEmai2;

                entity.ModifyDate = DateTime.Now;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac;

                await employeeContactInfoRepository.UpdateAsync(entity);
                await employeeContactInfoRepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred : {ex.Message}");
                await employeeContactInfoRepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region GenearateNextCode

        public async Task<string> GenerateNextCode()
        {
            var code = await employeeContactInfoRepository.GetAllAsync();
            var lastCode = code.Max(x => x.EmpContactId);
            int nextCode = 1;
            if (!string.IsNullOrEmpty(lastCode))
            {
                int lastNumber = int.Parse(lastCode.TrimStart('0'));
                lastNumber++;
                nextCode = lastNumber;
            }

            return nextCode.ToString("D4");

        }
        #endregion

        #region Delete

        public async Task<bool> DeleteTab(List<string> ids, DeleteHistoryViewModel model)
        {
            var entity = await employeeContactInfoRepository.All().Where(x => ids.Contains(x.EmpContactId)).ToListAsync();

            if (!entity.Any())
            {
                return false;
            }

            employeeContactInfoRepository.Delete(entity);

            model.tableName = TableName;
            await deleteHistoryService.LogDeletedRecordsAsync(
                entity, model
            );

            return true;
        }

        //public async Task<bool> DeleteLeaveType(List<string> ids, DeleteHistoryViewModel model)
        //{
        //    var entity = GetLeaveType(id);
        //    if (entity != null)
        //    {
        //        employeeContactInfoRepository.Delete(entity);
        //        return true;
        //    }
        //    return false;
        //}

        public HrmEmployeeContactInfo GetLeaveType(string code)
        {
            return employeeContactInfoRepository.GetById(code);
        }

        #endregion

        #region DuplicateCheck
        public async Task<bool> IsExistByAsync(string code, string EmployeeId)
        {
            return await employeeContactInfoRepository.All().AnyAsync(x =>
            x.EmpContactId != code &&
            x.EmployeeId == EmployeeId
            );
        }
        #endregion

        #region GetContactInfoByEmployeeId

        public async Task<EmployeeContactInfosSetupViewModel> GetContactInfoByEmployeeId(string employeeId)
        {
            try
            {
                if (string.IsNullOrEmpty(employeeId)) return null;
                return await (
                    from e in hrmEmployee.All().AsNoTracking()

                    join oi in hrmEmpOffialInfo.All()
                    .Select(x => new { x.EmployeeId, x.DepartmentCode, x.DesignationCode, x.BranchCode }).AsNoTracking()
                    on e.EmployeeId equals oi.EmployeeId into oiGroup
                    from oi in oiGroup.DefaultIfEmpty()

                    join des in designationRepository.All()
                    .Select(x => new { x.DesignationCode, x.DesignationName }).AsNoTracking()
                    on oi.DesignationCode equals des.DesignationCode into desGroup
                    from des in desGroup.DefaultIfEmpty()

                    join dep in departmentRepository.All()
                    .Select(x => new { x.DepartmentCode, x.DepartmentName }).AsNoTracking()
                    on oi.DepartmentCode equals dep.DepartmentCode into depGroup
                    from dep in depGroup.DefaultIfEmpty()

                    join br in coreBranchRepository.All()
                    .Select(x => new { x.BranchCode, x.BranchName }).AsNoTracking()
                    on oi.BranchCode equals br.BranchCode into brGroup
                    from br in brGroup.DefaultIfEmpty()

                    join ec in employeeContactInfoRepository.All().AsNoTracking()
                    on e.EmployeeId equals ec.EmployeeId into ecGroup
                    from ec in ecGroup.DefaultIfEmpty()

                    where e.EmployeeId == employeeId

                    select new EmployeeContactInfosSetupViewModel
                    {
                        // Employee Info
                        EmployeeId = e.EmployeeId,
                        EmployeeName = $"{e.FirstName} {e.LastName}",
                        DesignationName = des != null ? des.DesignationName : "",
                        DepartmentName = dep != null ? dep.DepartmentName : "",
                        BranchCode = br != null ? br.BranchCode : "",
                        CoreBranchName = br != null ? br.BranchName : "",

                        // Hidden fields — ec can be null (left join), so cast nullable
                        AutoId = ec != null ? ec.AutoId : 0,   // ← key fix
                        EmpContactId = ec != null ? ec.EmpContactId : "",
                        Ldate = ec != null ? (DateTime?)ec.Ldate : null,
                        ModifyDate = ec != null ? (DateTime?)ec.ModifyDate : null,

                        // Permanent Address
                        ParmanentAddress = ec != null ? ec.ParmanentAddress ?? "" : "",
                        ParmanentAddressBangla = ec != null ? ec.ParmanentAddressBangla ?? "" : "",
                        ParmanentPostOffice = ec != null ? ec.ParmanentPostOffice ?? "" : "",
                        ParmanentThana = ec != null ? ec.ParmanentThana ?? "" : "",
                        ParmanentPostCode = ec != null ? ec.ParmanentPostCode ?? "" : "",
                        ParmanentDistrict = ec != null ? ec.ParmanentDistrict ?? "" : "",
                        ParmanentPhone = ec != null ? ec.ParmanentPhone ?? "" : "",

                        // Present Address
                        PresentAddress = ec != null ? ec.PresentAddress ?? "" : "",
                        PresentAddressBangla = ec != null ? ec.PresentAddressBangla ?? "" : "",
                        PresentPostOffice = ec != null ? ec.PresentPostOffice ?? "" : "",
                        PresentThana = ec != null ? ec.PresentThana ?? "" : "",
                        PresentPostCode = ec != null ? ec.PresentPostCode ?? "" : "",
                        PresentDistrict = ec != null ? ec.PresentDistrict ?? "" : "",
                        PresentMobile = ec != null ? ec.PresentMobile ?? "" : "",
                        PresentPhone = ec != null ? ec.PresentPhone ?? "" : "",
                        PresentFax = ec != null ? ec.PresentFax ?? "" : "",
                        PresentEmail = ec != null ? ec.PresentEmail ?? "" : "",

                        // Emergency Contact 1
                        EmContactName1 = ec != null ? ec.EmContactName1 ?? "" : "",
                        EmContactRelation1 = ec != null ? ec.EmContactRelation1 ?? "" : "",
                        EmContactAddress1 = ec != null ? ec.EmContactAddress1 ?? "" : "",
                        EmContactMobile1 = ec != null ? ec.EmContactMobile1 ?? "" : "",
                        EmContactPhone1 = ec != null ? ec.EmContactPhone1 ?? "" : "",
                        EmContactFax1 = ec != null ? ec.EmContactFax1 ?? "" : "",
                        EmContactEmail = ec != null ? ec.EmContactEmail ?? "" : "",

                        // Emergency Contact 2
                        EmContactName2 = ec != null ? ec.EmContactName2 ?? "" : "",
                        EmContactRelation2 = ec != null ? ec.EmContactRelation2 ?? "" : "",
                        EmContactAddress2 = ec != null ? ec.EmContactAddress2 ?? "" : "",
                        EmContactMobile2 = ec != null ? ec.EmContactMobile2 ?? "" : "",
                        EmContactPhone2 = ec != null ? ec.EmContactPhone2 ?? "" : "",
                        EmContactFax2 = ec != null ? ec.EmContactFax2 ?? "" : "",
                        EmContactEmai2 = ec != null ? ec.EmContactEmai2 ?? "" : "",

                    }).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }


        public async Task<List<EmployeeContactInfosSetupViewModel>> GetComapnyByCode(string companyCode)
        {
            try
            {
                if (string.IsNullOrEmpty(companyCode)) return new List<EmployeeContactInfosSetupViewModel>();

                return await (
                    from e in hrmEmployee.All().AsNoTracking()
                    where e.CompanyCode == companyCode
                    select new EmployeeContactInfosSetupViewModel
                    {
                        EmployeeId = e.EmployeeId,
                        EmployeeName = $"{e.FirstName} {e.LastName} ({e.EmployeeId})"
                    }).ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        public async Task<List<EmployeeContactInfosSetupViewModel>> GetComapnyByBranchCode(string companyCode)
        {
            try
            {
                if (string.IsNullOrEmpty(companyCode)) return new List<EmployeeContactInfosSetupViewModel>();

                return await (
                    from br in coreBranchRepository.All().AsNoTracking()
                    where br.CompanyCode == companyCode
                    select new EmployeeContactInfosSetupViewModel
                    {
                        BranchCode = br.BranchCode,
                        CoreBranchName = br.BranchName
                    }).ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        #endregion

        #region Permission all type
        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Employee Information System" && x.TitleCheck);
        }

        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Employee Information System" && x.CheckAdd);
        }

        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Employee Information System" && x.CheckEdit);
        }

        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && x.Title == "Employee Information System" && x.CheckDelete);
        }
        #endregion

    }
}
