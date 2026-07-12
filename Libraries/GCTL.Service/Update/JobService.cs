using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Spreadsheet;
using GCTL.Core.Data;
using GCTL.Core.ViewModels;
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
using GCTL.Data.Models;
using GCTL.Core.ViewModels.Update;
using GCTL.Core.ViewModels.Update;


namespace GCTL.Service.Update
{
    public class JobService : IJobService
    {
        private readonly IGenericRepository<GCTL.Data.Models.SepDocumentation> _sepDocumentionRepository;
        private readonly IGenericRepository<GCTL.Data.Models.CfDailyJobUpdateEntry> _jobUpdateRepository;
        private readonly IGenericRepository<GCTL.Data.Models.CfDefExpenseType> _shipmentModeRepository;
        private readonly IGenericRepository<GCTL.Data.Models.SalesCustomer> _customerRepository;



        #region static

        //private static readonly List<JobDto> _staticJobs = new()
        //{
        //    new JobDto { Id = 1, JobNo = "JOB-001", ShipmentMode = "Sea", JobDate = "2025-12-01", CustomerName = "ABC Corp", DocReceivedDate = "2025-12-05" },
        //    new JobDto { Id = 1, JobNo = "JOB-002", ShipmentMode = "Air", JobDate = "2025-12-10", CustomerName = "XYZ Ltd", DocReceivedDate = "2025-12-12" },
        //    new JobDto { Id = 1, JobNo = "JOB-003", ShipmentMode = "Sea", JobDate = "2025-12-15", CustomerName = "ABC Corp", DocReceivedDate = null },
        //    new JobDto { Id = 1,  JobNo = "JOB-004", ShipmentMode = "Land", JobDate = "2025-12-20", CustomerName = "Global Traders", DocReceivedDate = "2025-12-21" },
        //    new JobDto {Id = 1,  JobNo = "JOB-005", ShipmentMode = "Air", JobDate = "2025-12-22", CustomerName = "XYZ Ltd", DocReceivedDate = "2025-12-23" },
        //    // Add more if you want to test pagination
        //    new JobDto {Id = 1,  JobNo = "JOB-006", ShipmentMode = "Sea", JobDate = "2025-12-24", CustomerName = "ABC Corp", DocReceivedDate = "2025-12-25" },
        //    new JobDto {Id = 1,  JobNo = "JOB-007", ShipmentMode = "Air", JobDate = "2025-12-25", CustomerName = "New Client", DocReceivedDate = null },
        //    new JobDto {Id = 1,  JobNo = "JOB-001", ShipmentMode = "Sea", JobDate = "2025-12-01", CustomerName = "ABC Corp", DocReceivedDate = "2025-12-05" },
        //    new JobDto {Id = 1,  JobNo = "JOB-002", ShipmentMode = "Air", JobDate = "2025-12-10", CustomerName = "XYZ Ltd", DocReceivedDate = "2025-12-12" },
        //    new JobDto {Id = 1,  JobNo = "JOB-003", ShipmentMode = "Sea", JobDate = "2025-12-15", CustomerName = "ABC Corp", DocReceivedDate = null },
        //    new JobDto {Id = 1,  JobNo = "JOB-004", ShipmentMode = "Land", JobDate = "2025-12-20", CustomerName = "Global Traders", DocReceivedDate = "2025-12-21" },
        //    new JobDto {Id = 1,  JobNo = "JOB-005", ShipmentMode = "Air", JobDate = "2025-12-22", CustomerName = "XYZ Ltd", DocReceivedDate = "2025-12-23" },
        //    // Add more if you want to test pagination
        //    new JobDto {Id = 1,  JobNo = "JOB-006", ShipmentMode = "Sea", JobDate = "2025-12-24", CustomerName = "ABC Corp", DocReceivedDate = "2025-12-25" },
        //    new JobDto {Id = 1,  JobNo = "JOB-007", ShipmentMode = "Air", JobDate = "2025-12-25", CustomerName = "New Client", DocReceivedDate = null }
        //};

    //    private static readonly List<JobDetailDto> staticDetails = new List<JobDetailDto>
    //        {
    //            new JobDetailDto
    //            {
    //                Id = 1,
    //                JobNo = "JOB-001",
    //                ShipmentMode = "Sea",
    //                //ShipmentModeId = 2,
    //                //IsCustomJobNo = false,
    //                JobDate = "2025-12-01",
    //               // CustomerId = 101,
    //                CustomerName = "ABC Corp",
    //                CustomerDeliveryAddress = "123 Main St, Dhaka",
    //               // PortId = 5,
    //                DocsReceivedDate = "2025-12-05",
    //                LcExpNo = "LC-2025-001",
    //                LcValue = 500000,
    //                IpEpNo = "IP-2025-001",
    //                IpDate = "2025-11-20",
    //               // ImporterId = 201,
    //                InvoiceNo = "INV-001",
    //                InvoiceDate = "2025-12-01",
    //                BlNo = "BL-SEA-001",
    //                BeNo = "BL-SEA-001",
    //                BeDate = "2025-12-10",
    //                BlDate = "2025-12-10",
    //                ContainerNo = "CONT-12345",
    //                ContainerSize = "40FT",
    //                LcaNo = "40FT",
    //                DischargeDate = "2025-12-15",
    //                MaterialDescription = "Cotton Fabric",
    //                Quantity = 1000,
    //               // QuantityUnitId = 3, // e.g., Rolls
    //                Weight = 25000,
    //               // WeightUnitId = 1, // KG
    //               // ForwarderId = 301,
    //                VesselRottNo = "VSL-ROT-2025-12",
    //                Remarks = "Urgent delivery required",
    //                DocReceivedDate = "2025-12-01",

    //            },

    //        };


    //    private static readonly List<JobStatusDto> _staticStatuses = new()
    //{
    //    new JobStatusDto { Id = 1, Status = "Success", JobNo = "JOB-001", ShipmentStatus = "Documents Received", DateTime = "2025-12-05 10:30" },
    //    new JobStatusDto { Id = 2, Status = "Warning", JobNo = "JOB-001", ShipmentStatus = "In Customs", DateTime = "2025-12-10 14:15" },
    //    new JobStatusDto { Id = 3, Status = "Success", JobNo = "JOB-002", ShipmentStatus = "BL Received", DateTime = "2025-12-12 09:00" },
    //    new JobStatusDto { Id = 4, Status = "Danger", JobNo = "JOB-001", ShipmentStatus = "Customs Hold", DateTime = "2025-12-15 11:45" },
    //    new JobStatusDto { Id = 5, Status = "Success", JobNo = "JOB-003", ShipmentStatus = "Discharged", DateTime = "2025-12-18 16:20" },
    //    new JobStatusDto { Id = 6, Status = "Success", JobNo = "JOB-002", ShipmentStatus = "Delivered", DateTime = "2025-12-20 13:10" },
    //    new JobStatusDto { Id = 7, Status = "Warning", JobNo = "JOB-004", ShipmentStatus = "Pending Payment", DateTime = "2025-12-22 08:55" },
    //    // Add more rows as needed
    //};

        #endregion

        public JobService(IGenericRepository<GCTL.Data.Models.SepDocumentation> sepDocumentionRepository, IGenericRepository<GCTL.Data.Models.CfDefExpenseType> shipmentModeRepository, IGenericRepository<GCTL.Data.Models.SalesCustomer> customerRepository, IGenericRepository<GCTL.Data.Models.CfDailyJobUpdateEntry> jobUpdateRepository)
        {
            _sepDocumentionRepository = sepDocumentionRepository;
            _shipmentModeRepository = shipmentModeRepository;
            _customerRepository = customerRepository;
            _jobUpdateRepository = jobUpdateRepository;
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
                IQueryable<JobDto> query =
                    from d in _sepDocumentionRepository.All().AsNoTracking()

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

                        // dd/MM/yyyy built with translatable string ops only (no PadLeft)
                        JobDateFormatted = d.Date != null ?
                            (d.Date.Value.Day < 10 ? "0" + d.Date.Value.Day.ToString() : d.Date.Value.Day.ToString()) + "/" +
                            (d.Date.Value.Month < 10 ? "0" + d.Date.Value.Month.ToString() : d.Date.Value.Month.ToString()) + "/" +
                            d.Date.Value.Year.ToString()
                            : "",

                        DocReceivedDateFormatted = d.DocReceivedDate != null ?
                            (d.DocReceivedDate.Value.Day < 10 ? "0" + d.DocReceivedDate.Value.Day.ToString() : d.DocReceivedDate.Value.Day.ToString()) + "/" +
                            (d.DocReceivedDate.Value.Month < 10 ? "0" + d.DocReceivedDate.Value.Month.ToString() : d.DocReceivedDate.Value.Month.ToString()) + "/" +
                            d.DocReceivedDate.Value.Year.ToString()
                            : ""
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

                //----------------------------------------
                // Global Search (text + partial/full dd/MM/yyyy date)
                //----------------------------------------
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

                foreach (var item in data)
                {
                    item.JobDateText = item.JobDate?.ToString("dd/MM/yyyy") ?? "";
                    item.DocReceivedDateText = item.DocReceivedDate?.ToString("dd/MM/yyyy") ?? "";
                }

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
                 JobDate = h.Date.Value.ToShortDateString(),
                 CustomerId = h.CustomerId,
                 CustomerName = h.CustomerId,
                 CustomerAddress = h.CustomerAddress,
                 CustomerDeliveryAddress = h.DeliveryLocationCode, //TODO: DElivery add
                 PortId = h.PortId,
                 DocsReceivedDate = h.DocReceivedDate.Value.ToShortDateString(),
                 LcExpNo = h.ExpNo,
                 LcValue = h.Lcvalue,
                 //LcUnitId= h.lc
                 IpEpNo = h.Ipno,
                 IpDate = h.Ipdate.Value.ToString(),
                 ImporterId = h.ImporterId,
                 InvoiceNo = h.InvoiceNo,
                 InvoiceDate = h.InvoiceDate.Value.ToString(),
                 BlNo = h.Beno,
                 BeNo = h.Beno,
                 BeDate = h.Bedate.Value.ToString(),
                 BlDate = h.Bldate.Value.ToString(),
                 ContainerNo = h.ContainerNo,
                 ContainerSize = h.ContainerSize,
                 LcaNo = h.Lcano,
                 DischargeDate = h.Dischargedate.Value.ToString(),
                 MaterialDescription = h.MaterialDescription,
                 Quantity = h.Quntity1,
                 QuantityUnitId = h.Unit1,
                 Weight = h.Quntity2,
                 WeightUnitId = h.Unit2,
                 ForwarderId = h.NameOfFreightForwarder,
                 VesselRottNo = h.VesselRottNo,
                 Remarks = h.Remarks,
                 DocReceivedDate = h.DocReceivedDate.Value.ToString(),
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
                    select new
                    {
                        JobUpdate = c,
                        Document = s
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

                //----------------------------------------
                // Global Search (text + partial/full dd/MM/yyyy date)
                //----------------------------------------
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.Trim().ToLower();

                    query = query.Where(x =>
                        x.JobUpdate.JobNo.ToLower().Contains(search) ||
                        x.JobUpdate.UpdateStatus.ToLower().Contains(search) ||
                        // dd/MM/yyyy built with translatable ternary ops only (no PadLeft, no ToString(format))
                        (x.JobUpdate.UpdateDateTime != null &&
                            (
                                (x.JobUpdate.UpdateDateTime.Value.Day < 10 ? "0" + x.JobUpdate.UpdateDateTime.Value.Day.ToString() : x.JobUpdate.UpdateDateTime.Value.Day.ToString()) + "/" +
                                (x.JobUpdate.UpdateDateTime.Value.Month < 10 ? "0" + x.JobUpdate.UpdateDateTime.Value.Month.ToString() : x.JobUpdate.UpdateDateTime.Value.Month.ToString()) + "/" +
                                x.JobUpdate.UpdateDateTime.Value.Year.ToString()
                            ).Contains(search)));
                }

                //----------------------------------------
                // Filtered Count
                //----------------------------------------
                var filteredCount = await query.CountAsync();

                //----------------------------------------
                // Paging
                //----------------------------------------
                var data = await query
                    .OrderByDescending(x => x.JobUpdate.Tc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new JobStatusDto
                    {
                        Id = x.JobUpdate.Tc,
                        JobUpdateCode = x.JobUpdate.DailyJobUpdateEntryId,
                        Status = "Success",
                        JobNo = x.JobUpdate.JobNo,
                        ShipmentStatus = x.JobUpdate.UpdateStatus,
                        DateTime = x.JobUpdate.UpdateDateTime
                    })
                    .ToListAsync();

                //----------------------------------------
                // Format after SQL execution (client-side, safe here since only current page's rows)
                //----------------------------------------
                foreach (var item in data)
                {
                    item.DateTimeText = item.DateTime?.ToString("dd/MM/yyyy HH:mm:ss") ?? "";
                }

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
        public async Task<CommonReturnViewModel> UpdateJobs(ShipmentUpdateViewModel model, int? company)
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

                doucument.Eta = model.ETADeliveryDate;
                doucument.Ata = model.ActualDeliveryDate; //TODO
                doucument.UnStuffingLocationDate = model.UnstuffingDate; 
                doucument.Etddate = model.ETDDate; 
                doucument.PlaceOfLoadingId = model.PlaceOfLoadingID; 
                doucument.ShedYardId = model.ShedYardID; 
                doucument.FreightCharge = model.FreightChargeID;
                doucument.ModifyDate = DateTime.Now;
                await _sepDocumentionRepository.UpdateAsync(doucument);

                if (model.AutoId > 0)
                {
                    var prevData = await _jobUpdateRepository.All().FirstOrDefaultAsync(e => e.Tc == model.AutoId);

                    if (prevData != null)
                    {
                        prevData.UpdateStatus = model.ShipmentStatus;
                        prevData.UpdateDateTime = DateTime.Now;
                        prevData.ModifyDate = DateTime.Now;
                        prevData.Lip = model.Lip;
                        prevData.Lmac = model.Lmac;
                        prevData.EmployeeId = model.Luser;
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
                        UpdateDateTime = DateTime.Now,
                        Ldate=DateTime.Now,
                        Lip = model.Lip,
                        Lmac = model.Lmac,
                        EmployeeId = model.Luser,
                        Luser = model.Luser,
                        CompanyCode = company.ToString()
                    };

                    await _jobUpdateRepository.AddAsync(updateDoc);
                }

               

                await _sepDocumentionRepository.CommitTransactionAsync();

                return new CommonReturnViewModel()
                {
                    Success = true,
                    Message = "Update  Success",
                    Data = doucument.JobNo
                };

            }
            catch (Exception)
            {
                await _sepDocumentionRepository.RollbackTransactionAsync();
                return new CommonReturnViewModel()
                {
                    Success = false,
                    Message = "Something went wrong"
                };
            }

            
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
                ShipmentMode = h.ExpenseTypeId,          // h থেকে bind করুন
                ShipmentModeId = h.ExpenseTypeId,
                IsCustomJobNo = h.IsCustomJobNo,
                JobDate = h.Date.Value.ToShortDateString(),
                CustomerId = h.CustomerId,
                //CustomerName = h.CustomerId,
                CustomerDeliveryAddress = h.DeliveryLocationCode, //TODO: DElivery add
                PortId = h.PortId,
                DocsReceivedDate = h.DocReceivedDate.Value.ToShortDateString(),
                LcExpNo = h.ExpNo,
                LcValue = h.Lcvalue,
                IpEpNo = h.Ipno,
                IpDate = h.Ipdate.Value.ToString(),
                ImporterId = h.ImporterId,
                InvoiceNo = h.InvoiceNo,
                InvoiceDate = h.InvoiceDate.Value.ToString(),
                BlNo = h.Beno,
                BeNo = h.Beno,
                BeDate = h.Bedate.Value.ToString(),
                BlDate = h.Bldate.Value.ToString(),
                ContainerNo = h.ContainerNo,
                ContainerSize = h.ContainerSize,
                LcaNo = h.Lcano,
                DischargeDate = h.Dischargedate.Value.ToString(),
                MaterialDescription = h.MaterialDescription,
                Quantity = h.Quntity1,
                QuantityUnitId = h.Unit1,
                Weight = h.Quntity2,
                WeightUnitId = h.Unit2,
                ForwarderId = h.NameOfFreightForwarder,
                VesselRottNo = h.VesselRottNo,
                Remarks = h.Remarks,
                DocReceivedDate = h.DocReceivedDate.Value.ToString()
            })
            .FirstOrDefaultAsync(j => j.JobNo == jobNo);


            if (query1 == null)
                throw new Exception("Job not found");

            return query1;
        }

        public async Task<ShipmentUpdateViewModel> GetJobBottomDetailsAsync(string jobNo, decimal id)
        {
            var status = await _jobUpdateRepository.All().Where(e => e.Tc == id).Select(e => new { e.UpdateStatus , e.Tc, e.Ldate, e.ModifyDate}).FirstOrDefaultAsync();

            var data = await _sepDocumentionRepository.All().Where(e => e.JobNo == jobNo).Select(model => new ShipmentUpdateViewModel
            {
                ETADeliveryDate = model.Eta,
                ActualDeliveryDate = model.Ata, //TODO
                UnstuffingDate = model.UnStuffingLocationDate,
                ETDDate = model.Etddate,
                PlaceOfLoadingID = model.PlaceOfLoadingId,
                ShedYardID = model.ShedYardId,
                FreightChargeID = model.FreightCharge,
                ShipmentStatus = status != null ? status.UpdateStatus : "",
                AutoId = status != null ? status.Tc : 0m,
                Ldate = status.Ldate,
                ModifyDate = status.ModifyDate,
            }).FirstOrDefaultAsync();

            return data ?? new ShipmentUpdateViewModel();
          
        }
    }
}

