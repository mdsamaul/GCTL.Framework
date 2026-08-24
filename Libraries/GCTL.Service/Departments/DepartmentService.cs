using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Core.ViewModels.Departments;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.DeleteHistories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.Service.Departments
{
    public class DepartmentService : AppService<HrmDefDepartment>, IDepartmentService
    {
        #region Service & Repository
        private readonly IRepository<HrmDefDepartment> departmentRepository;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly ICommonService commonService;
        private readonly IDeleteHistoryService deleteHistoryService;

        string strMaxNO = string.Empty;
        private const string TableName = "HRM_Def_Department";
        private const string ColumnName = "DepartmentCode";

        public DepartmentService(
            IRepository<HrmDefDepartment> departmentRepository,
            IRepository<CoreAccessCode> accessCodeRepository,
            ICommonService commonService,
            IDeleteHistoryService deleteHistoryService
        )
        : base(departmentRepository)
        {
            this.departmentRepository = departmentRepository;
            this.accessCodeRepository = accessCodeRepository;
            this.commonService = commonService;
            this.deleteHistoryService = deleteHistoryService;
        }

        #endregion

        #region GetAllAsync

        public async Task<List<DepartmentSetupViewModel>> GetAllAsync()
        {
            var entity = await departmentRepository.GetAllAsync();
            return entity.Select(entityVM => new DepartmentSetupViewModel
            {
                AutoId = entityVM.AutoId,
                DepartmentCode = entityVM.DepartmentCode,
                DepartmentName = entityVM.DepartmentName,
                DepartmentShortName = entityVM.DepartmentShortName,
                BanglaDepartment = entityVM.BanglaDepartment,
                BanglaShortName = entityVM.BanglaShortName,
                CompanyCode = entityVM.CompanyCode,
                EmployeeId = entityVM.EmployeeId,
                Ldate = entityVM.Ldate,
                ModifyDate = entityVM.ModifyDate,
                Luser = entityVM.Luser,
                Lip = entityVM.Lip,
                Lmac = entityVM.Lmac,
            }).ToList();
        }

        #endregion

        #region GetByIdAsync

        public async Task<DepartmentSetupViewModel> GetByIdAsync(string code)
        {
            var entity = await departmentRepository.GetByIdAsync(code);
            if (entity == null) return null;

            DepartmentSetupViewModel entityVM = new DepartmentSetupViewModel();
            entityVM.AutoId = entity.AutoId;
            entityVM.DepartmentCode = entity.DepartmentCode;
            entityVM.DepartmentName = entity.DepartmentName;
            entityVM.DepartmentShortName = entity.DepartmentShortName;
            entityVM.BanglaDepartment = entity.BanglaDepartment;
            entityVM.BanglaShortName = entity.BanglaShortName;
            entityVM.CompanyCode = entity.CompanyCode;
            entityVM.EmployeeId = entity.EmployeeId;
            entityVM.Luser = entity.Luser;
            entityVM.Ldate = entity.Ldate;
            entityVM.ModifyDate = entity.ModifyDate;
            entityVM.Lip = entity.Lip;
            entityVM.Lmac = entity.Lmac;

            return entityVM;
        }

        #endregion

        #region SelectionDepartmentTypeAsync

        public async Task<IEnumerable<CommonSelectModel>> SelectionDepartmentTypeAsync()
        {
            var data = await departmentRepository.All()
                       .Select(x => new CommonSelectModel
                       {
                           Code = x.DepartmentCode,
                           Name = x.DepartmentName,
                       }).ToListAsync();
            return data;
        }

        #endregion

        #region SaveAsync

        public async Task<bool> SaveAsync(DepartmentSetupViewModel entityVM)
        {
            commonService.FindMaxNo(ref strMaxNO, ColumnName, TableName, 2);
            await departmentRepository.BeginTransactionAsync();
            try
            {
                HrmDefDepartment entity = new HrmDefDepartment();
                entity.DepartmentCode = strMaxNO;
                entity.DepartmentName = entityVM.DepartmentName;
                entity.DepartmentShortName = entityVM.DepartmentShortName;
                entity.BanglaDepartment = entityVM.BanglaDepartment;
                entity.BanglaShortName = entityVM.BanglaShortName;
                entity.CompanyCode = entityVM.CompanyCode ?? string.Empty;
                entity.EmployeeId = entityVM.EmployeeId ?? string.Empty;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac ?? string.Empty;
                entity.Ldate = DateTime.Now;
                await departmentRepository.AddAsync(entity);
                await departmentRepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"error message {ex.Message}");
                await departmentRepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region UpdateAsync

        public async Task<bool> UpdateAsync(DepartmentSetupViewModel entityVM)
        {
            await departmentRepository.BeginTransactionAsync();
            try
            {
                var entity = await departmentRepository.GetByIdAsync(entityVM.DepartmentCode);
                if (entity == null)
                {
                    await departmentRepository.RollbackTransactionAsync();
                    return false;
                }
                entity.DepartmentCode = entityVM.DepartmentCode;
                entity.DepartmentName = entityVM.DepartmentName;
                entity.DepartmentShortName = entityVM.DepartmentShortName;
                entity.BanglaDepartment = entityVM.BanglaDepartment;
                entity.BanglaShortName = entityVM.BanglaShortName;
                entity.CompanyCode = entityVM.CompanyCode ?? string.Empty;
                entity.EmployeeId = entityVM.EmployeeId ?? string.Empty;
                entity.Luser = entityVM.Luser;
                entity.Lip = entityVM.Lip;
                entity.Lmac = entityVM.Lmac;
                entity.ModifyDate = DateTime.Now;
                await departmentRepository.UpdateAsync(entity);
                await departmentRepository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred : {ex.Message}");
                await departmentRepository.RollbackTransactionAsync();
                return false;
            }
        }

        #endregion

        #region DeleteTab

        public async Task<(bool succses, string messege)> DeleteTab(List<string> ids, DeleteHistoryViewModel model)
        {
            if (!ids.Any())
                return (false, "No Data found to delete");

            var alternateColumn = new List<string> { "DepartmentID" };
            var dependencyCheck = await deleteHistoryService.CheckDependenciesAsync(
                departmentRepository.GetTableName(),
                ColumnName,
                ids,
                alternateColumn
            );

            if (!dependencyCheck.CanDelete)
                return (false, dependencyCheck.Message);

            await departmentRepository.BeginTransactionAsync();

            try
            {
                var entity = await departmentRepository.All().Where(x => ids.Contains(x.DepartmentCode)).ToListAsync();

                departmentRepository.Delete(entity);

                model.tableName = departmentRepository.GetTableName();

                await deleteHistoryService.LogDeletedRecordsAsync(
                    entity, model
                );

                await departmentRepository.CommitTransactionAsync();
                return (true, "Deleted Successfully.");
            }
            catch (Exception ex)
            {
                await departmentRepository.RollbackTransactionAsync();
                Console.WriteLine(ex.ToString());
                return (false, "Delete Failed");
            }
        }

        #endregion

        #region Duplicate Check 
        public async Task<bool> IsExistByCodeAsync(string code)
        {
            return await departmentRepository.All().AnyAsync(x => x.DepartmentCode == code);
        }

        public async Task<bool> IsExistAsync(string name)
        {
            return await departmentRepository.All().AnyAsync(x => x.DepartmentName == name);
        }

        public async Task<bool> IsExistAsync(string name, string typeCode)
        {
            return await departmentRepository.All().AnyAsync(x => x.DepartmentName == name && x.DepartmentCode != typeCode);
        }

        #endregion

        #region Permission all type
        public async Task<bool> PagePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && (x.Title == "Department" || x.Title == "Departments") && x.TitleCheck);
        }

        public async Task<bool> SavePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && (x.Title == "Department" || x.Title == "Departments") && x.CheckAdd);
        }

        public async Task<bool> UpdatePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && (x.Title == "Department" || x.Title == "Departments") && x.CheckEdit);
        }

        public async Task<bool> DeletePermissionAsync(string accessCode)
        {
            return await accessCodeRepository.All().AnyAsync(x => x.AccessCodeId == accessCode && (x.Title == "Department" || x.Title == "Departments") && x.CheckDelete);
        }
        #endregion

        #region Legacy Methods
        public List<HrmDefDepartment> GetDepartments()
        {
            return GetAll();
        }

        public HrmDefDepartment GetDepartment(string id)
        {
            return departmentRepository.GetById(id);
        }

        public HrmDefDepartment SaveDepartment(HrmDefDepartment entity)
        {
            if (IsDepartmentExistByCode(entity.DepartmentCode))
                Update(entity);
            else
                Add(entity);

            return entity;
        }

        public async Task<(bool success, bool refSuccess, string message)> DeleteDepartment(List<string> ids, DeleteHistoryViewModel model)
        {
            var res = await DeleteTab(ids, model);
            return (res.succses, !res.succses, res.messege);
        }

        public bool IsDepartmentExistByCode(string code)
        {
            return departmentRepository.All().Any(x => x.DepartmentCode == code);
        }

        public bool IsDepartmentExist(string name)
        {
            return departmentRepository.All().Any(x => x.DepartmentName == name);
        }

        public bool IsDepartmentExist(string name, string typeCode)
        {
            return departmentRepository.All().Any(x => x.DepartmentName == name && x.DepartmentCode != typeCode);
        }

        public IEnumerable<CommonSelectModel> DepartmentSelection()
        {
            return departmentRepository.All()
                .Select(x => new CommonSelectModel
                {
                    Code = x.DepartmentCode,
                    Name = x.DepartmentName
                });
        }

        public bool SavePermission(string accessCode)
        {
            return accessCodeRepository.All().Any(x => x.AccessCodeId == accessCode && (x.Title == "Department" || x.Title == "Departments") && x.CheckAdd);
        }

        public bool UpdatePermission(string accessCode)
        {
            return accessCodeRepository.All().Any(x => x.AccessCodeId == accessCode && (x.Title == "Department" || x.Title == "Departments") && x.CheckEdit);
        }

        public bool DeletePermission(string accessCode)
        {
            return accessCodeRepository.All().Any(x => x.AccessCodeId == accessCode && (x.Title == "Department" || x.Title == "Departments") && x.CheckDelete);
        }
        #endregion
    }
}
