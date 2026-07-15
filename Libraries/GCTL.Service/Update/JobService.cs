using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Spreadsheet;
using GCTL.Core.Data;
using GCTL.Core.Helpers;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.SepDocumentation;
using GCTL.Core.ViewModels.Update;
using GCTL.Data.Models;
using GCTL.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static GCTL.Service.Update.JobService;


namespace GCTL.Service.Update
{
    public class JobService : IJobService
    {
        private readonly IGenericRepository<GCTL.Data.Models.SepDocumentation> _sepDocumentionRepository;
        private readonly IGenericRepository<GCTL.Data.Models.CfDailyJobUpdateEntry> _jobUpdateRepository;
        private readonly IGenericRepository<GCTL.Data.Models.CfDefExpenseType> _shipmentModeRepository;
        private readonly IGenericRepository<GCTL.Data.Models.SalesCustomer> _customerRepository;
        private readonly IGenericRepository<GCTL.Data.Models.SalesDefShipmentStatusInfo> _shipmentStatusInfoRepository;

        public JobService(
            IGenericRepository<GCTL.Data.Models.SepDocumentation> sepDocumentionRepository,
            IGenericRepository<GCTL.Data.Models.CfDefExpenseType> shipmentModeRepository,
            IGenericRepository<GCTL.Data.Models.SalesCustomer> customerRepository,
            IGenericRepository<GCTL.Data.Models.CfDailyJobUpdateEntry> jobUpdateRepository,
            IGenericRepository<GCTL.Data.Models.SalesDefShipmentStatusInfo> shipmentStatusInfoRepository)
        {
            _sepDocumentionRepository = sepDocumentionRepository;
            _shipmentModeRepository = shipmentModeRepository;
            _customerRepository = customerRepository;
            _jobUpdateRepository = jobUpdateRepository;
            _shipmentStatusInfoRepository = shipmentStatusInfoRepository;
        }

        public async Task<SepPagedResult<JobDto>> GetJobsPagedAsync(
     int page,
     int pageSize,
     string? customerId,
     string? shipmentMode,
     DateTime? dateFrom,
     DateTime? dateTo,
     string? search)
        {
            try
            {
                // সর্বোচ্চ স্ট্যাটাস ID বের করুন (07 = Delivered)
                var allStatusIds = await _shipmentStatusInfoRepository.All()
                    .Select(s => s.ShipmentStatusId)
                    .ToListAsync();

                var maxStatusId = allStatusIds
                    .Where(s => int.TryParse(s, out _))
                    .Select(int.Parse)
                    .DefaultIfEmpty(0)
                    .Max()
                    .ToString("D2");

                // যেসব জব সর্বোচ্চ স্ট্যাটাসে (07 = Delivered) পৌঁছে গেছে, তাদের JobNo বের করুন
                var completedJobNos = await _jobUpdateRepository.All()
                    .Where(e => e.UpdateStatus == maxStatusId)
                    .Select(e => e.JobNo)
                    .Distinct()
                    .ToListAsync();

                IQueryable<JobDto> query =
                    from d in _sepDocumentionRepository.All().AsNoTracking()
                    where !completedJobNos.Contains(d.JobNo)   // completed জব বাদ

                    join c in _customerRepository.All().AsNoTracking()
                        on d.CustomerId equals c.CustomerId into customers
                    from c in customers.DefaultIfEmpty()

                    join s in _shipmentModeRepository.All().AsNoTracking()
                        on d.ExpenseTypeId equals s.ExpenseTypeId into shipmentModes
                    from s in shipmentModes.DefaultIfEmpty()

                    select new JobDto
                    {
                        Id = (short)d.Tc,
                        JobNo = d.JobNo,
                        CustomerId = d.CustomerId,
                        CustomerName = c != null ? c.CustomerName : "",
                        ShipmentModeId = s != null ? s.ExpenseTypeId.ToString() : "",
                        ShipmentMode = s != null ? s.ExpenseType : "",
                        JobDate = d.Date,
                        DocReceivedDate = d.DocReceivedDate,
                        JobDateFormatted = d.Date.HasValue ?
                            (d.Date.Value.Day < 10 ? "0" + d.Date.Value.Day.ToString() : d.Date.Value.Day.ToString()) + "/" +
                            (d.Date.Value.Month < 10 ? "0" + d.Date.Value.Month.ToString() : d.Date.Value.Month.ToString()) + "/" +
                            d.Date.Value.Year.ToString()
                            : "",
                        DocReceivedDateFormatted = d.DocReceivedDate.HasValue ?
                            (d.DocReceivedDate.Value.Day < 10 ? "0" + d.DocReceivedDate.Value.Day.ToString() : d.DocReceivedDate.Value.Day.ToString()) + "/" +
                            (d.DocReceivedDate.Value.Month < 10 ? "0" + d.DocReceivedDate.Value.Month.ToString() : d.DocReceivedDate.Value.Month.ToString()) + "/" +
                            d.DocReceivedDate.Value.Year.ToString()
                            : "",
                        JobDateText = d.Date.HasValue ? d.Date.Value.ToString("dd/MM/yyyy") : "",
                        DocReceivedDateText = d.DocReceivedDate.HasValue ? d.DocReceivedDate.Value.ToString("dd/MM/yyyy") : ""
                    };

                var totalCount = await query.CountAsync();

                if (!string.IsNullOrWhiteSpace(customerId))
                    query = query.Where(x => x.CustomerId == customerId);

                if (!string.IsNullOrWhiteSpace(shipmentMode))
                    query = query.Where(x => x.ShipmentModeId == shipmentMode);

                if (dateFrom.HasValue)
                    query = query.Where(x => x.JobDate >= dateFrom.Value);

                if (dateTo.HasValue)
                {
                    var endDate = dateTo.Value.Date.AddDays(1);
                    query = query.Where(x => x.JobDate < endDate);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.Trim();
                    query = query.Where(x =>
                        x.JobNo.Contains(search) ||
                        x.CustomerId.Contains(search) ||
                        x.CustomerName.Contains(search) ||
                        x.ShipmentMode.Contains(search) ||
                        x.ShipmentModeId.Contains(search) ||
                        x.JobDateFormatted.Contains(search) ||
                        x.DocReceivedDateFormatted.Contains(search));
                }

                var filteredCount = await query.CountAsync();

                var data = await query
                    .OrderBy(x => x.JobNo)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return new SepPagedResult<JobDto>
                {
                    Data = data,
                    TotalRecords = totalCount,
                    FilteredRecords = filteredCount
                };
            }
            catch
            {
                return new SepPagedResult<JobDto>
                {
                    Data = new List<JobDto>(),
                    TotalRecords = 0,
                    FilteredRecords = 0
                };
            }
        }
        public async Task<JobDetailDto> GetJobDetailAsync(int id)
        {

           

            var query1 = await _sepDocumentionRepository.All()
             .Select(h => new JobDetailDto
             {
                 Id = Convert.ToInt16(h.Tc),
                 JobNo = h.JobNo,
                 ShipmentMode = h.ExpenseTypeId,          // h থেকে bind করুন
                 ShipmentModeId = h.ExpenseTypeId,
                 IsCustomJobNo = h.IsCustomJobNo,
                 JobDate = h.Date.HasValue ? h.Date.Value.ToDateFormat() : "",
                 CustomerId = h.CustomerId,
                 CustomerName = h.CustomerId,
                 CustomerAddress = h.CustomerAddress,
                 CustomerDeliveryAddress = h.DeliveryLocationCode, //TODO: DElivery add
                 PortId = h.PortId,
                 DocsReceivedDate = h.DocReceivedDate.HasValue? h.DocReceivedDate.Value.ToDateFormat():"",
                 LcExpNo = h.ExpNo,
                 LcValue = h.Lcvalue,
                 LcDate = h.Lcdate.HasValue ? h.Lcdate.Value.ToDateFormat() : "",
                 //LcUnitId= h.lc
                 IpEpNo = h.Ipno,
                 IpDate = h.Ipdate.HasValue ? h.Ipdate.Value.ToDateFormat() : "",
                 ImporterId = h.ImporterId,
                 InvoiceNo = h.InvoiceNo,
                 InvoiceDate = h.InvoiceDate.HasValue ? h.InvoiceDate.Value.ToDateFormat() : "",
                 BlNo = h.Beno,
                 BeNo = h.Beno,
                 BeDate = h.Bedate.HasValue ? h.Bedate.Value.ToDateFormat() : "",
                 BlDate = h.Bldate.HasValue ? h.Bldate.Value.ToDateFormat() : "",
                 ContainerNo = h.ContainerNo,
                 ContainerSize = h.ContainerSize,
                 LcaNo = h.Lcano,
                 DischargeDate = h.Dischargedate.HasValue ? h.Dischargedate.Value.ToDateFormat() : "",
                 MaterialDescription = h.MaterialDescription,
                 Quantity = h.Quntity1,
                 QuantityUnitId = h.Unit1,
                 Weight = h.Quntity2,
                 WeightUnitId = h.Unit2,
                 ForwarderId = h.NameOfFreightForwarder,
                 VesselRottNo = h.VesselRottNo,
                 Remarks = h.Remarks,
                 DocReceivedDate = h.DocReceivedDate.HasValue ? h.DocReceivedDate.Value.ToDateFormat() : "",
                 CreatedAt = h.Ldate,
                 UpdatedAt = h.Ldate,
             })
             .FirstOrDefaultAsync(j => j.Id == id);

           
            if (query1 == null)
                throw new Exception("Job not found");

            return query1;
        }
        public async Task<SepPagedResult<JobStatusDto>> GetJobStatusesPagedAsync(
     int page,
     int pageSize,
     string? search,
     string? customerCode,
     DateTime? fromDate,
     DateTime? toDate)
        {
            try
            {
                var query =
                    from c in _jobUpdateRepository.All().AsNoTracking()
                    join s in _sepDocumentionRepository.All().AsNoTracking()
                        on c.JobNo equals s.JobNo into sj
                    from s in sj.DefaultIfEmpty()
                    join sm in _shipmentStatusInfoRepository.All().AsNoTracking()
                        on c.UpdateStatus equals sm.ShipmentStatusId into shipmentStatuses
                    from sm in shipmentStatuses.DefaultIfEmpty()
                    select new
                    {
                        JobUpdate = c,
                        Document = s,
                        StatusInfo = sm
                    };

                var totalCount = await query.CountAsync();

                if (!string.IsNullOrWhiteSpace(customerCode))
                {
                    query = query.Where(x => x.Document.CustomerId == customerCode);
                }

                if (fromDate.HasValue)
                {
                    query = query.Where(x => x.JobUpdate.UpdateDateTime >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    var endDate = toDate.Value.Date.AddDays(1);
                    query = query.Where(x => x.JobUpdate.UpdateDateTime < endDate);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.Trim().ToLower();

                    query = query.Where(x =>
                        x.JobUpdate.JobNo.ToLower().Contains(search) ||
                        x.JobUpdate.UpdateStatus.ToLower().Contains(search) ||
                        (x.JobUpdate.UpdateDateTime != null &&
                            (
                                (x.JobUpdate.UpdateDateTime.Value.Day < 10 ? "0" + x.JobUpdate.UpdateDateTime.Value.Day.ToString() : x.JobUpdate.UpdateDateTime.Value.Day.ToString()) + "/" +
                                (x.JobUpdate.UpdateDateTime.Value.Month < 10 ? "0" + x.JobUpdate.UpdateDateTime.Value.Month.ToString() : x.JobUpdate.UpdateDateTime.Value.Month.ToString()) + "/" +
                                x.JobUpdate.UpdateDateTime.Value.Year.ToString()
                            ).Contains(search)));
                }

                var filteredCount = await query.CountAsync();

                var rawData = await query
                    .OrderByDescending(x => x.JobUpdate.Tc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new
                    {
                        x.JobUpdate.Tc,
                        x.JobUpdate.DailyJobUpdateEntryId,
                        x.JobUpdate.JobNo,
                        x.JobUpdate.UpdateStatus,
                        x.JobUpdate.UpdateDateTime,
                        StatusName = x.StatusInfo != null ? x.StatusInfo.ShipmentStatus : x.JobUpdate.UpdateStatus
                    })
                    .ToListAsync();

                var data = rawData.Select(x => new JobStatusDto
                {
                    Id = x.Tc,
                    JobUpdateCode = x.DailyJobUpdateEntryId,
                    Status = "Success",
                    JobNo = x.JobNo,
                    ShipmentStatus = x.StatusName,
                    DateTime = x.UpdateDateTime,
                    DateTimeText = x.UpdateDateTime.HasValue ? x.UpdateDateTime.Value.ToDateTimeFormat() : ""
                }).ToList();

                return new SepPagedResult<JobStatusDto>
                {
                    Data = data,
                    TotalRecords = totalCount,
                    FilteredRecords = filteredCount
                };
            }
            catch
            {
                return new SepPagedResult<JobStatusDto>
                {
                    Data = new List<JobStatusDto>(),
                    TotalRecords = 0,
                    FilteredRecords = 0
                };
            }
        }
        public async Task<CommonReturnViewModel> UpdateJobs(ShipmentUpdateViewModel model, string? company)
        {
            await _sepDocumentionRepository.BeginTransactionAsync();

            try
            {
                var doucument = await _sepDocumentionRepository.All().FirstOrDefaultAsync(e => e.JobNo == model.JobNo);

                if (doucument == null)
                {
                    await _sepDocumentionRepository.RollbackTransactionAsync();
                    return new CommonReturnViewModel()
                    {
                        Success = false,
                        Message = "job Not found"
                    };
                }

                var allStatuses = await _shipmentStatusInfoRepository.All()
                    .Select(s => new { s.ShipmentStatusId, s.ShipmentStatus })
                    .ToListAsync();

                var orderedStatuses = allStatuses
                    .Where(s => int.TryParse(s.ShipmentStatusId, out _))
                    .OrderBy(s => int.Parse(s.ShipmentStatusId))
                    .ToList();

                if (!orderedStatuses.Any())
                {
                    await _sepDocumentionRepository.RollbackTransactionAsync();
                    return new CommonReturnViewModel()
                    {
                        Success = false,
                        Message = "No shipment statuses configured"
                    };
                }

                if (!orderedStatuses.Any(s => s.ShipmentStatusId == model.ShipmentStatus))
                {
                    await _sepDocumentionRepository.RollbackTransactionAsync();
                    return new CommonReturnViewModel()
                    {
                        Success = false,
                        Message = "Invalid shipment status"
                    };
                }

                var existingStatusIds = await _jobUpdateRepository.All()
                    .Where(e => e.JobNo == model.JobNo && e.Tc != model.AutoId)
                    .Select(e => e.UpdateStatus)
                    .ToListAsync();

                bool hasExistingRecord = existingStatusIds.Any();

                int maxExisting = existingStatusIds
                    .Where(x => !string.IsNullOrEmpty(x) && int.TryParse(x, out _))
                    .Select(int.Parse)
                    .DefaultIfEmpty(0)
                    .Max();

                var firstStatus = orderedStatuses.First();

                if (!hasExistingRecord)
                {
                    // প্রথমবার সেভ - অবশ্যই প্রথম স্ট্যাটাস (01) হতে হবে, ডুপ্লিকেট চেকের দরকার নেই
                    if (model.ShipmentStatus != firstStatus.ShipmentStatusId)
                    {
                        await _sepDocumentionRepository.RollbackTransactionAsync();
                        return new CommonReturnViewModel()
                        {
                            Success = false,
                            Message = $"First status must be '{firstStatus.ShipmentStatusId} - {firstStatus.ShipmentStatus}'. You cannot skip the initial step."
                        };
                    }
                }
                else
                {
                    // ডুপ্লিকেট চেক - এই স্ট্যাটাস আগে থেকেই সেভ করা থাকলে আটকান
                    if (existingStatusIds.Contains(model.ShipmentStatus))
                    {
                        await _sepDocumentionRepository.RollbackTransactionAsync();
                        return new CommonReturnViewModel()
                        {
                            Success = false,
                            Message = "This status has already been saved for this job."
                        };
                    }

                    var expectedNext = orderedStatuses
                        .FirstOrDefault(s => int.Parse(s.ShipmentStatusId) > maxExisting);

                    if (expectedNext == null || model.ShipmentStatus != expectedNext.ShipmentStatusId)
                    {
                        await _sepDocumentionRepository.RollbackTransactionAsync();

                        string expectedName = expectedNext != null
                            ? $"{expectedNext.ShipmentStatusId} - {expectedNext.ShipmentStatus}"
                            : "N/A (all steps completed)";

                        return new CommonReturnViewModel()
                        {
                            Success = false,
                            Message = $"Step skipped. Next expected status is '{expectedName}'. You cannot skip steps."
                        };
                    }
                }

                doucument.Eta = model.ETADeliveryDate;
                doucument.Ata = model.ActualDeliveryDate;
                doucument.UnStuffingLocationDate = model.UnstuffingDate;
                doucument.Etddate = model.ETDDate;
                doucument.PlaceOfLoadingId = model.PlaceOfLoadingID;
                doucument.ShedYardId = model.ShedYardID;
                doucument.FreightCharge = model.FreightChargeID;
                doucument.ModifyDate = DateTime.Now;
                await _sepDocumentionRepository.UpdateAsync(doucument);

                DateTime updateDateTime = CombineDateAndTime(model.Date, model.Time);

                if (model.AutoId > 0)
                {
                    var prevData = await _jobUpdateRepository.All().FirstOrDefaultAsync(e => e.Tc == model.AutoId);

                    if (prevData != null)
                    {
                        prevData.UpdateStatus = model.ShipmentStatus;
                        prevData.UpdateDateTime = updateDateTime;
                        prevData.ModifyDate = DateTime.Now;
                        prevData.Lip = model.Lip;
                        prevData.Lmac = model.Lmac;
                        prevData.EmployeeId = model.Luser ?? "";
                        prevData.Luser = model.Luser;

                        await _jobUpdateRepository.UpdateAsync(prevData);
                    }
                }
                else
                {
                    var updateDoc = new CfDailyJobUpdateEntry()
                    {
                        JobNo = model.JobNo,
                        DailyJobUpdateEntryId = await GetLastJobEntryCode(),
                        UpdateStatus = model.ShipmentStatus,
                        UpdateDateTime = updateDateTime,
                        Ldate = DateTime.Now,
                        Lip = model.Lip,
                        Lmac = model.Lmac,
                        EmployeeId = model.Luser ?? "",
                        Luser = model.Luser,
                        CompanyCode = company.ToString(),
                        UpdateRemarks = model.UpdateRemarks
                    };

                    await _jobUpdateRepository.AddAsync(updateDoc);
                }

                await _sepDocumentionRepository.CommitTransactionAsync();

                return new CommonReturnViewModel()
                {
                    Success = true,
                    Message = "Update Success",
                    Data = doucument.JobNo
                };
            }
            catch (Exception ex)
            {
                await _sepDocumentionRepository.RollbackTransactionAsync();
                return new CommonReturnViewModel()
                {
                    Success = false,
                    Message = "Something went wrong: " + ex.Message
                };
            }
        }

        // Helper method to combine Date and Time
        private DateTime CombineDateAndTime(DateTime? date, TimeOnly? time)
        {
            if (date == null)
                date = DateTime.Now.Date;

            if (time == null)
                return date.Value.Date;

            return date.Value.Date.Add(time.Value.ToTimeSpan());
        }

        private async Task<string> GetLastJobEntryCode()
        {
            var lastCode = await _jobUpdateRepository.All()
                .Select(e => new { e.Tc, e.DailyJobUpdateEntryId })
                .OrderByDescending(e => e.Tc)
                .FirstOrDefaultAsync();

            if (lastCode == null)
            {
                // প্রথম এন্ট্রি হলে শুরু হবে 00000001 থেকে
                return "00000001";
            }

            // ধরে নিচ্ছি DailyJobUpdateEntryID হচ্ছে string ফরম্যাটে কোড (যেমন "00000001")
            int numericCode = int.Parse(lastCode.DailyJobUpdateEntryId);
            numericCode++;

            // 8 digit format এ আবার রিটার্ন করা হবে
            return numericCode.ToString("D8");
        }

        public async Task<JobDetailDto> GetJobTopDetailsAsync(string jobNo)
        {
            var query1 = await _sepDocumentionRepository.All()
            .Select(h => new JobDetailDto
            {
                Id = Convert.ToInt16(h.Tc),
                JobNo = h.JobNo,
                ShipmentMode = h.ExpenseTypeId,
                ShipmentModeId = h.ExpenseTypeId,
                IsCustomJobNo = h.IsCustomJobNo,
                JobDate = h.Date.HasValue ? h.Date.Value.ToDateFormat() : "",
                CustomerId = h.CustomerId,
                LcDate = h.Lcdate.HasValue ? h.Lcdate.Value.ToDateFormat() : "",
                CustomerDeliveryAddress = h.DeliveryLocationCode,
                PortId = h.PortId,
                DocsReceivedDate = h.DocReceivedDate.HasValue ? h.DocReceivedDate.Value.ToDateFormat() : "",
                LcExpNo = h.ExpNo,
                LcValue = h.Lcvalue,
                IpEpNo = h.Ipno,
                IpDate = h.Ipdate.HasValue ? h.Ipdate.Value.ToDateFormat() : "",
                ImporterId = h.ImporterId,
                InvoiceNo = h.InvoiceNo,
                InvoiceDate = h.InvoiceDate.HasValue ? h.InvoiceDate.Value.ToDateFormat() : "",
                BlNo = h.Beno,
                BeNo = h.Beno,
                BeDate = h.Bedate.HasValue ? h.Bedate.Value.ToDateFormat() : "",
                BlDate = h.Bldate.HasValue ? h.Bldate.Value.ToDateFormat() : "",
                ContainerNo = h.ContainerNo,
                ContainerSize = h.ContainerSize,
                LcaNo = h.Lcano,
                DischargeDate = h.Dischargedate.HasValue ? h.Dischargedate.Value.ToDateFormat() : "",
                MaterialDescription = h.MaterialDescription,
                Quantity = h.Quntity1,
                QuantityUnitId = h.Unit1,
                Weight = h.Quntity2,
                WeightUnitId = h.Unit2,
                ForwarderId = h.NameOfFreightForwarder,
                VesselRottNo = h.VesselRottNo,
                Remarks = h.Remarks,
                DocReceivedDate = h.DocReceivedDate.HasValue ? h.DocReceivedDate.Value.ToDateFormat() : ""
            })
            .FirstOrDefaultAsync(j => j.JobNo == jobNo);

            if (query1 == null)
                throw new Exception("Job not found");

            return query1;
        }

        public async Task<ShipmentUpdateViewModel> GetJobBottomDetailsAsync(string jobNo, decimal id)
        {
            var status = await _jobUpdateRepository.All().Where(e => e.Tc == id).FirstOrDefaultAsync();

            var data = await _sepDocumentionRepository.All().Where(e => e.JobNo == jobNo).Select(model => new ShipmentUpdateViewModel
            {
                ETADeliveryDate = (DateTime)model.Eta,
                ActualDeliveryDate = model.Ata,
                UnstuffingDate = model.UnStuffingLocationDate,
                ETDDate = model.Etddate,
                PlaceOfLoadingID = model.PlaceOfLoadingId,
                ShedYardID = model.ShedYardId,
                FreightChargeID = model.FreightCharge,
                ShipmentStatus = status != null ? status.UpdateStatus : "",
                AutoId = status != null ? status.Tc : 0m,
                Ldate = status.Ldate,
                ModifyDate = status.ModifyDate,
                Date = status != null && status.UpdateDateTime.HasValue ? status.UpdateDateTime.Value.Date : null,
                Time = status != null && status.UpdateDateTime.HasValue ? TimeOnly.FromDateTime(status.UpdateDateTime.Value) : null,
                UpdateRemarks = status.UpdateRemarks
            }).FirstOrDefaultAsync();

            return data ?? new ShipmentUpdateViewModel();
        }


        public async Task<CommonReturnViewModel> DeleteJobs(List<decimal> ids)
        {
            await _jobUpdateRepository.BeginTransactionAsync();
            try
            {
                if (ids == null || ids.Count == 0)
                {
                    return new CommonReturnViewModel
                    {
                        Success = false,
                        Message = "No records selected for deletion"
                    };
                }

                var recordsToDelete = await _jobUpdateRepository.All()
                    .Where(e => ids.Contains(e.Tc))
                    .ToListAsync();

                if (recordsToDelete.Count == 0)
                {
                    await _jobUpdateRepository.RollbackTransactionAsync();
                    return new CommonReturnViewModel
                    {
                        Success = false,
                        Message = "No records found to delete"
                    };
                }

                // প্রতিটি Job এর জন্য শেষ status ID বের করো
                var jobGroups = recordsToDelete.GroupBy(r => r.JobNo);

                foreach (var jobGroup in jobGroups)
                {
                    var jobNo = jobGroup.Key;

                    // এই job এর সব statuses (delete এবং non-delete দুটোই)
                    var allStatusesForJob = await _jobUpdateRepository.All()
                        .Where(e => e.JobNo == jobNo)
                        .Select(e => new { e.Tc, e.UpdateStatus })
                        .ToListAsync();

                    // সর্বোচ্চ status ID বের করো
                    var maxStatusId = allStatusesForJob
                        .Where(s => int.TryParse(s.UpdateStatus, out _))
                        .Select(s => int.Parse(s.UpdateStatus))
                        .DefaultIfEmpty(0)
                        .Max();

                    // যে statuses delete করতে চাচ্ছো
                    var statusesToDelete = jobGroup
                        .Select(r => new { r.Tc, r.UpdateStatus })
                        .ToList();

                    // যে statuses delete হবে না
                    var statusesNotDeleting = allStatusesForJob
                        .Where(s => !statusesToDelete.Any(std => std.Tc == s.Tc))
                        .Select(s => new { s.Tc, s.UpdateStatus })
                        .ToList();

                    // শেষের statuses (last status থেকে শুরু করে যেগুলো consecutive)
                    var lastStatuses = new List<decimal>();
                    for (int i = maxStatusId; i >= 1; i--)
                    {
                        var statusAtLevel = allStatusesForJob
                            .FirstOrDefault(s => s.UpdateStatus == i.ToString("D2"));

                        if (statusAtLevel == null)
                            break; // gap পেলে থেমে যাও

                        lastStatuses.Add(statusAtLevel.Tc);
                    }

                    // যদি delete করতে যাওয়া records শেষের statuses না হয়, তাহলে error
                    var invalidDeletes = statusesToDelete
                        .Where(std => !lastStatuses.Contains(std.Tc))
                        .ToList();

                    if (invalidDeletes.Any())
                    {
                        await _jobUpdateRepository.RollbackTransactionAsync();
                        return new CommonReturnViewModel
                        {
                            Success = false,
                            Message = $"Job {jobNo}: You can only delete from the last status backwards. Cannot skip middle statuses."
                        };
                    }

                    // প্রথম status (01) delete হচ্ছে কিনা check করো
                    var firstStatusDelete = statusesToDelete
                        .FirstOrDefault(s => s.UpdateStatus == "01");

                    if (firstStatusDelete != null)
                    {
                        var firstStatusCount = allStatusesForJob
                            .Where(s => s.UpdateStatus == "01")
                            .Count();

                        var firstStatusDeleteCount = statusesToDelete
                            .Where(s => s.UpdateStatus == "01")
                            .Count();

                        if (firstStatusCount == firstStatusDeleteCount)
                        {
                            await _jobUpdateRepository.RollbackTransactionAsync();
                            return new CommonReturnViewModel
                            {
                                Success = false,
                                Message = $"Job {jobNo}: Cannot delete the first status '01 - Document Received'. At least one status must remain."
                            };
                        }
                    }
                }

                // সব validation pass হলে delete করো
                foreach (var record in recordsToDelete)
                {
                    await _jobUpdateRepository.DeleteAsync(record);
                }

                await _jobUpdateRepository.CommitTransactionAsync();

                return new CommonReturnViewModel
                {
                    Success = true,
                    Message = $"{recordsToDelete.Count} record(s) deleted successfully"
                };
            }
            catch (Exception ex)
            {
                await _jobUpdateRepository.RollbackTransactionAsync();
                return new CommonReturnViewModel
                {
                    Success = false,
                    Message = "Error occurred while deleting records: " + ex.Message
                };
            }
        }
        public async Task<List<TimelineStepDto>> GetJobTimelineAsync(string jobNo)
        {
            // মাস্টার স্ট্যাটাস লিস্ট (numeric order অনুযায়ী sorted)
            var allStatuses = await _shipmentStatusInfoRepository.All()
                .Select(s => new { s.ShipmentStatusId, s.ShipmentStatus })
                .ToListAsync();

            var orderedStatuses = allStatuses
                .Where(s => int.TryParse(s.ShipmentStatusId, out _))
                .OrderBy(s => int.Parse(s.ShipmentStatusId))
                .ToList();

            // এই জবের জন্য যা যা আপডেট হয়েছে (StatusId + Date)
            var jobUpdates = await _jobUpdateRepository.All()
                .Where(e => e.JobNo == jobNo)
                .Select(e => new { e.UpdateStatus, e.UpdateDateTime })
                .ToListAsync();

            // status-id => সর্বশেষ update datetime ম্যাপ (একই স্ট্যাটাস একাধিকবার আপডেট হলে সর্বশেষটা নিন)
            var completedMap = jobUpdates
                .Where(u => !string.IsNullOrEmpty(u.UpdateStatus) && int.TryParse(u.UpdateStatus, out _))
                .GroupBy(u => u.UpdateStatus)
                .ToDictionary(g => int.Parse(g.Key), g => g.OrderByDescending(x => x.UpdateDateTime).First().UpdateDateTime);

            var maxCompletedId = completedMap.Keys.Any() ? completedMap.Keys.Max() : 0;

            var timeline = new List<TimelineStepDto>();

            foreach (var s in orderedStatuses)
            {
                int stepId = int.Parse(s.ShipmentStatusId);
                string state;

                if (stepId < maxCompletedId)
                    state = "completed";
                else if (stepId == maxCompletedId && completedMap.ContainsKey(stepId))
                    state = "active";
                else
                    state = "pending";

                timeline.Add(new TimelineStepDto
                {
                    StepId = s.ShipmentStatusId,
                    Title = s.ShipmentStatus,
                    State = state,
                    DateText = completedMap.TryGetValue(stepId, out var dt) && dt.HasValue
                        ? dt.Value.ToString("dd MMM, yyyy")
                        : null
                });
            }

            return timeline;
        }
    }
}

