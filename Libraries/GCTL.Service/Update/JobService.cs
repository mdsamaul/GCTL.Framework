//using GCTL.Core.Data;
//using GCTL.Core.ViewModels;
//using GCTL.Core.ViewModels.DailyJobUpdateEntry;
//using GCTL.Core.ViewModels.Update;
//using GCTL.Data.Models;
//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace GCTL.Service.Update
//{
//    public class JobService : IJobService
//    {
//        private readonly IGenericRepository<SepDocumentation> _sepDocumentionRepository;
//        private readonly IGenericRepository<CfDailyJobUpdateEntry> _jobUpdateRepository;
//        private readonly IGenericRepository<CfDefExpenseType> _shipmentModeRepository;
//        private readonly IGenericRepository<SalesCustomer> _customerRepository;
//        private readonly IGenericRepository<SalesDefShipmentStatusInfo> _shipmentStatusInfoRepository;

//        public JobService(
//            IGenericRepository<SepDocumentation> sepDocumentionRepository,
//            IGenericRepository<CfDefExpenseType> shipmentModeRepository,
//            IGenericRepository<SalesCustomer> customerRepository,
//            IGenericRepository<CfDailyJobUpdateEntry> jobUpdateRepository,
//            IGenericRepository<SalesDefShipmentStatusInfo> shipmentStatusInfoRepository)
//        {
//            _sepDocumentionRepository = sepDocumentionRepository;
//            _shipmentModeRepository = shipmentModeRepository;
//            _customerRepository = customerRepository;
//            _jobUpdateRepository = jobUpdateRepository;
//            _shipmentStatusInfoRepository = shipmentStatusInfoRepository;
//        }

//        public async Task<SepPagedResult<JobDto>> GetJobsPagedAsync(
//            int page = 1,
//            int pageSize = 5,
//            string customerId = null,
//            string shipmentMode = null,
//            DateTime? dateFrom = null,
//            DateTime? dateTo = null,
//            string search = null)
//        {
//            try
//            {
//                // Get max status ID to filter completed jobs
//                var allStatusIds = await _shipmentStatusInfoRepository.All()
//                    .Select(s => s.ShipmentStatusId)
//                    .ToListAsync();

//                var maxStatusId = allStatusIds
//                    .Where(s => int.TryParse(s, out _))
//                    .Select(int.Parse)
//                    .DefaultIfEmpty(0)
//                    .Max()
//                    .ToString("D2");

//                var completedJobNos = await _jobUpdateRepository.All()
//                    .Where(e => e.UpdateStatus == maxStatusId)
//                    .Select(e => e.JobNo)
//                    .Distinct()
//                    .ToListAsync();

//                // Main query
//                IQueryable<JobDto> query =
//                    from d in _sepDocumentionRepository.All().AsNoTracking()
//                    where !completedJobNos.Contains(d.JobNo)

//                    join c in _customerRepository.All().AsNoTracking()
//                        on d.CustomerId equals c.CustomerId into customers
//                    from c in customers.DefaultIfEmpty()

//                    join s in _shipmentModeRepository.All().AsNoTracking()
//                        on d.ExpenseTypeId equals s.ExpenseTypeId into shipmentModes
//                    from s in shipmentModes.DefaultIfEmpty()

//                    select new JobDto
//                    {
//                        Id = (short)d.Tc,
//                        JobNo = d.JobNo,
//                        CustomerId = d.CustomerId,
//                        CustomerName = c != null ? c.CustomerName : "",
//                        ShipmentModeId = s != null ? s.ExpenseTypeId : "",
//                        ShipmentMode = s != null ? s.ExpenseType : "",
//                        TransportModeTypeId = s != null ? s.TransportModeTypeId : "",
//                        JobDate = d.Date,
//                        DocReceivedDate = d.DocReceivedDate,
//                        JobDateFormatted = d.Date.HasValue ? d.Date.Value.ToString("dd/MM/yyyy") : "",
//                        DocReceivedDateFormatted = d.DocReceivedDate.HasValue ? d.DocReceivedDate.Value.ToString("dd/MM/yyyy") : "",
//                        JobDateText = d.Date.HasValue ? d.Date.Value.ToString("dd/MM/yyyy") : "",
//                        DocReceivedDateText = d.DocReceivedDate.HasValue ? d.DocReceivedDate.Value.ToString("dd/MM/yyyy") : ""
//                    };

//                var totalCount = await query.CountAsync();

//                // Apply filters
//                if (!string.IsNullOrWhiteSpace(customerId))
//                    query = query.Where(x => x.CustomerId == customerId);

//                if (!string.IsNullOrWhiteSpace(shipmentMode))
//                    query = query.Where(x => x.ShipmentModeId == shipmentMode);

//                if (dateFrom.HasValue)
//                    query = query.Where(x => x.JobDate >= dateFrom.Value);

//                if (dateTo.HasValue)
//                {
//                    var endDate = dateTo.Value.Date.AddDays(1);
//                    query = query.Where(x => x.JobDate < endDate);
//                }

//                if (!string.IsNullOrWhiteSpace(search))
//                {
//                    search = search.Trim();
//                    query = query.Where(x =>
//                        x.JobNo.Contains(search) ||
//                        x.CustomerId.Contains(search) ||
//                        x.CustomerName.Contains(search) ||
//                        x.ShipmentMode.Contains(search) ||
//                        x.ShipmentModeId.Contains(search) ||
//                        x.JobDateFormatted.Contains(search) ||
//                        x.DocReceivedDateFormatted.Contains(search));
//                }

//                var filteredCount = await query.CountAsync();

//                var data = await query
//                    .OrderBy(x => x.JobNo)
//                    .Skip((page - 1) * pageSize)
//                    .Take(pageSize)
//                    .ToListAsync();

//                return new SepPagedResult<JobDto>
//                {
//                    Data = data,
//                    TotalRecords = totalCount,
//                    FilteredRecords = filteredCount
//                };
//            }
//            catch
//            {
//                return new SepPagedResult<JobDto> { Data = new List<JobDto>(), TotalRecords = 0, FilteredRecords = 0 };
//            }
//        }

//        public async Task<JobDetailDto> GetJobDetailAsync(int id)
//        {
//            try
//            {
//                var job = await _sepDocumentionRepository.All()
//                    .AsNoTracking()
//                    .FirstOrDefaultAsync(j => j.Tc == id);

//                if (job == null)
//                    throw new Exception("Job not found");

//                return MapToJobDetailDto(job);
//            }
//            catch
//            {
//                throw;
//            }
//        }

//        public async Task<JobDetailDto> GetJobTopDetailsAsync(string jobNo)
//        {
//            try
//            {
//                var job = await _sepDocumentionRepository.All()
//                    .AsNoTracking()
//                    .FirstOrDefaultAsync(j => j.JobNo == jobNo);

//                if (job == null)
//                    throw new Exception("Job not found");

//                return MapToJobDetailDto(job);
//            }
//            catch
//            {
//                throw;
//            }
//        }

//        private static JobDetailDto MapToJobDetailDto(SepDocumentation h)
//        {
//            return new JobDetailDto
//            {
//                Id = Convert.ToInt16(h.Tc),
//                JobNo = h.JobNo,
//                PrifixId = h.PrifixId,
//                IsCustomJobNo = h.IsCustomJobNo == "1" || h.IsCustomJobNo == "true",
//                JobDate = h.Date.HasValue ? h.Date.Value.ToString("dd/MM/yyyy") : "",

//                LCNo = h.Lcno,
//                LCValue = h.Lcvalue?.ToString(),
//                IPNo = h.Ipno,
//                IPDate = h.Ipdate.HasValue ? h.Ipdate.Value.ToString("dd/MM/yyyy") : "",
//                UndertakingNo = h.UndertakingNo,

//                InvoiceNo = h.InvoiceNo,
//                InvoiceDate = h.InvoiceDate.HasValue ? h.InvoiceDate.Value.ToString("dd/MM/yyyy") : "",
//                InvoiceValue = h.InvoiceValue?.ToString(),
//                InvoiceCurrencyID = h.InvoiceCurrencyId,

//                BillNo = h.BillNo,
//                PONo = h.Pono,

//                ContainerNo = h.ContainerNo,
//                ContainerSize = h.ContainerSize,

//                MaterialDescription = h.MaterialDescription,

//                Quantity = h.Quntity1?.ToString(),
//                QuantityUnitId = h.Unit1,
//                Weight = h.Quntity2?.ToString(),
//                WeightUnitId = h.Unit2,

//                ForwarderId = h.NameOfFreightForwarder,
//                VesselRottNo = h.VesselRottNo,
//                NameOfFreightForwarder = h.NameOfFreightForwarder,

//                ETA = h.Eta.HasValue ? h.Eta.Value.ToString("dd/MM/yyyy") : "",
//                ETB = h.Etb.HasValue ? h.Etb.Value.ToString("dd/MM/yyyy") : "",
//                ATA = h.Ata.HasValue ? h.Ata.Value.ToString("dd/MM/yyyy") : "",
//                ATB = h.Atb.HasValue ? h.Atb.Value.ToString("dd/MM/yyyy") : "",
//                ETDDate = h.Etddate.HasValue ? h.Etddate.Value.ToString("dd/MM/yyyy") : "",
//                ETACTGPORT = h.Etactgport.HasValue ? h.Etactgport.Value.ToString("dd/MM/yyyy") : "",
//                SHIPARRIVEDDATE = h.Shiparriveddate.HasValue ? h.Shiparriveddate.Value.ToString("dd/MM/yyyy") : "",

//                BeNo = h.Beno,
//                BeDate = h.Bedate.HasValue ? h.Bedate.Value.ToString("dd/MM/yyyy") : "",

//                UnStuffingLocationDate = h.UnStuffingLocationDate.HasValue ? h.UnStuffingLocationDate.Value.ToString("dd/MM/yyyy") : "",
//                DateOfClearence = h.DateOfClearence.HasValue ? h.DateOfClearence.Value.ToString("dd/MM/yyyy") : "",
//                DischargeDate = h.Dischargedate.HasValue ? h.Dischargedate.Value.ToString("dd/MM/yyyy") : "",

//                ShipmentStatus = h.ShipmentStatus,
//                Remarks = h.Remarks,

//                CompanyCode = h.CompanyCode,
//                EmployeeID = h.EmployeeId,

//                SupplierId = h.SupplierId,
//                PortId = h.PortId,
//                PortNameId = h.PortNameId,

//                CustomerId = h.CustomerId,
//                CustomerName = h.CustomerId,
//                CustomerAddress = h.CustomerAddress,
//                CustomerID_Importer = h.CustomerIdImporter,
//                DeliveryLocationCode = h.DeliveryLocationCode,
//                CustomerDeliveryAddress = h.DeliveryLocationCode,

//                DocsReceivedDate = h.DocReceivedDate.HasValue ? h.DocReceivedDate.Value.ToString("dd/MM/yyyy") : "",
//                DocReceivedDate = h.DocReceivedDate.HasValue ? h.DocReceivedDate.Value.ToString("dd/MM/yyyy") : "",

//                TransportCost = h.TransportCost?.ToString(),
//                VehicleID = h.VehicleId,
//                Vehicle = h.Vehicle,
//                Vendor = h.Vendor,

//                DocumentationId = h.DocumentationId,
//                CurrencyId = h.CurrencyId,
//                ConsoleNo = h.ConsoleNo,
//                ExpenseTypeID = h.ExpenseTypeId,

//                IsEdit = h.IsEdit,

//                BDTRate = h.Bdtrate?.ToString(),
//                MinimumAgencyCom = h.MinimumAgencyCom?.ToString(),
//                CommOnInvoiceValue = h.CommOnInvoiceValue?.ToString(),

//                BlNo = h.Beno,
//                BlDate = h.Bldate.HasValue ? h.Bldate.Value.ToString("dd/MM/yyyy") : "",
//                LcDate = h.Lcdate.HasValue ? h.Lcdate.Value.ToString("dd/MM/yyyy") : "",
//                LcExpNo = h.ExpNo,

//                DocumentTypeID = h.DocumentTypeId,

//                HAWB = h.Hawb,
//                MAWB = h.Mawb,
//                LcaNo = h.Lcano,

//                PlaceOfLoadingID = h.PlaceOfLoadingId,
//                ShedYardID = h.ShedYardId,
//                FreightCharge = h.FreightCharge,

//                DocumentHandoverDate = h.DocumentHandoverDate.HasValue ? h.DocumentHandoverDate.Value.ToString("dd/MM/yyyy") : "",
//                CargoHandoverDate = h.CargoHandoverDate.HasValue ? h.CargoHandoverDate.Value.ToString("dd/MM/yyyy") : "",
//                DepotID = h.DepotId,
//                BookingConfirmationReference = h.BookingConfirmationReference,

//                ImporterId = h.ImporterId,
//                ExporterID = h.ExporterId,

//                PackingQuantity = h.PackingQuantity?.ToString(),
//                PackingUnitType = h.PackingUnitType,

//                AssessmentNo = h.AssessmentNo,
//                RNo = h.Rno,
//                ClientStationwiseSLNo = h.ClientStationwiseSlno,

//                ExpNo = h.ExpNo,
//                ExpDate = h.ExpDate.HasValue ? h.ExpDate.Value.ToString("dd/MM/yyyy") : "",

//                AssessableRate = h.AssessableRate?.ToString(),
//                AssessableValue = h.AssessableValue?.ToString(),

//                ShipmentMode = h.ExpenseTypeId,
//                ShipmentModeId = h.ExpenseTypeId,

//                CreatedAt = h.Ldate,
//                UpdatedAt = h.ModifyDate
//            };
//        }

//        public async Task<SepPagedResult<JobStatusDto>> GetJobStatusesPagedAsync(
//            int page = 1,
//            int pageSize = 10,
//            string search = null,
//            string customerCode = null,
//            DateTime? fromDate = null,
//            DateTime? toDate = null)
//        {
//            try
//            {
//                var query =
//                    from c in _jobUpdateRepository.All().AsNoTracking()
//                    join s in _sepDocumentionRepository.All().AsNoTracking()
//                        on c.JobNo equals s.JobNo into sj
//                    from s in sj.DefaultIfEmpty()
//                    join sm in _shipmentStatusInfoRepository.All().AsNoTracking()
//                        on c.UpdateStatus equals sm.ShipmentStatusId into shipmentStatuses
//                    from sm in shipmentStatuses.DefaultIfEmpty()
//                    select new { JobUpdate = c, Document = s, StatusInfo = sm };

//                var totalCount = await query.CountAsync();

//                // Apply filters
//                if (!string.IsNullOrWhiteSpace(customerCode))
//                    query = query.Where(x => x.Document.CustomerId == customerCode);

//                if (fromDate.HasValue)
//                    query = query.Where(x => x.JobUpdate.UpdateDateTime >= fromDate.Value);

//                if (toDate.HasValue)
//                {
//                    var endDate = toDate.Value.Date.AddDays(1);
//                    query = query.Where(x => x.JobUpdate.UpdateDateTime < endDate);
//                }

//                if (!string.IsNullOrWhiteSpace(search))
//                {
//                    search = search.Trim().ToLower();
//                    query = query.Where(x =>
//                        x.JobUpdate.JobNo.ToLower().Contains(search) ||
//                        x.JobUpdate.UpdateStatus.ToLower().Contains(search));
//                }

//                var filteredCount = await query.CountAsync();

//                var rawData = await query
//                    .OrderByDescending(x => x.JobUpdate.Tc)
//                    .Skip((page - 1) * pageSize)
//                    .Take(pageSize)
//                    .Select(x => new
//                    {
//                        x.JobUpdate.Tc,
//                        x.JobUpdate.DailyJobUpdateEntryId,
//                        x.JobUpdate.JobNo,
//                        x.JobUpdate.UpdateStatus,
//                        x.JobUpdate.UpdateDateTime,
//                        StatusName = x.StatusInfo != null ? x.StatusInfo.ShipmentStatus : x.JobUpdate.UpdateStatus
//                    })
//                    .ToListAsync();

//                var data = rawData.Select(x => new JobStatusDto
//                {
//                    Id = x.Tc,
//                    JobUpdateCode = x.DailyJobUpdateEntryId,
//                    Status = "Success",
//                    JobNo = x.JobNo,
//                    ShipmentStatus = x.StatusName,
//                    DateTime = x.UpdateDateTime,
//                    DateTimeText = x.UpdateDateTime.HasValue ? x.UpdateDateTime.Value.ToString("dd/MM/yyyy hh:mm tt") : ""
//                }).ToList();

//                return new SepPagedResult<JobStatusDto> { Data = data, TotalRecords = totalCount, FilteredRecords = filteredCount };
//            }
//            catch
//            {
//                return new SepPagedResult<JobStatusDto> { Data = new List<JobStatusDto>(), TotalRecords = 0, FilteredRecords = 0 };
//            }
//        }

//        public async Task<ShipmentUpdateViewModel> GetJobBottomDetailsAsync(string jobNo, decimal id)
//        {
//            try
//            {
//                var status = await _jobUpdateRepository.All()
//                    .AsNoTracking()
//                    .FirstOrDefaultAsync(e => e.Tc == id);

//                var document = await _sepDocumentionRepository.All()
//                    .AsNoTracking()
//                    .FirstOrDefaultAsync(e => e.JobNo == jobNo);

//                if (document == null)
//                    return new ShipmentUpdateViewModel();

//                return new ShipmentUpdateViewModel
//                {
//                    JobNo = document.JobNo,
//                    ETADeliveryDate = document.Eta,
//                    ActualDeliveryDate = document.Ata,
//                    UnstuffingDate = document.UnStuffingLocationDate,
//                    ETDDate = document.Etddate,
//                    PlaceOfLoadingID = document.PlaceOfLoadingId,
//                    ShedYardID = document.ShedYardId,
//                    FreightCharge = document.FreightCharge,
//                    ShipmentStatus = status != null ? status.UpdateStatus : "",
//                    AutoId = status != null ? status.Tc : 0m,
//                    LDate = status?.Ldate,
//                    ModifyDate = status?.ModifyDate,
//                    JobUpdateDate = status != null && status.UpdateDateTime.HasValue ? status.UpdateDateTime.Value.Date : null,
//                    Time = status != null && status.UpdateDateTime.HasValue ? TimeOnly.FromDateTime(status.UpdateDateTime.Value) : null,
//                    UpdateRemarks = status?.UpdateRemarks
//                };
//            }
//            catch
//            {
//                return new ShipmentUpdateViewModel();
//            }
//        }

//        public async Task<CommonReturnViewModel> UpdateJobs(ShipmentUpdateViewModel model, string company = null)
//        {
//            await _sepDocumentionRepository.BeginTransactionAsync();
//            try
//            {
//                var document = await _sepDocumentionRepository.All()
//                    .FirstOrDefaultAsync(e => e.JobNo == model.JobNo);

//                bool isNew = document == null;

//                if (isNew)
//                {
//                    document = new SepDocumentation { JobNo = model.JobNo };
//                }

//                // Map all fields from ViewModel to entity
//                document.PrifixId = model.PrifixId;
//                document.IsCustomJobNo = model.IsCustomJobNo ? "1" : "0";
//                document.Date = model.Date;
//                document.Lcno = model.LCNo;
//                document.Lcvalue = model.LCValue;
//                document.Ipno = model.IPNo;
//                document.Ipdate = model.IPDate;
//                document.UndertakingNo = model.UndertakingNo;
//                document.InvoiceNo = model.InvoiceNo;
//                document.InvoiceDate = model.InvoiceDate;
//                document.InvoiceValue = model.InvoiceValue;
//                document.InvoiceCurrencyId = model.InvoiceCurrencyID;
//                document.BillNo = model.BillNo;
//                document.Pono = model.PONo;
//                document.ContainerNo = model.ContainerNo;
//                document.ContainerSize = model.ContainerSize;
//                document.MaterialDescription = model.MaterialDescription;
//                document.Quntity1 = model.Quntity1;
//                document.Unit1 = model.Unit1;
//                document.Quntity2 = model.Quntity2;
//                document.Unit2 = model.Unit2;
//                document.FwdrShippingAgent = model.FWDR_ShippingAgent;
//                document.VesselRottNo = model.Vessel_RottNo;
//                document.NameOfFreightForwarder = model.NameOfFreightForwarder;
//                document.Eta = model.ETA;
//                document.Etb = model.ETB;
//                document.Ata = model.ATA;
//                document.Atb = model.ATB;
//                document.Etddate = model.ETDDate;
//                document.Etactgport = model.ETACTGPORT;
//                document.Shiparriveddate = model.SHIPARRIVEDDATE;
//                document.Beno = model.BENo;
//                document.Bedate = model.BEDate;
//                document.UnStuffingLocationDate = model.Un_StuffingLocationDate;
//                document.DateOfClearence = model.DateOfClearence;
//                document.Dischargedate = model.Dischargedate;
//                document.ShipmentStatus = model.ShipmentStatus;
//                document.Remarks = model.Remarks;
//                document.CompanyCode = company ?? document.CompanyCode;
//                document.EmployeeId = model.EmployeeID;
//                document.SupplierId = model.SupplierId;
//                document.PortId = model.PortId;
//                document.PortNameId = model.PortNameId;
//                document.CustomerId = model.CustomerID;
//                document.CustomerAddress = model.CustomerAddress;
//                document.CustomerIdImporter = model.CustomerID_Importer;
//                document.DeliveryLocationCode = model.DeliveryLocationCode ?? model.CustomerDeliveryAddress;
//                document.DocReceivedDate = model.DocReceivedDate;
//                document.TransportCost = model.TransportCost;
//                document.VehicleId = model.VehicleID;
//                document.Vehicle = model.Vehicle;
//                document.Vendor = model.Vendor;
//                document.DocumentationId = model.DocumentationId;
//                document.CurrencyId = model.CurrencyId;
//                document.ConsoleNo = model.ConsoleNo;
//                document.ExpenseTypeId = model.ExpenseTypeID ?? model.ShipmentModeId;
//                document.IsEdit = model.IsEdit ?? true;
//                document.Bdtrate = model.BDTRate;
//                document.MinimumAgencyCom = model.MinimumAgencyCom;
//                document.CommOnInvoiceValue = model.CommOnInvoiceValue;
//                document.Bldate = model.BLDate;
//                document.Lcdate = model.LCDate;
//                document.DocumentTypeId = model.DocumentTypeID;
//                document.Hawb = model.HAWB;
//                document.Mawb = model.MAWB;
//                document.Lcano = model.LCANo;
//                document.PlaceOfLoadingId = model.PlaceOfLoadingID;
//                document.ShedYardId = model.ShedYardID;
//                document.FreightCharge = model.FreightCharge;
//                document.DocumentHandoverDate = model.DocumentHandoverDate;
//                document.CargoHandoverDate = model.CargoHandoverDate;
//                document.DepotId = model.DepotID;
//                document.BookingConfirmationReference = model.BookingConfirmationReference;
//                document.ImporterId = model.ImporterID;
//                document.ExporterId = model.ExporterID;
//                document.PackingQuantity = model.PackingQuantity;
//                document.PackingUnitType = model.PackingUnitType;
//                document.AssessmentNo = model.AssessmentNo;
//                document.Rno = model.RNo;
//                document.ClientStationwiseSlno = model.ClientStationwiseSLNo;
//                document.ExpNo = model.ExpNo;
//                document.ExpDate = model.ExpDate;
//                document.AssessableRate = model.AssessableRate;
//                document.AssessableValue = model.AssessableValue;
//                document.Luser = model.LUser;
//                document.Lip = model.LIP;
//                document.Lmac = model.LMAC;

//                if (isNew)
//                {
//                    document.Ldate = DateTime.Now;
//                    await _sepDocumentionRepository.AddAsync(document);
//                }
//                else
//                {
//                    document.ModifyDate = DateTime.Now;
//                    await _sepDocumentionRepository.UpdateAsync(document);
//                }

//                // Handle shipment status update entry
//                if (!string.IsNullOrEmpty(model.ShipmentStatus))
//                {
//                    var allStatuses = await _shipmentStatusInfoRepository.All()
//                        .Select(s => new { s.ShipmentStatusId, s.ShipmentStatus })
//                        .ToListAsync();

//                    var orderedStatuses = allStatuses
//                        .Where(s => int.TryParse(s.ShipmentStatusId, out _))
//                        .OrderBy(s => int.Parse(s.ShipmentStatusId))
//                        .ToList();

//                    if (orderedStatuses.Any())
//                    {
//                        var existingStatusIds = await _jobUpdateRepository.All()
//                            .Where(e => e.JobNo == model.JobNo && e.Tc != model.AutoId)
//                            .Select(e => e.UpdateStatus)
//                            .ToListAsync();

//                        bool hasExistingRecord = existingStatusIds.Any();
//                        var firstStatus = orderedStatuses.First();

//                        bool statusValid = true;
//                        string statusMessage = "";

//                        if (!hasExistingRecord)
//                        {
//                            if (model.ShipmentStatus != firstStatus.ShipmentStatusId)
//                            {
//                                statusValid = false;
//                                statusMessage = $"First status must be '{firstStatus.ShipmentStatusId} - {firstStatus.ShipmentStatus}'.";
//                            }
//                        }
//                        else
//                        {
//                            if (existingStatusIds.Contains(model.ShipmentStatus))
//                            {
//                                statusValid = false;
//                                statusMessage = "This status has already been saved for this job.";
//                            }
//                        }

//                        if (!statusValid)
//                        {
//                            await _sepDocumentionRepository.RollbackTransactionAsync();
//                            return new CommonReturnViewModel { Success = false, Message = statusMessage };
//                        }

//                        DateTime updateDateTime = CombineDateAndTime(model.JobUpdateDate, model.Time);

//                        if (model.AutoId > 0)
//                        {
//                            var prevData = await _jobUpdateRepository.All()
//                                .FirstOrDefaultAsync(e => e.Tc == model.AutoId);

//                            if (prevData != null)
//                            {
//                                prevData.UpdateStatus = model.ShipmentStatus;
//                                prevData.UpdateDateTime = updateDateTime;
//                                prevData.ModifyDate = DateTime.Now;
//                                prevData.Lip = model.LIP;
//                                prevData.Lmac = model.LMAC;
//                                prevData.EmployeeId = model.LUser ?? "";
//                                prevData.Luser = model.LUser;
//                                prevData.UpdateRemarks = model.UpdateRemarks;

//                                await _jobUpdateRepository.UpdateAsync(prevData);
//                            }
//                        }
//                        else
//                        {
//                            var updateEntry = new CfDailyJobUpdateEntry
//                            {
//                                JobNo = model.JobNo,
//                                DailyJobUpdateEntryId = await GetLastJobEntryCode(),
//                                UpdateStatus = model.ShipmentStatus,
//                                UpdateDateTime = updateDateTime,
//                                Ldate = DateTime.Now,
//                                Lip = model.LIP,
//                                Lmac = model.LMAC,
//                                EmployeeId = model.LUser ?? "",
//                                Luser = model.LUser,
//                                CompanyCode = company ?? "",
//                                UpdateRemarks = model.UpdateRemarks
//                            };

//                            await _jobUpdateRepository.AddAsync(updateEntry);
//                        }
//                    }
//                }

//                await _sepDocumentionRepository.CommitTransactionAsync();

//                return new CommonReturnViewModel
//                {
//                    Success = true,
//                    Message = "Update Success",
//                    Data = document.JobNo
//                };
//            }
//            catch (Exception ex)
//            {
//                await _sepDocumentionRepository.RollbackTransactionAsync();
//                return new CommonReturnViewModel { Success = false, Message = "Something went wrong: " + ex.Message };
//            }
//        }

//        public async Task<CommonReturnViewModel> DeleteJobs(List<decimal> ids)
//        {
//            await _jobUpdateRepository.BeginTransactionAsync();
//            try
//            {
//                if (ids == null || ids.Count == 0)
//                    return new CommonReturnViewModel { Success = false, Message = "No records selected for deletion" };

//                var recordsToDelete = await _jobUpdateRepository.All()
//                    .Where(e => ids.Contains(e.Tc))
//                    .ToListAsync();

//                if (recordsToDelete.Count == 0)
//                {
//                    await _jobUpdateRepository.RollbackTransactionAsync();
//                    return new CommonReturnViewModel { Success = false, Message = "No records found to delete" };
//                }

//                var jobGroups = recordsToDelete.GroupBy(r => r.JobNo);

//                foreach (var jobGroup in jobGroups)
//                {
//                    var jobNo = jobGroup.Key;

//                    var allStatusesForJob = await _jobUpdateRepository.All()
//                        .Where(e => e.JobNo == jobNo)
//                        .Select(e => new { e.Tc, e.UpdateStatus })
//                        .ToListAsync();

//                    var maxStatusId = allStatusesForJob
//                        .Where(s => int.TryParse(s.UpdateStatus, out _))
//                        .Select(s => int.Parse(s.UpdateStatus))
//                        .DefaultIfEmpty(0)
//                        .Max();

//                    var statusesToDelete = jobGroup.Select(r => new { r.Tc, r.UpdateStatus }).ToList();

//                    var lastStatuses = new List<decimal>();
//                    for (int i = maxStatusId; i >= 1; i--)
//                    {
//                        var statusAtLevel = allStatusesForJob
//                            .FirstOrDefault(s => s.UpdateStatus == i.ToString("D2"));
//                        if (statusAtLevel == null) break;
//                        lastStatuses.Add(statusAtLevel.Tc);
//                    }

//                    var invalidDeletes = statusesToDelete
//                        .Where(std => !lastStatuses.Contains(std.Tc))
//                        .ToList();

//                    if (invalidDeletes.Any())
//                    {
//                        await _jobUpdateRepository.RollbackTransactionAsync();
//                        return new CommonReturnViewModel
//                        {
//                            Success = false,
//                            Message = $"Job {jobNo}: You can only delete from the last status backwards."
//                        };
//                    }

//                    var firstStatusDelete = statusesToDelete.FirstOrDefault(s => s.UpdateStatus == "01");
//                    if (firstStatusDelete != null)
//                    {
//                        var firstStatusCount = allStatusesForJob.Count(s => s.UpdateStatus == "01");
//                        var firstStatusDeleteCount = statusesToDelete.Count(s => s.UpdateStatus == "01");

//                        if (firstStatusCount == firstStatusDeleteCount)
//                        {
//                            await _jobUpdateRepository.RollbackTransactionAsync();
//                            return new CommonReturnViewModel
//                            {
//                                Success = false,
//                                Message = $"Job {jobNo}: Cannot delete the first status. At least one status must remain."
//                            };
//                        }
//                    }
//                }

//                foreach (var record in recordsToDelete)
//                    await _jobUpdateRepository.DeleteAsync(record);

//                await _jobUpdateRepository.CommitTransactionAsync();

//                return new CommonReturnViewModel
//                {
//                    Success = true,
//                    Message = $"{recordsToDelete.Count} record(s) deleted successfully"
//                };
//            }
//            catch (Exception ex)
//            {
//                await _jobUpdateRepository.RollbackTransactionAsync();
//                return new CommonReturnViewModel
//                {
//                    Success = false,
//                    Message = "Error occurred while deleting records: " + ex.Message
//                };
//            }
//        }

//        public async Task<List<TimelineStepDto>> GetJobTimelineAsync(string jobNo)
//        {
//            try
//            {
//                var allStatuses = await _shipmentStatusInfoRepository.All()
//                    .Select(s => new { s.ShipmentStatusId, s.ShipmentStatus })
//                    .ToListAsync();

//                var orderedStatuses = allStatuses
//                    .Where(s => int.TryParse(s.ShipmentStatusId, out _))
//                    .OrderBy(s => int.Parse(s.ShipmentStatusId))
//                    .ToList();

//                var jobUpdates = await _jobUpdateRepository.All()
//                    .AsNoTracking()
//                    .Where(e => e.JobNo == jobNo)
//                    .Select(e => new { e.UpdateStatus, e.UpdateDateTime })
//                    .ToListAsync();

//                var completedMap = jobUpdates
//                    .Where(u => !string.IsNullOrEmpty(u.UpdateStatus) && int.TryParse(u.UpdateStatus, out _))
//                    .GroupBy(u => u.UpdateStatus)
//                    .ToDictionary(
//                        g => int.Parse(g.Key),
//                        g => g.OrderByDescending(x => x.UpdateDateTime).First().UpdateDateTime);

//                var maxCompletedId = completedMap.Keys.Any() ? completedMap.Keys.Max() : 0;

//                var timeline = new List<TimelineStepDto>();

//                foreach (var s in orderedStatuses)
//                {
//                    int stepId = int.Parse(s.ShipmentStatusId);
//                    string state = stepId < maxCompletedId ? "completed"
//                        : (stepId == maxCompletedId && completedMap.ContainsKey(stepId)) ? "active"
//                        : "pending";

//                    timeline.Add(new TimelineStepDto
//                    {
//                        StepId = s.ShipmentStatusId,
//                        Title = s.ShipmentStatus,
//                        State = state,
//                        DateText = completedMap.TryGetValue(stepId, out var dt) && dt.HasValue
//                            ? dt.Value.ToString("dd MMM, yyyy")
//                            : null
//                    });
//                }

//                return timeline;
//            }
//            catch
//            {
//                return new List<TimelineStepDto>();
//            }
//        }

//        private DateTime CombineDateAndTime(DateTime? date, TimeOnly? time)
//        {
//            if (date == null) date = DateTime.Now.Date;
//            if (time == null) return date.Value.Date;
//            return date.Value.Date.Add(time.Value.ToTimeSpan());
//        }

//        private async Task<string> GetLastJobEntryCode()
//        {
//            var lastCode = await _jobUpdateRepository.All()
//                .OrderByDescending(e => e.Tc)
//                .Select(e => new { e.Tc, e.DailyJobUpdateEntryId })
//                .FirstOrDefaultAsync();

//            if (lastCode == null) return "00000001";

//            if (!int.TryParse(lastCode.DailyJobUpdateEntryId, out int numericCode))
//                return "00000001";

//            numericCode++;
//            return numericCode.ToString("D8");
//        }
//    }
//}