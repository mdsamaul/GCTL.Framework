using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GCTL.Core.Data;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Accounts;
using GCTL.Core.ViewModels.CustomerWiseApprovalRate;
using GCTL.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GCTL.Service.CustomerWiseApprovalRate
{
    public class CustomerWiseApprovalRateService : ICustomerWiseApprovalRateService
    {
        private readonly IRepository<CustomerWiseCostingEntry> _cwcRepository;
        private readonly IRepository<CustomerWiseCostingDetailsEntry> _cwcdeRepository;
        private readonly IRepository<SalesCustomer> _customerRepository;
        private readonly IRepository<CustomerHandilingCharge> _chcRepository;
        private readonly IRepository<CfDefExpenseType> _expenseTypeRepository;

        public CustomerWiseApprovalRateService(
            IRepository<CustomerWiseCostingEntry> cwcRepository,
            IRepository<CustomerWiseCostingDetailsEntry> cwcdeRepository,
            IRepository<SalesCustomer> customerRepository,
            IRepository<CustomerHandilingCharge> chcRepository,
            IRepository<CfDefExpenseType> expenseTypeRepository)
        {
            _cwcRepository = cwcRepository;
            _cwcdeRepository = cwcdeRepository;
            _customerRepository = customerRepository;
            _chcRepository = chcRepository;
            _expenseTypeRepository = expenseTypeRepository;
        }

        public async Task<string> GetNextCostingIdAsync()
        {
            var costingIds = await _cwcRepository.All().AsNoTracking()
                .Where(x => !string.IsNullOrEmpty(x.CostingId))
                .Select(x => x.CostingId)
                .ToListAsync();

            int maxId = 0;
            foreach (var id in costingIds)
            {
                if (int.TryParse(id, out int num) && num > maxId)
                {
                    maxId = num;
                }
            }

            return (maxId + 1).ToString("D4");
        }

        public async Task<CustomerWiseApprovalRateViewModel?> GetByCustomerIdAsync(string customerId)
        {
            if (string.IsNullOrWhiteSpace(customerId))
                return null;

            var customer = await _customerRepository.All().AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerId == customerId || c.CustomerId == customerId.Trim());

            if (customer == null)
                return null;

            var costing = await _cwcRepository.All().AsNoTracking()
                .OrderByDescending(c => c.Tc)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            var vm = new CustomerWiseApprovalRateViewModel
            {
                CustomerId = customer.CustomerId,
                CustomerName = customer.CustomerName,
                Address = customer.CustomerAddress,
                Phone = customer.Phone,
                Email = customer.Email,
                CostingId = costing?.CostingId,
                MinimumAgencyCommission = costing?.MinimumAgencyCommission,
                OnInvoiceValue = costing?.OnInvoiceValue,
                ExpenseTypeId = costing?.ExpenseTypeId,
                Ldate = costing?.Ldate,
                ModifyDate = costing?.ModifyDate,
                Tc = costing?.Tc ?? 0
            };

            if (costing != null && !string.IsNullOrEmpty(costing.CostingId))
            {
                var detailsList = await _cwcdeRepository.All().AsNoTracking()
                    .Where(d => d.CostingId == costing.CostingId || d.CustomerId == customerId)
                    .ToListAsync();

                if (detailsList.Any())
                {
                    var chcList = await _chcRepository.All().AsNoTracking()
                        .Where(x => !string.IsNullOrEmpty(x.Chcid))
                        .ToListAsync();
                    var chcDict = chcList
                        .GroupBy(x => x.Chcid)
                        .ToDictionary(g => g.Key, g => g.First().Chchead ?? g.Key);

                    var expList = await _expenseTypeRepository.All().AsNoTracking()
                        .Where(x => !string.IsNullOrEmpty(x.ExpenseTypeId))
                        .ToListAsync();
                    var expDict = expList
                        .GroupBy(x => x.ExpenseTypeId)
                        .ToDictionary(g => g.Key, g => g.First().ExpenseType ?? g.Key);

                    vm.Details = detailsList.Select(d => new CustomerWiseApprovalRateDetailViewModel
                    {
                        Tc = d.Tc,
                        CostingDetailsId = d.CostingDetailsId,
                        CostingId = d.CostingId,
                        CustomerId = d.CustomerId,
                        Chcid = d.Chcid,
                        Chchead = (!string.IsNullOrEmpty(d.Chcid) && chcDict.TryGetValue(d.Chcid, out var headName)) ? headName : (d.Chcid ?? string.Empty),
                        ExpenseTypeId = d.ExpenseTypeId,
                        ExpenseTypeName = (!string.IsNullOrEmpty(d.ExpenseTypeId) && expDict.TryGetValue(d.ExpenseTypeId, out var typeName)) ? typeName : (d.ExpenseTypeId ?? string.Empty),
                        BillAmount = d.BillAmount
                    }).ToList();
                }
            }

            return vm;
        }

        public async Task<CommonReturnViewModel> SaveAsync(CustomerWiseApprovalRateViewModel model, UserInfoViewModel? userInfo)
        {
            if (string.IsNullOrWhiteSpace(model.CustomerId))
            {
                return new CommonReturnViewModel { Success = false, Message = "Please select a Customer." };
            }

            await _cwcRepository.BeginTransactionAsync();
            try
            {
                var now = DateTime.Now;
                string username = userInfo?.Username ?? "System";
                string empId = !string.IsNullOrEmpty(userInfo?.EmployeeId) ? userInfo.EmployeeId : username;
                string companyCode = !string.IsNullOrEmpty(userInfo?.CompanyCode) ? userInfo.CompanyCode : "GCTL";
                string ip = userInfo?.IPAddress ?? string.Empty;
                string mac = userInfo?.MacAddress ?? string.Empty;

                var existingMaster = await _cwcRepository.All()
                    .FirstOrDefaultAsync(c => c.CustomerId == model.CustomerId || (!string.IsNullOrEmpty(model.CostingId) && c.CostingId == model.CostingId));

                string costingId = existingMaster?.CostingId ?? model.CostingId ?? string.Empty;
                if (string.IsNullOrWhiteSpace(costingId))
                {
                    costingId = await GetNextCostingIdAsync();
                }

                if (existingMaster != null)
                {
                    existingMaster.CostingId = costingId;
                    existingMaster.CustomerId = model.CustomerId;
                    existingMaster.MinimumAgencyCommission = model.MinimumAgencyCommission ?? 0;
                    existingMaster.OnInvoiceValue = model.OnInvoiceValue ?? 0;
                    existingMaster.ExpenseTypeId = model.ExpenseTypeId;
                    existingMaster.ModifyDate = now;
                    existingMaster.Luser = username;
                    existingMaster.Lip = ip;
                    existingMaster.Lmac = mac;
                    await _cwcRepository.UpdateAsync(existingMaster);
                }
                else
                {
                    var newMaster = new CustomerWiseCostingEntry
                    {
                        CostingId = costingId,
                        CustomerId = model.CustomerId,
                        MinimumAgencyCommission = model.MinimumAgencyCommission ?? 0,
                        OnInvoiceValue = model.OnInvoiceValue ?? 0,
                        ExpenseTypeId = model.ExpenseTypeId,
                        CompanyCode = companyCode,
                        EmployeeId = empId,
                        Luser = username,
                        Ldate = now,
                        Lip = ip,
                        Lmac = mac
                    };
                    await _cwcRepository.AddAsync(newMaster);
                }

                // Sync details
                var existingDetails = await _cwcdeRepository.All()
                    .Where(d => d.CostingId == costingId || d.CustomerId == model.CustomerId)
                    .ToListAsync();

                if (existingDetails.Any())
                {
                    await _cwcdeRepository.DeleteRangeAsync(existingDetails);
                }

                if (model.Details != null && model.Details.Any())
                {
                    var allDetailIds = await _cwcdeRepository.All().AsNoTracking()
                        .Where(x => !string.IsNullOrEmpty(x.CostingDetailsId))
                        .Select(x => x.CostingDetailsId)
                        .ToListAsync();

                    int maxDetailId = 0;
                    foreach (var id in allDetailIds)
                    {
                        if (int.TryParse(id, out int num) && num > maxDetailId)
                        {
                            maxDetailId = num;
                        }
                    }

                    var newDetails = new List<CustomerWiseCostingDetailsEntry>();
                    foreach (var d in model.Details)
                    {
                        maxDetailId++;
                        newDetails.Add(new CustomerWiseCostingDetailsEntry
                        {
                            CostingDetailsId = maxDetailId.ToString("D4"),
                            CostingId = costingId,
                            CustomerId = model.CustomerId,
                            Chcid = d.Chcid ?? string.Empty,
                            ExpenseTypeId = d.ExpenseTypeId,
                            BillAmount = d.BillAmount,
                            CompanyCode = companyCode,
                            EmployeeId = empId,
                            Luser = username,
                            Ldate = now,
                            Lip = ip,
                            Lmac = mac
                        });
                    }
                    await _cwcdeRepository.AddRangeAsync(newDetails);
                }

                await _cwcRepository.CommitTransactionAsync();
                return new CommonReturnViewModel
                {
                    Success = true,
                    Message = "Customer Wise Approval Rate saved successfully.",
                    Data = costingId
                };
            }
            catch (Exception ex)
            {
                await _cwcRepository.RollbackTransactionAsync();
                return new CommonReturnViewModel
                {
                    Success = false,
                    Message = "Error saving approval rate: " + ex.Message
                };
            }
        }

        public async Task<CommonReturnViewModel> DeleteAsync(string costingId, UserInfoViewModel? userInfo)
        {
            if (string.IsNullOrWhiteSpace(costingId))
            {
                return new CommonReturnViewModel { Success = false, Message = "Costing ID is required." };
            }

            await _cwcRepository.BeginTransactionAsync();
            try
            {
                var master = await _cwcRepository.All().FirstOrDefaultAsync(c => c.CostingId == costingId);
                if (master != null)
                {
                    var details = await _cwcdeRepository.All().Where(d => d.CostingId == costingId).ToListAsync();
                    if (details.Any())
                    {
                        await _cwcdeRepository.DeleteRangeAsync(details);
                    }
                    await _cwcRepository.DeleteAsync(master);
                }

                await _cwcRepository.CommitTransactionAsync();
                return new CommonReturnViewModel
                {
                    Success = true,
                    Message = "Customer Wise Approval Rate deleted successfully."
                };
            }
            catch (Exception ex)
            {
                await _cwcRepository.RollbackTransactionAsync();
                return new CommonReturnViewModel
                {
                    Success = false,
                    Message = "Error deleting approval rate: " + ex.Message
                };
            }
        }

        public async Task<CommonReturnViewModel> DeleteMultipleAsync(List<string> costingIds, UserInfoViewModel? userInfo)
        {
            if (costingIds == null || !costingIds.Any())
            {
                return new CommonReturnViewModel { Success = false, Message = "No records selected for deletion." };
            }

            await _cwcRepository.BeginTransactionAsync();
            try
            {
                var masters = await _cwcRepository.All().Where(c => costingIds.Contains(c.CostingId)).ToListAsync();
                if (masters.Any())
                {
                    var details = await _cwcdeRepository.All().Where(d => costingIds.Contains(d.CostingId)).ToListAsync();
                    if (details.Any())
                    {
                        await _cwcdeRepository.DeleteRangeAsync(details);
                    }
                    await _cwcRepository.DeleteRangeAsync(masters);
                }

                await _cwcRepository.CommitTransactionAsync();
                return new CommonReturnViewModel
                {
                    Success = true,
                    Message = $"{masters.Count} record(s) deleted successfully."
                };
            }
            catch (Exception ex)
            {
                await _cwcRepository.RollbackTransactionAsync();
                return new CommonReturnViewModel
                {
                    Success = false,
                    Message = "Error deleting approval rates: " + ex.Message
                };
            }
        }

        public async Task<List<CustomerWiseApprovalRateMasterGridModel>> GetAllMasterListAsync()
        {
            var masters = await _cwcRepository.All().AsNoTracking()
                .OrderByDescending(c => c.CostingId)
                .ToListAsync();

            var custIds = masters.Where(m => !string.IsNullOrEmpty(m.CustomerId)).Select(m => m.CustomerId).Distinct().ToList();

            var customers = await _customerRepository.All().AsNoTracking()
                .Where(c => custIds.Contains(c.CustomerId))
                .ToListAsync();

            var custDict = customers
                .GroupBy(c => c.CustomerId)
                .ToDictionary(g => g.Key, g => g.First());

            return masters.Select(c =>
            {
                string custName = c.CustomerId;
                if (!string.IsNullOrEmpty(c.CustomerId) && custDict.TryGetValue(c.CustomerId, out var cust) && !string.IsNullOrEmpty(cust.CustomerName))
                {
                    custName = $"{cust.CustomerName} ({c.CustomerId})";
                }

                return new CustomerWiseApprovalRateMasterGridModel
                {
                    Id = c.CostingId,
                    CustomerId = c.CustomerId,
                    CustomerName = custName,
                    MinimumAgencyCommission = c.MinimumAgencyCommission.ToString("F2"),
                    OnInvoiceValue = c.OnInvoiceValue.ToString("F2"),
                    User = c.Luser ?? string.Empty
                };
            }).ToList();
        }

        public async Task<List<CommonSelectModel>> GetCustomersAsync()
        {
            return await _customerRepository.All().AsNoTracking()
                .OrderBy(c => c.CustomerName)
                .Select(c => new CommonSelectModel
                {
                    Id = c.CustomerId,
                    Name = !string.IsNullOrEmpty(c.CustomerName) ? $"{c.CustomerName} ({c.CustomerId})" : c.CustomerId
                }).ToListAsync();
        }

        public async Task<List<CommonSelectModel>> GetChcHeadsAsync()
        {
            return await _chcRepository.All().AsNoTracking()
                .OrderBy(c => c.Chchead)
                .Select(c => new CommonSelectModel
                {
                    Id = c.Chcid,
                    Name = c.Chchead ?? c.Chcid
                }).ToListAsync();
        }

        public async Task<List<CommonSelectModel>> GetExpenseTypesAsync()
        {
            return await _expenseTypeRepository.All().AsNoTracking()
                .OrderBy(e => e.ExpenseType)
                .Select(e => new CommonSelectModel
                {
                    Id = e.ExpenseTypeId,
                    Name = e.ExpenseType ?? e.ExpenseTypeId
                }).ToListAsync();
        }
    }
}
