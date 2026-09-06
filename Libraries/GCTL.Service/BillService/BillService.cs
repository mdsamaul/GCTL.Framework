using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GCTL.Core.Data;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Accounts;
using GCTL.Core.ViewModels.ClearAndF.BillEntry;
using GCTL.Core.ViewModels.ClearAndF.Update;
using GCTL.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GCTL.Service.BillService
{
    public class BillService : IBillService
    {
        private readonly IRepository<GCTL.Data.Models.SepDocumentation> _sepDocumentationRepository;
        private readonly IRepository<CfBillEntry> _billEntryRepository;
        private readonly IRepository<CfDefExpenseType> _shipmentModeRepository;
        private readonly IRepository<SalesCustomer> _customerRepository;
        private readonly IRepository<CfOperationalFundRequisitionDetail> _ofrdRepository;
        private readonly IRepository<CfDefExpenseHead> _expHeadRepository;
        private readonly IRepository<GCTL.Data.Models.CoreServiceType> _stRepository;
        private readonly IRepository<CustomerWiseCostingDetailsEntry> _cwcdeRepository;
        private readonly IRepository<CustomerWiseCostingEntry> _cwcRepository;
        private readonly IRepository<CustomerHandilingCharge> _cRepository;

        public BillService(
            IRepository<GCTL.Data.Models.SepDocumentation> sepDocumentationRepository,
            IRepository<CfBillEntry> billEntryRepository,
            IRepository<CfDefExpenseType> shipmentModeRepository,
            IRepository<SalesCustomer> customerRepository,
            IRepository<CfOperationalFundRequisitionDetail> ofrdRepository,
            IRepository<CfDefExpenseHead> expHeadRepository,
            IRepository<GCTL.Data.Models.CoreServiceType> stRepository,
            IRepository<CustomerWiseCostingDetailsEntry> cwcdeRepository,
            IRepository<CustomerWiseCostingEntry> cwcRepository,
            IRepository<CustomerHandilingCharge> cRepository)
        {
            _sepDocumentationRepository = sepDocumentationRepository;
            _billEntryRepository = billEntryRepository;
            _shipmentModeRepository = shipmentModeRepository;
            _customerRepository = customerRepository;
            _ofrdRepository = ofrdRepository;
            _expHeadRepository = expHeadRepository;
            _stRepository = stRepository;
            _cwcdeRepository = cwcdeRepository;
            _cwcRepository = cwcRepository;
            _cRepository = cRepository;
        }

        public async Task<SepPagedResult<JobDto>> GetBillPagedAsync(int page, int pageSize, string? customerName, string? shipmentMode, string? dateFrom, string? dateTo, string? search)
        {
            try
            {
                var billedJobNos = _billEntryRepository.All()
                    .Where(s => !string.IsNullOrEmpty(s.JobNo))
                    .Select(s => s.JobNo);

                var query = from d in _sepDocumentationRepository.All().AsNoTracking()
                            where !billedJobNos.Contains(d.JobNo)
                            join c in _customerRepository.All().AsNoTracking()
                                on d.CustomerId equals c.CustomerId into customers
                            from c in customers.DefaultIfEmpty()
                            join s in _shipmentModeRepository.All().AsNoTracking()
                                on d.ExpenseTypeId equals s.ExpenseTypeId into modes
                            from s in modes.DefaultIfEmpty()
                            select new
                            {
                                d.Tc,
                                d.JobNo,
                                ExpenseTypeId = d.ExpenseTypeId,
                                ShipmentMode = s != null ? s.ExpenseType : string.Empty,
                                d.Date,
                                CustomerId = d.CustomerId,
                                CustomerName = c != null ? c.CustomerName : string.Empty,
                                d.DocReceivedDate
                            };

                // Apply filters
                if (!string.IsNullOrWhiteSpace(customerName))
                    query = query.Where(j => j.CustomerId == customerName);

                if (!string.IsNullOrWhiteSpace(shipmentMode))
                    query = query.Where(j => j.ExpenseTypeId == shipmentMode);

                if (DateTime.TryParse(dateFrom, out var fromDate))
                    query = query.Where(j => j.Date >= fromDate);

                if (DateTime.TryParse(dateTo, out var toDate))
                {
                    var toDateEnd = toDate.Date.AddDays(1);
                    query = query.Where(j => j.Date < toDateEnd);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim();
                    query = query.Where(j =>
                        (j.JobNo != null && j.JobNo.Contains(term)) ||
                        (j.CustomerName != null && j.CustomerName.Contains(term)) ||
                        (j.ShipmentMode != null && j.ShipmentMode.Contains(term)));
                }

                var totalCount = await query.CountAsync();

                var rawData = await query
                    .OrderByDescending(j => j.Tc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var data = rawData.Select(h => new JobDto
                {
                    Id = (int)h.Tc,
                    JobNo = h.JobNo ?? string.Empty,
                    ShipmentMode = h.ShipmentMode,
                    JobDate = h.Date.HasValue ? h.Date.Value.ToString("dd/MM/yyyy") : string.Empty,
                    CustomerName = h.CustomerName,
                    DocReceivedDate = h.DocReceivedDate.HasValue ? h.DocReceivedDate.Value.ToString("dd/MM/yyyy") : string.Empty
                }).ToList();

                return new SepPagedResult<JobDto>
                {
                    Data = data,
                    TotalRecords = totalCount,
                    FilteredRecords = totalCount
                };
            }
            catch (Exception)
            {
                return new SepPagedResult<JobDto>
                {
                    Data = new List<JobDto>(),
                    TotalRecords = 0,
                    FilteredRecords = 0
                };
            }
        }

        public async Task<BillEntryJobVM> GetJobDetailAsync(decimal id)
        {
            var job = await _sepDocumentationRepository.All().AsNoTracking()
                .Where(j => j.Tc == id)
                .Select(h => new
                {
                    h.Tc,
                    h.JobNo,
                    h.ExpenseTypeId,
                    h.IsCustomJobNo,
                    h.Date,
                    h.CustomerId,
                    h.DeliveryLocationCode,
                    h.PortId,
                    h.DocReceivedDate,
                    h.ExpNo,
                    h.ExpDate,
                    h.Lcvalue,
                    h.Ipno,
                    h.Ipdate,
                    h.ImporterId,
                    h.InvoiceNo,
                    h.InvoiceDate,
                    h.InvoiceValue,
                    h.InvoiceCurrencyId,
                    h.Beno,
                    h.Bedate,
                    h.Bldate,
                    h.ContainerNo,
                    h.ContainerSize,
                    h.Lcano,
                    h.Dischargedate,
                    h.MaterialDescription,
                    h.Quntity1,
                    h.Unit1,
                    h.Quntity2,
                    h.Unit2,
                    h.NameOfFreightForwarder,
                    h.VesselRottNo,
                    h.Remarks,
                    h.Ldate,
                    h.ModifyDate,
                    h.BillNo,
                    h.CustomerAddress,
                    h.Lcno,
                    h.Lcdate,
                    h.CurrencyId,
                    h.Bdtrate,
                    h.ConsoleNo,
                    h.MinimumAgencyCom,
                    h.CommOnInvoiceValue
                })
                .FirstOrDefaultAsync();

            if (job == null)
                throw new Exception("Job not found");

            return new BillEntryJobVM
            {
                Id = (int)job.Tc,
                JobNo = job.JobNo ?? string.Empty,
                ShipmentMode = job.ExpenseTypeId ?? string.Empty,
                ShipmentModeId = job.ExpenseTypeId,
                IsCustomJobNo = job.IsCustomJobNo ?? string.Empty,
                JobDate = job.Date.HasValue ? job.Date.Value.ToString("yyyy-MM-dd") : string.Empty,
                CustomerId = job.CustomerId,
                CustomerName = job.CustomerId ?? string.Empty,
                CustomerDeliveryAddress = job.DeliveryLocationCode ?? string.Empty,
                PortId = job.PortId,
                DocsReceivedDate = job.DocReceivedDate.HasValue ? job.DocReceivedDate.Value.ToString("yyyy-MM-dd") : string.Empty,
                LcExpNo = job.ExpNo ?? string.Empty,
                LcValue = job.Lcvalue,
                IpEpNo = job.Ipno ?? string.Empty,
                IpDate = job.Ipdate.HasValue ? job.Ipdate.Value.ToString("yyyy-MM-dd") : string.Empty,
                ImporterId = job.ImporterId,
                InvoiceNo = job.InvoiceNo ?? string.Empty,
                InvoiceDate = job.InvoiceDate.HasValue ? job.InvoiceDate.Value.ToString("yyyy-MM-dd") : string.Empty,
                BlNo = job.Beno ?? string.Empty,
                BlDate = job.Bldate.HasValue ? job.Bldate.Value.ToString("yyyy-MM-dd") : string.Empty,
                BeNo = job.Beno ?? string.Empty,
                BeDate = job.Bedate.HasValue ? job.Bedate.Value.ToString("yyyy-MM-dd") : string.Empty,
                ContainerNo = job.ContainerNo ?? string.Empty,
                ContainerSize = job.ContainerSize ?? string.Empty,
                LcaNo = job.Lcano ?? string.Empty,
                DischargeDate = job.Dischargedate.HasValue ? job.Dischargedate.Value.ToString("yyyy-MM-dd") : string.Empty,
                MaterialDescription = job.MaterialDescription ?? string.Empty,
                Quantity = job.Quntity1,
                QuantityUnitId = job.Unit1,
                Weight = job.Quntity2,
                WeightUnitId = job.Unit2,
                ForwarderId = job.NameOfFreightForwarder,
                VesselRottNo = job.VesselRottNo ?? string.Empty,
                Remarks = job.Remarks ?? string.Empty,
                DocReceivedDate = job.DocReceivedDate.HasValue ? job.DocReceivedDate.Value.ToString("yyyy-MM-dd") : string.Empty,
                CreatedAt = job.Ldate,
                UpdatedAt = job.ModifyDate ?? job.Ldate,
                BillNo = job.BillNo ?? string.Empty,
                BillDate = string.Empty,
                CustomerAddress = job.CustomerAddress ?? string.Empty,
                ExpDate = job.ExpDate.HasValue ? job.ExpDate.Value.ToString("yyyy-MM-dd") : string.Empty,
                LcNo = job.Lcno ?? string.Empty,
                LcDate = job.Lcdate.HasValue ? job.Lcdate.Value.ToString("yyyy-MM-dd") : string.Empty,
                LcValCurrencyId = job.CurrencyId ?? string.Empty,
                BdtRate = job.Bdtrate,
                BdtTotal = (job.Lcvalue ?? 0m) * (job.Bdtrate ?? 0m),
                InvoiceValue = job.InvoiceValue,
                InvValCurrencyId = job.InvoiceCurrencyId ?? string.Empty,
                ConsoleNo = job.ConsoleNo ?? string.Empty,
                DestinationId = string.Empty,
                AgComm = job.MinimumAgencyCom,
                OnInvVal = job.CommOnInvoiceValue,
                Amount = null,
                HeadId = string.Empty
            };
        }

        public async Task<List<ExpenseDetailDto>> GetExpenseDetailsAsync(decimal id)
        {
            var job = await _sepDocumentationRepository.All()
                .Where(e => e.Tc == id)
                .Select(h => new { h.Tc, h.JobNo })
                .FirstOrDefaultAsync();

            if (job == null || string.IsNullOrEmpty(job.JobNo))
            {
                return new List<ExpenseDetailDto>();
            }

            var query = from fr in _ofrdRepository.All().AsNoTracking()
                        where fr.JobNo == job.JobNo
                        join eh in _expHeadRepository.All().AsNoTracking()
                            on fr.ExpenseHeadId equals eh.ExpenseHeadId into expHeads
                        from eh in expHeads.DefaultIfEmpty()
                        join st in _stRepository.All().AsNoTracking()
                            on fr.ServiceTypeId equals st.ServiceTypeId into serviceTypes
                        from st in serviceTypes.DefaultIfEmpty()
                        select new ExpenseDetailDto
                        {
                            Head = eh != null ? eh.ExpenseHead : string.Empty,
                            IsRec = eh != null ? (eh.IsReceiptable ?? string.Empty) : string.Empty,
                            ServiceType = st != null ? st.ServiceTypeName : string.Empty,
                            ConfirmAmt = fr.ActualAmount,
                            BillAmt = fr.ActualAmount
                        };

            return await query.ToListAsync();
        }

        public async Task<List<ChargeTableItemDto>> GetChargeTableDataAsync(decimal id)
        {
            var job = await _sepDocumentationRepository.All().AsNoTracking()
                .Where(e => e.Tc == id)
                .Select(h => new
                {
                    h.Tc,
                    h.JobNo,
                    CustomerId = h.CustomerId,
                    ShipmentMode = h.ExpenseTypeId
                })
                .FirstOrDefaultAsync();

            if (job == null || string.IsNullOrEmpty(job.JobNo))
            {
                return new List<ChargeTableItemDto>();
            }

            var query = from cwde in _cwcdeRepository.All().AsNoTracking()
                        where cwde.CustomerId == job.CustomerId && cwde.ExpenseTypeId == job.ShipmentMode
                        join chc in _cRepository.All().AsNoTracking()
                            on cwde.Chcid equals chc.Chcid into gj
                        from subChc in gj.DefaultIfEmpty()
                        select new ChargeTableItemDto
                        {
                            IsRec = subChc != null ? (subChc.IsReceiptable ?? string.Empty) : string.Empty,
                            Chc = subChc != null ? subChc.Chchead : string.Empty,
                            Description = subChc != null ? subChc.ChcheadDescription : string.Empty,
                            BillAmt = cwde.BillAmount,
                            Tc = cwde.Tc
                        };

            return await query.ToListAsync();
        }

        public async Task<CommonReturnViewModel> AddCostAsync(AddCostRequestDto request, UserInfoViewModel? userInfo)
        {
            await _cwcRepository.BeginTransactionAsync();

            try
            {
                // Find CHCID from headId (CustomerHandilingCharge.Tc)
                var head = await _cRepository.All()
                    .Where(e => e.Tc == request.HeadId)
                    .Select(e => e.Chcid)
                    .FirstOrDefaultAsync();

                // Find job info from SepDocumentation
                var sep = await _sepDocumentationRepository.All()
                    .Where(e => e.Tc == request.Tc)
                    .Select(e => new
                    {
                        tc = e.Tc,
                        jobNo = e.JobNo,
                        customerId = e.CustomerId,
                        expenseType = e.ExpenseTypeId
                    })
                    .FirstOrDefaultAsync();

                if (sep == null)
                {
                    await _cwcRepository.RollbackTransactionAsync();
                    return new CommonReturnViewModel { Success = false, Message = "Job not found" };
                }

                var now = DateTime.Now;
                string username = userInfo?.Username ?? "System";
                string empId = !string.IsNullOrEmpty(userInfo?.EmployeeId) ? userInfo.EmployeeId : username;
                string companyCode = !string.IsNullOrEmpty(userInfo?.CompanyCode) ? userInfo.CompanyCode : "GCTL";
                string ip = userInfo?.IPAddress ?? string.Empty;
                string mac = userInfo?.MacAddress ?? string.Empty;

                var lastEntry = await _cwcdeRepository.All().OrderByDescending(x => x.Tc).FirstOrDefaultAsync();
                var lastEntry11 = await _cwcRepository.All().OrderByDescending(x => x.Tc).FirstOrDefaultAsync();

                string nextDetailsId = "0001";
                if (lastEntry != null && int.TryParse(lastEntry.CostingDetailsId, out int parsedDetailsId))
                {
                    nextDetailsId = (parsedDetailsId + 1).ToString("D4");
                }

                string nextCostingId = "0001";
                if (lastEntry11 != null && int.TryParse(lastEntry11.CostingId, out int parsedCostingId))
                {
                    nextCostingId = (parsedCostingId + 1).ToString("D4");
                }

                var entry1 = new CustomerWiseCostingEntry
                {
                    CostingId = nextCostingId,
                    CustomerId = sep.customerId ?? string.Empty,
                    ExpenseTypeId = sep.expenseType ?? string.Empty,
                    CompanyCode = companyCode,
                    EmployeeId = empId,
                    Luser = username,
                    Ldate = now,
                    Lip = ip,
                    Lmac = mac
                };

                await _cwcRepository.AddAsync(entry1);

                var entry = new CustomerWiseCostingDetailsEntry
                {
                    CostingDetailsId = nextDetailsId,
                    BillAmount = request.Cost,
                    CostingId = entry1.CostingId,
                    CustomerId = sep.customerId,
                    Chcid = head ?? string.Empty,
                    CompanyCode = companyCode,
                    ExpenseTypeId = sep.expenseType,
                    EmployeeId = empId,
                    Luser = username,
                    Ldate = now,
                    Lip = ip,
                    Lmac = mac
                };

                await _cwcdeRepository.AddAsync(entry);
                await _cwcRepository.CommitTransactionAsync();

                return new CommonReturnViewModel
                {
                    Success = true,
                    Message = "Cost added successfully",
                    Data = sep.tc
                };
            }
            catch (Exception ex)
            {
                await _cwcRepository.RollbackTransactionAsync();
                return new CommonReturnViewModel
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<CommonReturnViewModel> DeleteChargeAsync(decimal tc, UserInfoViewModel? userInfo)
        {
            try
            {
                var entry = await _cwcdeRepository.All()
                    .FirstOrDefaultAsync(e => e.Tc == tc);

                if (entry == null)
                {
                    return new CommonReturnViewModel
                    {
                        Success = false,
                        Message = "Charge entry not found"
                    };
                }

                await _cwcdeRepository.DeleteAsync(entry);

                return new CommonReturnViewModel
                {
                    Success = true,
                    Message = "Deleted successfully"
                };
            }
            catch (Exception ex)
            {
                return new CommonReturnViewModel
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<SepPagedResult<BillHistoryDto>> GetBillHistoryPagedAsync(int page, int pageSize, string? search)
        {
            try
            {
                var query = from b in _billEntryRepository.All().AsNoTracking()
                            join s in _shipmentModeRepository.All().AsNoTracking()
                                on b.ExpenseTypeId equals s.ExpenseTypeId into modes
                            from s in modes.DefaultIfEmpty()
                            select new
                            {
                                b.Tc,
                                b.BillNo,
                                b.BillDate,
                                b.JobNo,
                                ShipmentMode = s != null ? s.ExpenseType : string.Empty
                            };

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = search.Trim().ToLower();
                    query = query.Where(x =>
                        (x.BillNo != null && x.BillNo.ToLower().Contains(term)) ||
                        (x.JobNo != null && x.JobNo.ToLower().Contains(term)) ||
                        (x.ShipmentMode != null && x.ShipmentMode.ToLower().Contains(term)));
                }

                var totalCount = await query.CountAsync();

                var rawData = await query
                    .OrderByDescending(x => x.Tc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var data = rawData.Select(x => new BillHistoryDto
                {
                    Tc = x.Tc,
                    BillNo = x.BillNo ?? string.Empty,
                    BillDate = x.BillDate.HasValue ? x.BillDate.Value.ToString("dd/MM/yyyy") : string.Empty,
                    JobNo = x.JobNo ?? string.Empty,
                    ShipmentMode = x.ShipmentMode
                }).ToList();

                return new SepPagedResult<BillHistoryDto>
                {
                    Data = data,
                    TotalRecords = totalCount,
                    FilteredRecords = totalCount
                };
            }
            catch (Exception)
            {
                return new SepPagedResult<BillHistoryDto>
                {
                    Data = new List<BillHistoryDto>(),
                    TotalRecords = 0,
                    FilteredRecords = 0
                };
            }
        }
    }
}
