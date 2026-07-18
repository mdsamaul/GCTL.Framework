using GCTL.Core.Data;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.CNFMaster.VendorPrefixs;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.DeleteHistories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace GCTL.Service.CNFMaster.VendorPrefixs
{
    public class VendorPrefixService : IVendorPrefixService
    {
        private readonly IRepository<CfDefVendorPrefix> entryRepository;
        private readonly IRepository<CoreAccessCode> accessRepository;
        private readonly ICommonService commonService;
        private readonly IDeleteHistoryService deleteHistoryService;

        string strMaxNo = string.Empty;
        private const string TableName = "CF_Def_VendorPrefix";
        private const string ColumnName = "VendorPrifixId";

        public VendorPrefixService(IRepository<CfDefVendorPrefix> entryRepository, IRepository<CoreAccessCode> accessRepository, ICommonService commonService, IDeleteHistoryService deleteHistoryService)
        {
            this.entryRepository = entryRepository;
            this.accessRepository = accessRepository;
            this.commonService = commonService;
            this.deleteHistoryService = deleteHistoryService;
        }

        public async Task<CommonReturnViewModel> BulkDeleteAsync(List<decimal> ids, DeleteHistoryViewModel model)
        {
            if (ids == null && !ids.Any())
                return new CommonReturnViewModel
                {
                    Success = false,
                    Message = "No records selected",
                    RefError = false
                };

            var entity = await entryRepository.All().Where(x => ids.Contains(x.Tc)).ToListAsync();

            if (!entity.Any())
            {
                return new CommonReturnViewModel
                {
                    Success = false,
                    Message = "No records found to delete",
                    RefError = false
                };
            }

            var dependencyCheck = await deleteHistoryService.CheckDependenciesAsync(
                TableName,
                ColumnName,
                entity.Select(x => x.VendorPrifixId).Cast<string>().ToList()
            );

            if (dependencyCheck != null && !dependencyCheck.CanDelete)
            {
                return new CommonReturnViewModel
                {
                    Success = false,
                    Message = dependencyCheck.Message,
                    RefError = true
                };
            }

            await entryRepository.BeginTransactionAsync();

            try
            {
                await entryRepository.DeleteRangeAsync(entity);
                model.tableName = TableName;
                await deleteHistoryService.LogDeletedRecordsAsync(entity, model);

                await entryRepository.CommitTransactionAsync();
                return new CommonReturnViewModel
                {
                    Success = true,
                    Message = "Delete Successfully",
                    RefError = false
                };
            }
            catch (Exception ex)
            {
                await entryRepository.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<(List<VendorPrefixSetupVM> Data, int TotalRecords, int filterRecords)> GetPaginatedDataAsync(string searchValue, int page, int pageSize, string sortColumn, string sortDirection)
        {
            var entity = await (from e in entryRepository.All().AsNoTracking()
                                select new VendorPrefixSetupVM
                                {
                                    Tc = e.Tc,
                                    VendorPrefixID = e.VendorPrifixId,
                                    PrifixName = e.PrifixName,
                                    ShortName = e.ShortName
                                }
                    ).ToListAsync();

            var totalRecords = entity.Count;

            IEnumerable<VendorPrefixSetupVM> filterData = entity;

            if (!string.IsNullOrWhiteSpace(searchValue))
            {
                filterData = filterData.Where(d =>
                    (d?.VendorPrefixID?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (d?.PrifixName?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (d?.ShortName?.Contains(searchValue, StringComparison.OrdinalIgnoreCase) ?? false)
                );
            }
            var filterRecords = filterData.Count();

            if(!string.IsNullOrWhiteSpace(sortColumn)&& !string.IsNullOrWhiteSpace(sortDirection))
            {
                filterData = sortColumn.ToLower() switch
                {
                    "vendorprefixid" => sortDirection.ToLower() == "asc" ? filterData.OrderBy(x => x.VendorPrefixID) : filterData.OrderByDescending(x => x.VendorPrefixID),
                    "prifixname" => sortDirection.ToLower() == "asc" ? filterData.OrderBy(x => x.PrifixName) : filterData.OrderByDescending(x => x.PrifixName),
                    "shortname" => sortDirection.ToLower() == "asc" ? filterData.OrderBy(x => x.ShortName) : filterData.OrderByDescending(x => x.ShortName),
                    _ => filterData.OrderByDescending(d => d.Tc)
                };
            }
            else
            {
                filterData = filterData.OrderByDescending(d => d.Tc);
            }

            var data = pageSize < 0 ? filterData.ToList()
                : filterData.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return (data, totalRecords, filterRecords);
        }

        public async Task<CommonReturnViewModel> SaveAsync(VendorPrefixSetupVM model)
        {
            commonService.FindMaxNo(ref strMaxNo, ColumnName, TableName, 2);
            await entryRepository.BeginTransactionAsync();
            try
            {

                CfDefVendorPrefix entity = new CfDefVendorPrefix();
                entity.VendorPrifixId = strMaxNo;
                entity.PrifixName = model.PrifixName;
                entity.ShortName = model.ShortName ?? "";
                entity.Luser = model.Luser;
                entity.Lip = model.Lip;
                entity.Lmac = model.Lmac ?? string.Empty;
                entity.Ldate = DateTime.Now;
                await entryRepository.AddAsync(entity);
                await entryRepository.CommitTransactionAsync();
                return new CommonReturnViewModel
                {
                    Success = true,
                    Message = "Save Successfully"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"error message {ex.Message}");
                await entryRepository.RollbackTransactionAsync();
                return new CommonReturnViewModel
                {
                    Success = false,
                    Message = "Save Failed." 
                };
            }
        }

        public async Task<CommonReturnViewModel> EditAsync(VendorPrefixSetupVM model)
        {
            await entryRepository.BeginTransactionAsync();
            try
            {

                var entity = await entryRepository.GetByIdAsync(model.Tc);
                if (entity == null)
                {
                    await entryRepository.RollbackTransactionAsync();
                    return new CommonReturnViewModel
                    {
                        Success = false,
                        Message = "Update Failed."
                    };
                }
                //entity.BenefitType = entityVM.BenefitType;
                entity.VendorPrifixId = model.VendorPrefixID;
                entity.ShortName = model.ShortName ?? "";
                entity.Luser = model.Luser;
                entity.Lip = model.Lip;
                entity.Lmac = model.Lmac;
                entity.ModifyDate = DateTime.Now;
                await entryRepository.UpdateAsync(entity);
                await entryRepository.CommitTransactionAsync();
                return new CommonReturnViewModel
                {
                    Success = false,
                    Message = "Update successfully"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred : {ex.Message}");
                await entryRepository.RollbackTransactionAsync();
                return new CommonReturnViewModel
                {
                    Success = false,
                    Message = "Update Failed"
                };
            }
        }

        public Task<VendorPrefixSetupVM> GetByIdAsync(decimal id)
        {
            throw new NotImplementedException();
        }

        public Task<CommonReturnViewModel> HasDuplicate(string name, string id = null)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SavePermissionAsync(string accessCode)
        {
            throw new NotImplementedException();
        }

        public Task<bool> PagePermissionAsync(string accessCode)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdatePermissionAsync(string accessCode)
        {
            throw new NotImplementedException();
        }
        
        public Task<bool> DeletePermissionAsync(string accessCode)
        {
            throw new NotImplementedException();
        }

    }
}
