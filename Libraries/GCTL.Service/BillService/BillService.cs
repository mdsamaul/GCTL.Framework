using GCTL.Core.Data;
using GCTL.Core.DataTables;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.Accounts;
using GCTL.Core.ViewModels.ClearAndF.BillEntry;
using GCTL.Core.ViewModels.ClearAndF.Update;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using System.Globalization;

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
        private readonly IRepository<CfBillEntryDetail> _billDetailRepository;
        private readonly IRepository<CfBillEntryDetailsChchead> _billChcRepository;

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
            IRepository<CustomerHandilingCharge> cRepository,
            IRepository<CfBillEntryDetail> billDetailRepository,
            IRepository<CfBillEntryDetailsChchead> billChcRepository)
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
            _billDetailRepository = billDetailRepository;
            _billChcRepository = billChcRepository;
        }

        public async Task<PaginationResponse<JobDto>> GetBillPagedAsync(PaginationRequest request, string? customerName, string? shipmentMode, string? dateFrom, string? dateTo)
        {
            try
            {
                var billedJobNos = _billEntryRepository.All()
                    .Where(s => !string.IsNullOrEmpty(s.JobNo))
                    .Select(s => s.JobNo);

                var baseQuery = from d in _sepDocumentationRepository.All().AsNoTracking()
                                where !billedJobNos.Contains(d.JobNo)
                                join c in _customerRepository.All().AsNoTracking()
                                    on d.CustomerId equals c.CustomerId into customers
                                from c in customers.DefaultIfEmpty()
                                join s in _shipmentModeRepository.All().AsNoTracking()
                                    on d.ExpenseTypeId equals s.ExpenseTypeId into modes
                                from s in modes.DefaultIfEmpty()
                                select new
                                {
                                    Tc = d.Tc,
                                    JobNo = d.JobNo ?? string.Empty,
                                    ShipmentMode = s != null ? s.ExpenseType : string.Empty,
                                    ExpenseTypeId = d.ExpenseTypeId,
                                    Date = d.Date,
                                    CustomerId = d.CustomerId,
                                    CustomerName = c != null ? c.CustomerName : string.Empty,
                                    CustomerAddress = c != null ? c.CustomerAddress : (d.CustomerAddress ?? string.Empty),
                                    DocReceivedDate = d.DocReceivedDate,
                                    Invoice = d.InvoiceNo ?? string.Empty,
                                    Beno = d.Beno ?? string.Empty
                                };

                // Apply custom dropdown / date filters
                if (!string.IsNullOrWhiteSpace(customerName))
                    baseQuery = baseQuery.Where(j => j.CustomerId == customerName);

                if (!string.IsNullOrWhiteSpace(shipmentMode))
                    baseQuery = baseQuery.Where(j => j.ExpenseTypeId == shipmentMode);

                var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "d-M-yyyy", "dd-MM-yyyy" };
                if (!string.IsNullOrWhiteSpace(dateFrom) && DateTime.TryParseExact(dateFrom.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fromDate))
                    baseQuery = baseQuery.Where(j => j.Date >= fromDate);

                if (!string.IsNullOrWhiteSpace(dateTo) && DateTime.TryParseExact(dateTo.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var toDate))
                {
                    var toDateEnd = toDate.Date.AddDays(1);
                    baseQuery = baseQuery.Where(j => j.Date < toDateEnd);
                }

                // Project into JobDto for PaginationService
                var query = baseQuery.Select(h => new JobDto
                {
                    Id = (int)h.Tc,
                    JobNo = h.JobNo,
                    ShipmentMode = h.ShipmentMode,
                    JobDate = h.Date,
                    CustomerName = h.CustomerName,
                    CustomerAddress = h.CustomerAddress,
                    DocReceivedDate = h.DocReceivedDate,
                    Invoice = h.Invoice,
                    Beno = h.Beno
                });

                if (string.IsNullOrEmpty(request.SortColumn))
                {
                    request.SortColumn = nameof(JobDto.Id);
                    request.SortOrder = "desc";
                }

                return await PaginationService<JobDto>.GetPaginatedData(query, request);
            }
            catch (Exception)
            {
                return new PaginationResponse<JobDto>
                {
                    Data = new List<JobDto>(),
                    TotalCount = 0,
                    FilteredCount = 0
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
                    h.CommOnInvoiceValue,
                    h.AssessableRate,
                    h.AssessableValue,
                    h.UndertakingNo,
                    h.Ata,
                    h.Shiparriveddate,
                    h.Hawb,
                    h.PackingQuantity,
                    h.PackingUnitType,
                    h.Eta,
                    h.DateOfClearence,
                    h.UnStuffingLocationDate,
                    h.PlaceOfLoadingId,
                    h.Etddate,
                    h.ShedYardId,
                    h.FreightCharge
                })
                .FirstOrDefaultAsync();

            if (job == null)
                throw new Exception("Job not found");

            return new BillEntryJobVM
            {
                AgComm = job.MinimumAgencyCom,
                Amount = null,
                BdtRate = job.Bdtrate,
                BdtTotal = (job.Lcvalue ?? 0m) * (job.Bdtrate ?? 0m),
                BeDate = job.Bedate.HasValue ? job.Bedate.Value.ToString("yyyy-MM-dd") : string.Empty,
                BeNo = job.Beno ?? string.Empty,
                BillDate = string.Empty,
                BillNo = job.JobNo ?? string.Empty,
                BlDate = job.Bldate.HasValue ? job.Bldate.Value.ToString("yyyy-MM-dd") : string.Empty,
                BlNo = job.BillNo ?? string.Empty,
                ConsoleNo = job.ConsoleNo ?? string.Empty,
                ContainerNo = job.ContainerNo ?? string.Empty,
                ContainerSize = job.ContainerSize ?? string.Empty,
                CreatedAt = job.Ldate,
                CustomerAddress = job.CustomerAddress ?? string.Empty,
                CustomerDeliveryAddress = job.DeliveryLocationCode ?? string.Empty,
                CustomerId = job.CustomerId,
                CustomerName = job.CustomerId ?? string.Empty,
                DestinationId = string.Empty,
                DischargeDate = job.Dischargedate.HasValue ? job.Dischargedate.Value.ToString("yyyy-MM-dd") : string.Empty,
                DocReceivedDate = job.DocReceivedDate.HasValue ? job.DocReceivedDate.Value.ToString("yyyy-MM-dd") : string.Empty,
                DocsReceivedDate = job.DocReceivedDate.HasValue ? job.DocReceivedDate.Value.ToString("yyyy-MM-dd") : string.Empty,
                ExpDate = job.ExpDate.HasValue ? job.ExpDate.Value.ToString("yyyy-MM-dd") : string.Empty,
                ForwarderId = job.NameOfFreightForwarder,
                HeadId = string.Empty,
                Id = (int)job.Tc,
                ImporterId = job.ImporterId,
                InvoiceDate = job.InvoiceDate.HasValue ? job.InvoiceDate.Value.ToString("yyyy-MM-dd") : string.Empty,
                InvoiceNo = job.InvoiceNo ?? string.Empty,
                InvoiceValue = job.InvoiceValue,
                InvValCurrencyId = job.InvoiceCurrencyId ?? string.Empty,
                IpDate = job.Ipdate.HasValue ? job.Ipdate.Value.ToString("yyyy-MM-dd") : string.Empty,
                IpEpNo = job.Ipno ?? string.Empty,
                IsCustomJobNo = job.IsCustomJobNo ?? string.Empty,
                JobDate = job.Date.HasValue ? job.Date.Value.ToString("yyyy-MM-dd") : string.Empty,
                JobNo = job.JobNo ?? string.Empty,
                LcaNo = job.Lcano ?? string.Empty,
                LcDate = job.Lcdate.HasValue ? job.Lcdate.Value.ToString("yyyy-MM-dd") : string.Empty,
                LcExpNo = job.ExpNo ?? string.Empty,
                LcNo = job.Lcno ?? string.Empty,
                LcValCurrencyId = job.CurrencyId ?? string.Empty,
                LcValue = job.Lcvalue,
                MaterialDescription = job.MaterialDescription ?? string.Empty,
                OnInvVal = job.CommOnInvoiceValue,
                PortId = job.PortId,
                Quantity = job.Quntity1,
                QuantityUnitId = job.Unit1,
                Remarks = job.Remarks ?? string.Empty,
                ShipmentMode = job.ExpenseTypeId ?? string.Empty,
                ShipmentModeId = job.ExpenseTypeId,
                UpdatedAt = job.ModifyDate ?? job.Ldate,
                VesselRottNo = job.VesselRottNo ?? string.Empty,
                Weight = job.Quntity2,
                WeightUnitId = job.Unit2,

                InvBdtRate = job.AssessableRate,
                InvBdtTotal = job.AssessableValue ?? ((job.InvoiceValue ?? 0m) * (job.AssessableRate ?? 0m)),
                NoTransport = null,
                DemarageDay = null,
                UndertakingNo = job.UndertakingNo,
                ArrivalDate = job.Ata.HasValue ? job.Ata.Value.ToString("yyyy-MM-dd") : (job.Shiparriveddate.HasValue ? job.Shiparriveddate.Value.ToString("yyyy-MM-dd") : string.Empty),
                EfrNo = string.Empty,
                EfrDate = string.Empty,
                RotNo = job.VesselRottNo ?? string.Empty,
                Hawb = job.Hawb ?? string.Empty,
                PackQty = job.PackingQuantity,
                PackQtyUnitId = job.PackingUnitType ?? string.Empty,
                EtaDeliveryDate = job.Eta.HasValue ? job.Eta.Value.ToString("yyyy-MM-dd") : string.Empty,
                ActualDeliveryDate = job.DateOfClearence.HasValue ? job.DateOfClearence.Value.ToString("yyyy-MM-dd") : (job.Ata.HasValue ? job.Ata.Value.ToString("yyyy-MM-dd") : string.Empty),
                UnstuffingDate = job.UnStuffingLocationDate.HasValue ? job.UnStuffingLocationDate.Value.ToString("yyyy-MM-dd") : string.Empty,
                PlaceOfLoadingId = job.PlaceOfLoadingId ?? string.Empty,
                EtdDate = job.Etddate.HasValue ? job.Etddate.Value.ToString("yyyy-MM-dd") : string.Empty,
                ShedYardId = job.ShedYardId ?? string.Empty,
                FreightChargeId = job.FreightCharge ?? string.Empty
            };
        }

        public async Task<List<ExpenseDetailDto>> GetExpenseDetailsAsync(decimal id, string? billNo = null)
        {
            GCTL.Data.Models.SepDocumentation? job = null;
            if (id > 0)
            {
                job = await _sepDocumentationRepository.All().AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Tc == id);
            }

            if (job == null && !string.IsNullOrWhiteSpace(billNo))
            {
                var bill = await _billEntryRepository.All().AsNoTracking()
                    .FirstOrDefaultAsync(b => b.BillNo == billNo);
                if (bill != null && !string.IsNullOrWhiteSpace(bill.JobNo))
                {
                    job = await _sepDocumentationRepository.All().AsNoTracking()
                        .FirstOrDefaultAsync(e => e.JobNo == bill.JobNo);
                }
            }

            if (job == null || string.IsNullOrEmpty(job.JobNo))
            {
                if (!string.IsNullOrWhiteSpace(billNo))
                {
                    var savedDetailsOnly = await _billDetailRepository.All().AsNoTracking()
                        .Where(d => d.BillNo == billNo)
                        .ToListAsync();

                    if (savedDetailsOnly.Any())
                    {
                        var expHeadDict = await _expHeadRepository.All().AsNoTracking()
                            .ToDictionaryAsync(h => h.ExpenseHeadId, h => h);
                        var stDict = await _stRepository.All().AsNoTracking()
                            .ToDictionaryAsync(s => s.ServiceTypeId, s => s.ServiceTypeName);

                        int seq = 1;
                        return savedDetailsOnly.Select(d =>
                        {
                            expHeadDict.TryGetValue(d.ExpenseHeadId ?? string.Empty, out var eh);
                            string stName = string.Empty;
                            if (eh != null && !string.IsNullOrEmpty(eh.ServiceTypeId))
                            {
                                stDict.TryGetValue(eh.ServiceTypeId, out stName);
                            }
                            return new ExpenseDetailDto
                            {
                                ExpenseHeadId = d.ExpenseHeadId,
                                SerialNo = eh?.SerialNo ?? (seq++).ToString(),
                                Head = d.BillHead ?? eh?.ExpenseHead ?? string.Empty,
                                IsRec = eh != null ? (eh.IsReceiptable == "Y" ? "Yes" : eh.IsReceiptable == "N" ? "No" : string.Empty) : string.Empty,
                                ServiceType = stName,
                                ConfirmAmt = d.ActualAmount ?? 0m,
                                BillAmt = d.ExpenseBillAmount ?? d.ActualAmount ?? 0m,
                                Include = true
                            };
                        }).ToList();
                    }
                }

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
                        select new
                        {
                            frServiceTypeId = fr.ServiceTypeId,
                            ehServiceTypeId = eh.ServiceTypeId,
                            frExpenseHeadId = fr.ExpenseHeadId,
                            ehExpenseHeadId = eh.ExpenseHeadId,
                            SerialNo = eh.SerialNo,
                            Head = eh.ExpenseHead,
                            IsReceiptable = eh.IsReceiptable,
                            ServiceType = st.ServiceTypeName,
                            ConfirmAmt = fr.ActualAmount,
                            BillAmt = fr.ActualAmount
                        };

            var rawOfrList = await query.ToListAsync();

            var ofrList = rawOfrList.Select(x => new
            {
                ServiceTypeId = x.frServiceTypeId ?? x.ehServiceTypeId ?? string.Empty,
                IsReceiptable = x.IsReceiptable ?? string.Empty,
                ExpenseHeadId = x.frExpenseHeadId ?? x.ehExpenseHeadId ?? string.Empty,
                SerialNo = x.SerialNo ?? string.Empty,
                Head = x.Head ?? string.Empty,
                IsRec = x.IsReceiptable == "Y" ? "Yes" : (x.IsReceiptable == "N" ? "No" : string.Empty),
                ServiceType = x.ServiceType ?? string.Empty,
                ConfirmAmt = x.ConfirmAmt,
                BillAmt = x.BillAmt
            }).ToList();

            if (!string.IsNullOrWhiteSpace(billNo))
            {
                var savedDetails = await _billDetailRepository.All().AsNoTracking()
                    .Where(d => d.BillNo == billNo)
                    .ToListAsync();

                var savedDict = savedDetails
                    .Where(d => !string.IsNullOrEmpty(d.ExpenseHeadId))
                    .GroupBy(d => d.ExpenseHeadId)
                    .ToDictionary(g => g.Key!, g => g.First());

                var handledHeadIds = new HashSet<string>();
                var result = new List<ExpenseDetailDto>();

                foreach (var item in ofrList)
                {
                    CfBillEntryDetail? savedItem = null;
                    bool isPresentInBill = !string.IsNullOrEmpty(item.ExpenseHeadId) && savedDict.TryGetValue(item.ExpenseHeadId, out savedItem);
                    if (isPresentInBill)
                    {
                        handledHeadIds.Add(item.ExpenseHeadId!);
                    }

                    result.Add(new ExpenseDetailDto
                    {
                        ExpenseHeadId = item.ExpenseHeadId,
                        SerialNo = item.SerialNo,
                        Head = item.Head,
                        IsRec = item.IsRec,
                        ServiceType = item.ServiceType,
                        ConfirmAmt = item.ConfirmAmt ?? 0.00m,
                        BillAmt = isPresentInBill && savedItem != null ? (savedItem.ExpenseBillAmount ?? savedItem.ActualAmount ?? item.BillAmt ?? 0m) : (item.ConfirmAmt ?? 0m),
                        Include = isPresentInBill
                    });
                }

                foreach (var saved in savedDetails)
                {
                    if (!string.IsNullOrEmpty(saved.ExpenseHeadId) && handledHeadIds.Contains(saved.ExpenseHeadId))
                        continue;

                    result.Add(new ExpenseDetailDto
                    {
                        ExpenseHeadId = saved.ExpenseHeadId,
                        SerialNo = string.Empty,
                        Head = saved.BillHead ?? string.Empty,
                        IsRec = string.Empty,
                        ServiceType = string.Empty,
                        ConfirmAmt = saved.ActualAmount ?? 0m,
                        BillAmt = saved.ExpenseBillAmount ?? saved.ActualAmount ?? 0m,
                        Include = true
                    });
                }

                return result
                    .OrderBy(x => x.ServiceType ?? string.Empty)
                    .ThenBy(x => x.IsRec == "Yes" ? 0 : (x.IsRec == "No" ? 1 : 2))
                    .ThenBy(x => int.TryParse(x.SerialNo, out int s) ? s : int.MaxValue)
                    .ThenBy(x => x.SerialNo ?? string.Empty)
                    .ToList();
            }

            return ofrList
                .OrderBy(x => x.ServiceType ?? string.Empty)
                .ThenBy(x => x.IsRec == "Yes" ? 0 : (x.IsRec == "No" ? 1 : 2))
                .ThenBy(x => int.TryParse(x.SerialNo, out int s) ? s : int.MaxValue)
                .ThenBy(x => x.SerialNo ?? string.Empty)
                .Select(x => new ExpenseDetailDto
                {
                    ExpenseHeadId = x.ExpenseHeadId,
                    SerialNo = x.SerialNo,
                    Head = x.Head,
                    IsRec = x.IsRec,
                    ServiceType = x.ServiceType,
                    ConfirmAmt = x.ConfirmAmt ?? 0.00m,
                    BillAmt = x.BillAmt,
                    Include = false
                })
                .ToList();
        }

        public async Task<List<ChargeTableItemDto>> GetChargeTableDataAsync(decimal id, string? billNo = null)
        {
            GCTL.Data.Models.SepDocumentation? job = null;
            if (id > 0)
            {
                job = await _sepDocumentationRepository.All().AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Tc == id);
            }

            CfBillEntry? bill = null;
            if (!string.IsNullOrWhiteSpace(billNo))
            {
                bill = await _billEntryRepository.All().AsNoTracking()
                    .FirstOrDefaultAsync(b => b.BillNo == billNo);
            }

            if (job == null && bill != null && !string.IsNullOrWhiteSpace(bill.JobNo))
            {
                job = await _sepDocumentationRepository.All().AsNoTracking()
                    .FirstOrDefaultAsync(e => e.JobNo == bill.JobNo);
            }

            string customerId = bill?.CustomerId ?? job?.CustomerId ?? string.Empty;
            string expenseTypeId = bill?.ExpenseTypeId ?? job?.ExpenseTypeId ?? string.Empty;

            var query = from cwde in _cwcdeRepository.All().AsNoTracking()
                        where cwde.CustomerId == customerId && cwde.ExpenseTypeId == expenseTypeId
                        join chc in _cRepository.All().AsNoTracking()
                            on cwde.Chcid equals chc.Chcid into gj
                        from subChc in gj.DefaultIfEmpty()
                        select new
                        {
                            CwdeChcid = cwde.Chcid,
                            ChcId = subChc.Chcid,
                            ExpenseTypeId = subChc.ExpenseTypeId,
                            CwdeExpenseTypeId = cwde.ExpenseTypeId,
                            IsReceiptable = subChc.IsReceiptable,
                            SerialNo = subChc.SerialNo,
                            Chc = subChc.Chchead,
                            Description = subChc.ChcheadDescription,
                            BillAmt = cwde.BillAmount,
                            Tc = cwde.Tc
                        };

            var rawList = await query.ToListAsync();

            var baseList = rawList.Select(x => new
            {
                Chcid = x.CwdeChcid ?? x.ChcId ?? string.Empty,
                ExpenseTypeId = x.ExpenseTypeId ?? x.CwdeExpenseTypeId ?? string.Empty,
                IsRec = x.IsReceiptable == "Y" ? "Yes" : (x.IsReceiptable == "N" ? "No" : string.Empty),
                SerialNo = x.SerialNo ?? string.Empty,
                Chc = x.Chc ?? string.Empty,
                Description = x.Description ?? string.Empty,
                BillAmt = x.BillAmt,
                Tc = x.Tc
            }).ToList();

            if (!string.IsNullOrWhiteSpace(billNo))
            {
                var savedChcs = await _billChcRepository.All().AsNoTracking()
                    .Where(c => c.BillNo == billNo)
                    .ToListAsync();

                var savedDict = savedChcs
                    .Where(c => !string.IsNullOrEmpty(c.Chcid))
                    .GroupBy(c => c.Chcid)
                    .ToDictionary(g => g.Key, g => g.First());

                var handledChcIds = new HashSet<string>();
                var result = new List<ChargeTableItemDto>();

                foreach (var item in baseList)
                {
                    ChargeTableItemDto? savedItemDto = null;
                    CfBillEntryDetailsChchead? savedItem = null;
                    bool isPresentInBill = !string.IsNullOrEmpty(item.Chcid) && savedDict.TryGetValue(item.Chcid, out savedItem);
                    if (isPresentInBill)
                    {
                        handledChcIds.Add(item.Chcid!);
                    }

                    result.Add(new ChargeTableItemDto
                    {
                        Chcid = item.Chcid,
                        SerialNo = item.SerialNo,
                        IsRec = item.IsRec,
                        Chc = item.Chc,
                        Description = isPresentInBill && savedItem != null && !string.IsNullOrEmpty(savedItem.ChcheadDescription) ? savedItem.ChcheadDescription : item.Description,
                        BillAmt = isPresentInBill && savedItem != null ? (savedItem.BillAmount ?? item.BillAmt) : item.BillAmt,
                        Tc = isPresentInBill && savedItem != null ? savedItem.Tc : item.Tc,
                        Include = isPresentInBill
                    });
                }

                int extraSeq = 1;
                foreach (var saved in savedChcs)
                {
                    if (!string.IsNullOrEmpty(saved.Chcid) && handledChcIds.Contains(saved.Chcid))
                        continue;

                    result.Add(new ChargeTableItemDto
                    {
                        Chcid = saved.Chcid,
                        SerialNo = (extraSeq++).ToString(),
                        IsRec = saved.IsReceiptable == "Y" ? "Yes" : (saved.IsReceiptable == "N" ? "No" : (saved.IsReceiptable ?? string.Empty)),
                        Chc = saved.BillHead ?? string.Empty,
                        Description = saved.ChcheadDescription ?? string.Empty,
                        BillAmt = saved.BillAmount,
                        Tc = saved.Tc,
                        Include = true
                    });
                }

                return result
                    .OrderBy(x => x.IsRec == "Yes" ? 0 : (x.IsRec == "No" ? 1 : 2))
                    .ThenBy(x => int.TryParse(x.SerialNo, out int s) ? s : int.MaxValue)
                    .ThenBy(x => x.SerialNo ?? string.Empty)
                    .ToList();
            }

            return baseList
                .OrderBy(x => x.IsRec == "Yes" ? 0 : (x.IsRec == "No" ? 1 : 2))
                .ThenBy(x => int.TryParse(x.SerialNo, out int s) ? s : int.MaxValue)
                .ThenBy(x => x.SerialNo ?? string.Empty)
                .Select(x => new ChargeTableItemDto
                {
                    Chcid = x.Chcid,
                    SerialNo = x.SerialNo,
                    IsRec = x.IsRec,
                    Chc = x.Chc,
                    Description = x.Description,
                    BillAmt = x.BillAmt,
                    Tc = x.Tc,
                    Include = false
                })
                .ToList();
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

        public async Task<PaginationResponse<BillHistoryDto>> GetBillHistoryPagedAsync(PaginationRequest request)
        {
            try
            {
                var query = from b in _billEntryRepository.All().AsNoTracking()
                            join s in _shipmentModeRepository.All().AsNoTracking()
                                on b.ExpenseTypeId equals s.ExpenseTypeId into modes
                            from s in modes.DefaultIfEmpty()
                            join c in _customerRepository.All().AsNoTracking()
                                on b.CustomerId equals c.CustomerId into customers
                            from c in customers.DefaultIfEmpty()
                            join d in _sepDocumentationRepository.All().AsNoTracking()
                                on b.JobNo equals d.JobNo into docs
                            from d in docs.DefaultIfEmpty()
                            select new BillHistoryDto
                            {
                                Tc = b.Tc,
                                BillNo = b.BillNo ?? string.Empty,
                                BillDate = b.BillDate,
                                JobNo = b.JobNo ?? string.Empty,
                                ShipmentMode = s != null ? s.ExpenseType : string.Empty,
                                Customer = c != null ? c.CustomerName : (b.CustomerId ?? string.Empty),
                                DeliveryDate = b.DeliveryDate.HasValue ? b.DeliveryDate : (d != null ? d.DateOfClearence : null),
                                Hawb = d != null ? (d.Hawb ?? string.Empty) : string.Empty,
                                BeNo = b.Beno ?? (d != null ? (d.Beno ?? string.Empty) : string.Empty),
                                InvoiceValue = b.InvoiceValue ?? (d != null ? d.InvoiceValue : null)
                            };

                if (string.IsNullOrEmpty(request.SortColumn))
                {
                    request.SortColumn = nameof(BillHistoryDto.Tc);
                    request.SortOrder = "desc";
                }

                return await PaginationService<BillHistoryDto>.GetPaginatedData(query, request);
            }
            catch (Exception)
            {
                return new PaginationResponse<BillHistoryDto>
                {
                    Data = new List<BillHistoryDto>(),
                    TotalCount = 0,
                    FilteredCount = 0
                };
            }
        }

        public async Task<BillEntryJobVM?> GetBillDetailAsync(string billNo)
        {
            if (string.IsNullOrWhiteSpace(billNo))
                return null;

            var bill = await _billEntryRepository.All().AsNoTracking()
                .FirstOrDefaultAsync(b => b.BillNo == billNo);

            if (bill == null)
                return null;

            GCTL.Data.Models.SepDocumentation? doc = null;
            if (!string.IsNullOrEmpty(bill.JobNo))
            {
                doc = await _sepDocumentationRepository.All().AsNoTracking()
                    .FirstOrDefaultAsync(d => d.JobNo == bill.JobNo);
            }

            return new BillEntryJobVM
            {
                Id = (int)(doc?.Tc ?? bill.Tc),
                BillNo = bill.BillNo ?? string.Empty,
                BillDate = bill.BillDate.HasValue ? bill.BillDate.Value.ToString("yyyy-MM-dd") : string.Empty,
                JobNo = bill.JobNo ?? (doc?.JobNo ?? string.Empty),
                JobDate = doc?.Date.HasValue == true ? doc.Date.Value.ToString("yyyy-MM-dd") : string.Empty,
                ShipmentMode = bill.ExpenseTypeId ?? (doc?.ExpenseTypeId ?? string.Empty),
                ShipmentModeId = bill.ExpenseTypeId ?? doc?.ExpenseTypeId,
                IsCustomJobNo = doc?.IsCustomJobNo ?? string.Empty,
                CustomerId = bill.CustomerId ?? doc?.CustomerId,
                CustomerName = bill.CustomerId ?? (doc?.CustomerId ?? string.Empty),
                CustomerAddress = bill.Address ?? (doc?.CustomerAddress ?? string.Empty),
                CustomerDeliveryAddress = doc?.DeliveryLocationCode ?? string.Empty,
                PortId = doc?.PortId,
                DocReceivedDate = bill.DocRecvDate.HasValue ? bill.DocRecvDate.Value.ToString("yyyy-MM-dd") : (doc?.DocReceivedDate.HasValue == true ? doc.DocReceivedDate.Value.ToString("yyyy-MM-dd") : string.Empty),
                DocsReceivedDate = bill.DocRecvDate.HasValue ? bill.DocRecvDate.Value.ToString("yyyy-MM-dd") : (doc?.DocReceivedDate.HasValue == true ? doc.DocReceivedDate.Value.ToString("yyyy-MM-dd") : string.Empty),
                ExpDate = doc?.ExpDate.HasValue == true ? doc.ExpDate.Value.ToString("yyyy-MM-dd") : string.Empty,
                LcExpNo = doc?.ExpNo ?? string.Empty,
                LcNo = doc?.Lcno ?? string.Empty,
                LcDate = doc?.Lcdate.HasValue == true ? doc.Lcdate.Value.ToString("yyyy-MM-dd") : string.Empty,
                LcValCurrencyId = bill.LcurrencyId ?? (doc?.CurrencyId ?? string.Empty),
                LcValue = bill.Lcvalue ?? doc?.Lcvalue,
                BdtRate = doc?.Bdtrate,
                BdtTotal = ((bill.Lcvalue ?? doc?.Lcvalue) ?? 0m) * (doc?.Bdtrate ?? 0m),
                IpEpNo = bill.Ipno ?? (doc?.Ipno ?? string.Empty),
                IpDate = bill.Ipdate.HasValue ? bill.Ipdate.Value.ToString("yyyy-MM-dd") : (doc?.Ipdate.HasValue == true ? doc.Ipdate.Value.ToString("yyyy-MM-dd") : string.Empty),
                ImporterId = doc?.ImporterId,
                InvoiceNo = bill.Invno ?? (doc?.InvoiceNo ?? string.Empty),
                InvoiceDate = bill.Invdate.HasValue ? bill.Invdate.Value.ToString("yyyy-MM-dd") : (doc?.InvoiceDate.HasValue == true ? doc.InvoiceDate.Value.ToString("yyyy-MM-dd") : string.Empty),
                InvoiceValue = bill.InvoiceValue ?? doc?.InvoiceValue,
                InvValCurrencyId = bill.InvoiceCurrencyId ?? (doc?.InvoiceCurrencyId ?? string.Empty),
                BlNo = bill.Blno ?? string.Empty,
                BlDate = bill.Bldate.HasValue ? bill.Bldate.Value.ToString("yyyy-MM-dd") : (doc?.Bldate.HasValue == true ? doc.Bldate.Value.ToString("yyyy-MM-dd") : string.Empty),
                BeNo = bill.Beno ?? (doc?.Beno ?? string.Empty),
                BeDate = bill.Bedate.HasValue ? bill.Bedate.Value.ToString("yyyy-MM-dd") : (doc?.Bedate.HasValue == true ? doc.Bedate.Value.ToString("yyyy-MM-dd") : string.Empty),
                ContainerNo = bill.ContainerNo ?? (doc?.ContainerNo ?? string.Empty),
                ContainerSize = doc?.ContainerSize ?? string.Empty,
                LcaNo = doc?.Lcano ?? string.Empty,
                DischargeDate = doc?.Dischargedate.HasValue == true ? doc.Dischargedate.Value.ToString("yyyy-MM-dd") : string.Empty,
                MaterialDescription = bill.MaterialDescription ?? (doc?.MaterialDescription ?? string.Empty),
                Quantity = bill.Quantity ?? doc?.Quntity1,
                QuantityUnitId = bill.QunitTypeId ?? doc?.Unit1,
                Weight = bill.DocWeight ?? doc?.Quntity2,
                WeightUnitId = bill.WunitTypeId ?? doc?.Unit2,
                ForwarderId = doc?.NameOfFreightForwarder,
                VesselRottNo = bill.Rotno ?? (doc?.VesselRottNo ?? string.Empty),
                Remarks = bill.Remarks ?? (doc?.Remarks ?? string.Empty),
                ConsoleNo = doc?.ConsoleNo ?? string.Empty,
                DestinationId = bill.Destination ?? string.Empty,
                AgComm = bill.MinimumAgencyCommission ?? doc?.MinimumAgencyCom,
                OnInvVal = bill.OnInvoiceValue ?? doc?.CommOnInvoiceValue,
                Amount = bill.BillAmount,
                SpecialDiscount = bill.SpecialDiscount,
                CreatedAt = bill.Ldate ?? doc?.Ldate,
                UpdatedAt = bill.ModifyDate ?? doc?.ModifyDate,

                InvBdtRate = bill.AssessableRate ?? doc?.AssessableRate,
                InvBdtTotal = bill.AssessableValue ?? doc?.AssessableValue ?? ((bill.InvoiceValue ?? doc?.InvoiceValue ?? 0m) * (bill.AssessableRate ?? doc?.AssessableRate ?? 0m)),
                NoTransport = bill.NumberOfTransport,
                DemarageDay = bill.NumberOfDemerageDay,
                UndertakingNo = bill.UndertakingNo ?? doc?.UndertakingNo,
                ArrivalDate = bill.ArrivalDate.HasValue ? bill.ArrivalDate.Value.ToString("yyyy-MM-dd") : (doc?.Ata.HasValue == true ? doc.Ata.Value.ToString("yyyy-MM-dd") : (doc?.Shiparriveddate.HasValue == true ? doc.Shiparriveddate.Value.ToString("yyyy-MM-dd") : string.Empty)),
                EfrNo = bill.Efrno ?? string.Empty,
                EfrDate = bill.Efrdate.HasValue ? bill.Efrdate.Value.ToString("yyyy-MM-dd") : string.Empty,
                RotNo = bill.Rotno ?? doc?.VesselRottNo ?? string.Empty,
                Hawb = doc?.Hawb ?? string.Empty,
                PackQty = doc?.PackingQuantity,
                PackQtyUnitId = doc?.PackingUnitType ?? string.Empty,
                EtaDeliveryDate = doc?.Eta.HasValue == true ? doc.Eta.Value.ToString("yyyy-MM-dd") : string.Empty,
                ActualDeliveryDate = bill.DeliveryDate.HasValue ? bill.DeliveryDate.Value.ToString("yyyy-MM-dd") : (doc?.DateOfClearence.HasValue == true ? doc.DateOfClearence.Value.ToString("yyyy-MM-dd") : (doc?.Ata.HasValue == true ? doc.Ata.Value.ToString("yyyy-MM-dd") : string.Empty)),
                UnstuffingDate = doc?.UnStuffingLocationDate.HasValue == true ? doc.UnStuffingLocationDate.Value.ToString("yyyy-MM-dd") : string.Empty,
                PlaceOfLoadingId = doc?.PlaceOfLoadingId ?? string.Empty,
                EtdDate = doc?.Etddate.HasValue == true ? doc.Etddate.Value.ToString("yyyy-MM-dd") : string.Empty,
                ShedYardId = doc?.ShedYardId ?? string.Empty,
                FreightChargeId = doc?.FreightCharge ?? string.Empty
            };
        }

        public async Task<CommonReturnViewModel> SaveBillAsync(SaveBillRequestDto request, UserInfoViewModel? userInfo)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.JobNo))
            {
                return new CommonReturnViewModel
                {
                    Success = false,
                    Message = "Job No is required."
                };
            }

            await _billEntryRepository.BeginTransactionAsync();

            try
            {
                var doc = await _sepDocumentationRepository.All()
                    .FirstOrDefaultAsync(d => d.JobNo == request.JobNo);

                if (doc == null)
                {
                    await _billEntryRepository.RollbackTransactionAsync();
                    return new CommonReturnViewModel
                    {
                        Success = false,
                        Message = $"Job '{request.JobNo}' not found."
                    };
                }

                var now = DateTime.Now;
                string username = userInfo?.Username ?? "System";
                string empId = !string.IsNullOrEmpty(userInfo?.EmployeeId) ? userInfo.EmployeeId : username;
                string companyCode = !string.IsNullOrEmpty(userInfo?.CompanyCode) ? userInfo.CompanyCode : "GCTL";
                string ip = userInfo?.IPAddress ?? string.Empty;
                string mac = userInfo?.MacAddress ?? string.Empty;

                DateTime? billDate = null;
                if (!string.IsNullOrWhiteSpace(request.BillDate))
                {
                    string[] formats = { "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy" };
                    if (DateTime.TryParseExact(request.BillDate.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                    {
                        billDate = parsedDate;
                    }
                    else if (DateTime.TryParse(request.BillDate, out var fallbackDate))
                    {
                        billDate = fallbackDate;
                    }
                }
                billDate ??= now;

                string billNo = request.BillNo?.Trim() ?? string.Empty;

                CfBillEntry? bill = null;
                if (!string.IsNullOrWhiteSpace(billNo))
                {
                    bill = await _billEntryRepository.All()
                        .FirstOrDefaultAsync(b => b.BillNo == billNo);
                }

                if (bill == null)
                {
                    // Generate new BillNo if not provided or doesn't exist
                    if (string.IsNullOrWhiteSpace(billNo))
                    {
                        var maxBillNo = await _billEntryRepository.All()
                            .Where(b => b.BillNo != null)
                            .Select(b => b.BillNo)
                            .OrderByDescending(b => b)
                            .FirstOrDefaultAsync();

                        long nextSeq = 1;
                        if (!string.IsNullOrEmpty(maxBillNo))
                        {
                            var digits = new string(maxBillNo.Where(char.IsDigit).ToArray());
                            if (long.TryParse(digits, out long parsedNum))
                            {
                                nextSeq = parsedNum + 1;
                            }
                        }
                        billNo = $"BILL-{now:yyyyMMdd}-{nextSeq:D4}";
                    }

                    bill = new CfBillEntry
                    {
                        BillNo = billNo,
                        BillDate = billDate,
                        JobNo = doc.JobNo,
                        ExpenseTypeId = doc.ExpenseTypeId,
                        CustomerId = !string.IsNullOrWhiteSpace(request.CustomerId) ? request.CustomerId : doc.CustomerId,
                        Address = doc.CustomerAddress,
                        MaterialDescription = doc.MaterialDescription,
                        Blno = doc.BillNo ?? string.Empty,
                        Bldate = doc.Bldate,
                        Beno = doc.Beno,
                        Bedate = doc.Bedate,
                        Invno = doc.InvoiceNo,
                        Invdate = doc.InvoiceDate,
                        Ipno = doc.Ipno,
                        Ipdate = doc.Ipdate,
                        DocRecvDate = doc.DocReceivedDate,
                        CompanyCode = companyCode,
                        EmployeeId = empId,
                        MinimumAgencyCommission = request.AgComm,
                        OnInvoiceValue = request.OnInvVal,
                        Remarks = request.Remarks,
                        ContainerNo = doc.ContainerNo,
                        Quantity = doc.Quntity1,
                        QunitTypeId = doc.Unit1,
                        DocWeight = doc.Quntity2,
                        WunitTypeId = doc.Unit2,
                        Lcvalue = doc.Lcvalue,
                        LcurrencyId = doc.CurrencyId,
                        Destination = request.DestinationId,
                        BillAmount = request.Amount,
                        SpecialDiscount = request.SpecialDiscount,
                        InvoiceCurrencyId = doc.InvoiceCurrencyId,
                        InvoiceValue = doc.InvoiceValue,
                        Rotno = doc.VesselRottNo,
                        Luser = username,
                        Ldate = now,
                        Lip = ip,
                        Lmac = mac
                    };

                    await _billEntryRepository.AddAsync(bill);
                }
                else
                {
                    // Update existing
                    bill.BillDate = billDate;
                    bill.Remarks = request.Remarks;
                    bill.Destination = request.DestinationId;
                    bill.MinimumAgencyCommission = request.AgComm;
                    bill.OnInvoiceValue = request.OnInvVal;
                    bill.BillAmount = request.Amount;
                    bill.SpecialDiscount = request.SpecialDiscount;
                    bill.ModifyDate = now;
                    bill.Luser = username;
                    bill.Lip = ip;
                    bill.Lmac = mac;

                    await _billEntryRepository.UpdateAsync(bill);
                }

                // NOTE: SepDocumentation.BillNo (BL No) update commented out pending supervisor discussion
                // doc.BillNo = billNo;
                // await _sepDocumentationRepository.UpdateAsync(doc);

                // Clear existing child records for this BillNo
                var existingChcs = await _billChcRepository.All()
                    .Where(c => c.BillNo == billNo)
                    .ToListAsync();
                if (existingChcs.Any())
                {
                    await _billChcRepository.DeleteRangeAsync(existingChcs);
                }

                var existingDetails = await _billDetailRepository.All()
                    .Where(d => d.BillNo == billNo)
                    .ToListAsync();
                if (existingDetails.Any())
                {
                    await _billDetailRepository.DeleteRangeAsync(existingDetails);
                }

                // Insert Custom Handling Charges
                if (request.ChcItems != null && request.ChcItems.Any())
                {
                    var chcEntities = request.ChcItems.Select(item => new CfBillEntryDetailsChchead
                    {
                        BillNo = billNo,
                        JobNo = doc.JobNo,
                        Chcid = item.Chcid ?? string.Empty,
                        BillHead = item.BillHead,
                        ChcheadDescription = item.Description,
                        BillAmount = item.BillAmount,
                        IsReceiptable = item.IsReceiptable,
                        CompanyCode = companyCode,
                        EmployeeId = empId,
                        Luser = username,
                        Ldate = now,
                        Lip = ip,
                        Lmac = mac
                    }).ToList();

                    await _billChcRepository.AddRangeAsync(chcEntities);
                }

                // Insert Expense Details
                if (request.ExpenseItems != null && request.ExpenseItems.Any())
                {
                    int detailSeq = 1;
                    var detailEntities = request.ExpenseItems.Select(item => new CfBillEntryDetail
                    {
                        BillDetailsId = (detailSeq++).ToString("D4"),
                        BillNo = billNo,
                        ExpenseHeadId = item.ExpenseHeadId ?? string.Empty,
                        BillHead = item.BillHead,
                        ActualAmount = item.ConfirmAmt,
                        ExpenseBillAmount = item.BillAmt,
                        CompanyCode = companyCode,
                        EmployeeId = empId,
                        Luser = username,
                        Ldate = now,
                        Lip = ip,
                        Lmac = mac
                    }).ToList();

                    await _billDetailRepository.AddRangeAsync(detailEntities);
                }

                await _billEntryRepository.CommitTransactionAsync();

                return new CommonReturnViewModel
                {
                    Success = true,
                    Message = "Bill saved successfully.",
                    Data = billNo
                };
            }
            catch (Exception ex)
            {
                await _billEntryRepository.RollbackTransactionAsync();
                return new CommonReturnViewModel
                {
                    Success = false,
                    Message = $"Error saving bill: {ex.Message}"
                };
            }
        }

        public async Task<CommonReturnViewModel> DeleteBillsAsync(List<string> billNos, UserInfoViewModel? userInfo)
        {
            if (billNos == null || !billNos.Any())
            {
                return new CommonReturnViewModel
                {
                    Success = false,
                    Message = "No bills selected for deletion."
                };
            }

            await _billEntryRepository.BeginTransactionAsync();

            try
            {
                var billsToDelete = await _billEntryRepository.All()
                    .Where(b => billNos.Contains(b.BillNo))
                    .ToListAsync();

                if (!billsToDelete.Any())
                {
                    await _billEntryRepository.RollbackTransactionAsync();
                    return new CommonReturnViewModel
                    {
                        Success = false,
                        Message = "No matching bills found to delete."
                    };
                }

                // NOTE: SepDocumentation BL No update commented out pending supervisor discussion
                /*
                var jobNos = billsToDelete
                    .Where(b => !string.IsNullOrEmpty(b.JobNo))
                    .Select(b => b.JobNo)
                    .Distinct()
                    .ToList();

                if (jobNos.Any())
                {
                    var docsToReset = await _sepDocumentationRepository.All()
                        .Where(d => jobNos.Contains(d.JobNo))
                        .ToListAsync();

                    foreach (var doc in docsToReset)
                    {
                        if (billNos.Contains(doc.BillNo))
                        {
                            var matchedBill = billsToDelete.FirstOrDefault(b => b.JobNo == doc.JobNo && !string.IsNullOrEmpty(b.Blno));
                            doc.BillNo = matchedBill?.Blno ?? string.Empty;
                            await _sepDocumentationRepository.UpdateAsync(doc);
                        }
                    }
                }
                */

                // Delete child records for all deleted bills
                var childChcs = await _billChcRepository.All()
                    .Where(c => billNos.Contains(c.BillNo))
                    .ToListAsync();
                if (childChcs.Any())
                {
                    await _billChcRepository.DeleteRangeAsync(childChcs);
                }

                var childDetails = await _billDetailRepository.All()
                    .Where(d => billNos.Contains(d.BillNo))
                    .ToListAsync();
                if (childDetails.Any())
                {
                    await _billDetailRepository.DeleteRangeAsync(childDetails);
                }

                foreach (var bill in billsToDelete)
                {
                    await _billEntryRepository.DeleteAsync(bill);
                }

                await _billEntryRepository.CommitTransactionAsync();

                return new CommonReturnViewModel
                {
                    Success = true,
                    Message = $"{billsToDelete.Count} bill(s) deleted successfully."
                };
            }
            catch (Exception ex)
            {
                await _billEntryRepository.RollbackTransactionAsync();
                return new CommonReturnViewModel
                {
                    Success = false,
                    Message = $"Error deleting bills: {ex.Message}"
                };
            }
        }
    }
}
