using GCTL.Core.Data;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.SepDocumentation;
using GCTL.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GCTL.Service.SepDocumentation
{
    public class SepDocumentationService : ISepDocumentationService
    {
        private readonly IGenericRepository<SalesCustomer> _customerRepository;
        private readonly IGenericRepository<SalesDeliveryLocation> _deliveryLocationRepository;
        private readonly IGenericRepository<GCTL.Data.Models.SepDocumentation> _sepDocumentationRepository;
        private readonly IGenericRepository<CfDefExpenseType> _shipmentModeRepository;

        public SepDocumentationService(
            IGenericRepository<SalesCustomer> customerRepository,
            IGenericRepository<SalesDeliveryLocation> deliveryLocationRepository,
            IGenericRepository<GCTL.Data.Models.SepDocumentation> sepDocumentationRepository,
            IGenericRepository<CfDefExpenseType> shipmentModeRepository)
        {
            _customerRepository = customerRepository;
            _deliveryLocationRepository = deliveryLocationRepository;
            _sepDocumentationRepository = sepDocumentationRepository;
            _shipmentModeRepository = shipmentModeRepository;
        }

        #region Grid

        public async Task<SepDocPagedResult<SepDocJobListDto>> GetJobsPagedAsync(
            int page, int pageSize, string customerId, string shipmentMode,
            DateTime? dateFrom, DateTime? dateTo, string search)
        {
            try
            {
                IQueryable<SepDocJobListDto> query =
                    from d in _sepDocumentationRepository.All().AsNoTracking()

                    join c in _customerRepository.All().AsNoTracking()
                        on d.CustomerId equals c.CustomerId into customers
                    from c in customers.DefaultIfEmpty()

                    join s in _shipmentModeRepository.All().AsNoTracking()
                        on d.ExpenseTypeId equals s.ExpenseTypeId into modes
                    from s in modes.DefaultIfEmpty()

                    select new SepDocJobListDto
                    {
                        Tc = d.Tc,
                        JobNo = d.JobNo,
                        JobDate = d.Date,
                        CustomerName = c != null ? c.CustomerName : "",
                        CustomerAddress = d.CustomerAddress,
                        ShipmentMode = s != null ? s.ExpenseType : "",
                        DocReceivedDate = d.DocReceivedDate,
                        LCValue = d.Lcvalue,
                        JobStatus = d.ShipmentStatus
                    };

                var totalCount = await query.CountAsync();

                if (!string.IsNullOrWhiteSpace(customerId))
                    query = query.Where(x => x.CustomerName == customerId || x.CustomerName.Contains(customerId));

                if (!string.IsNullOrWhiteSpace(shipmentMode))
                    query = query.Where(x => x.ShipmentMode == shipmentMode);

                if (dateFrom.HasValue)
                    query = query.Where(x => x.JobDate >= dateFrom.Value);

                if (dateTo.HasValue)
                {
                    var end = dateTo.Value.Date.AddDays(1);
                    query = query.Where(x => x.JobDate < end);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.Trim();
                    query = query.Where(x =>
                        x.JobNo.Contains(search) ||
                        x.CustomerName.Contains(search) ||
                        x.ShipmentMode.Contains(search));
                }

                var filteredCount = await query.CountAsync();

                var data = await query
                    .OrderByDescending(x => x.Tc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                foreach (var row in data)
                {
                    row.JobDateFormatted = row.JobDate.HasValue ? row.JobDate.Value.ToString("dd/MM/yyyy") : "";
                    row.DocReceivedDateFormatted = row.DocReceivedDate.HasValue ? row.DocReceivedDate.Value.ToString("dd/MM/yyyy") : "";
                }

                return new SepDocPagedResult<SepDocJobListDto>
                {
                    Data = data,
                    TotalRecords = totalCount,
                    FilteredRecords = filteredCount
                };
            }
            catch
            {
                return new SepDocPagedResult<SepDocJobListDto>();
            }
        }

        #endregion

        #region Detail

        public async Task<SepDocumentationEntryViewModel> GetJobDetailAsync(decimal tc)
        {
            var h = await _sepDocumentationRepository.All().AsNoTracking().FirstOrDefaultAsync(x => x.Tc == tc);
            if (h == null) return new SepDocumentationEntryViewModel();
            return await MapToViewModelAsync(h);
        }

        public async Task<SepDocumentationEntryViewModel> GetJobDetailByJobNoAsync(string jobNo)
        {
            var h = await _sepDocumentationRepository.All().AsNoTracking().FirstOrDefaultAsync(x => x.JobNo == jobNo);
            if (h == null) return new SepDocumentationEntryViewModel();
            return await MapToViewModelAsync(h);
        }

        private async Task<SepDocumentationEntryViewModel> MapToViewModelAsync(GCTL.Data.Models.SepDocumentation h)
        {
            var transportModeTypeId = await GetTransportModeTypeAsync(h.ExpenseTypeId);

            return new SepDocumentationEntryViewModel
            {
                Tc = h.Tc,
                JobNo = h.JobNo,
                PrifixId = h.PrifixId,
                IsCustomJobNo = h.IsCustomJobNo == "1" || h.IsCustomJobNo == "true",
                ShipmentModeId = h.ExpenseTypeId,
                TransportModeTypeId = transportModeTypeId,
                Date = h.Date,
                CustomerId = h.CustomerId,
                CustomerAddress = h.CustomerAddress,
                DeliveryLocationCode = h.DeliveryLocationCode,
                PortId = h.PortId,
                PortNameId = h.PortNameId,
                DocReceivedDate = h.DocReceivedDate,
                ExpNo = h.ExpNo,
                ExpDate = h.ExpDate,
                LCNo = h.Lcno,
                LCDate = h.Lcdate,
                LCValue = h.Lcvalue,
                CurrencyId = h.CurrencyId,
                BDTRate = h.Bdtrate,
                IPNo = h.Ipno,
                IPDate = h.Ipdate,
                ImporterID = h.ImporterId,
                ExporterID = h.ExporterId,
                InvoiceNo = h.InvoiceNo,
                InvoiceDate = h.InvoiceDate,
                InvoiceValue = h.InvoiceValue,
                InvoiceCurrencyID = h.InvoiceCurrencyId,
                AssessableRate = h.AssessableRate,
                AssessableValue = h.AssessableValue,
                BLNo = h.Beno,          // legacy: BLNo tracked via Beno/Bldate pairing in existing code
                BLDate = h.Bldate,
                ContainerNo = h.ContainerNo,
                ContainerSize = h.ContainerSize,
                MaterialDescription = h.MaterialDescription,
                Quntity1 = h.Quntity1,
                Unit1 = h.Unit1,
                Quntity2 = h.Quntity2,
                Unit2 = h.Unit2,
                FWDR_ShippingAgent = h.FwdrShippingAgent,
                NameOfFreightForwarder = h.NameOfFreightForwarder,
                VesselRottNo = h.VesselRottNo,
                PlaceOfLoadingID = h.PlaceOfLoadingId,
                ShedYardID = h.ShedYardId,
                HAWB = h.Hawb,
                MAWB = h.Mawb,
                AssessmentNo = h.AssessmentNo,
                RNo = h.Rno,
                ClientStationwiseSLNo = h.ClientStationwiseSlno,
                FreightCharge = h.FreightCharge,
                LCANo = h.Lcano,
                Remarks = h.Remarks,

                // shared
                ETDDate = h.Etddate,
                //DocsBankSignDate = h.DocsBankSignDate,
                BENo = h.Beno,
                BEDate = h.Bedate,
                ActualDeliveryDate = h.Ata,

                // sea
                //FinalIgmRecDate = h.FinalIgmRecDate,
                ShipArrivedDate = h.Shiparriveddate,
                UnstuffDischargeDate = h.UnStuffingLocationDate,
                EstimatedDeliveryDate = h.Eta,

                // air
                FlightArrivalDate = h.Atb,
                //ArrivalNoticeDate = h.ArrivalNoticeDate,

                CreatedAt = h.Ldate,
                UpdatedAt = h.ModifyDate
            };
        }

        #endregion

        #region Save / Delete

        public async Task<CommonReturnViewModel> SaveDocumentationAsync(SepDocumentationEntryViewModel model, string company, string loginUser)
        {
            await _sepDocumentationRepository.BeginTransactionAsync();
            try
            {
                bool isNew = model.Tc == 0;
                GCTL.Data.Models.SepDocumentation entity;

                if (isNew)
                {
                    var dupExists = await _sepDocumentationRepository.All()
                        .AnyAsync(x => (x.InvoiceNo == model.InvoiceNo && !string.IsNullOrEmpty(model.InvoiceNo))
                                    || (x.ExpNo == model.ExpNo && !string.IsNullOrEmpty(model.ExpNo)));
                    if (dupExists)
                    {
                        await _sepDocumentationRepository.RollbackTransactionAsync();
                        return new CommonReturnViewModel { Success = false, Message = "Exp. No or Invoice No already exists" };
                    }

                    entity = new GCTL.Data.Models.SepDocumentation
                    {
                        JobNo = model.JobNo,
                        Ldate = DateTime.Now,
                        Luser = loginUser,
                        CompanyCode = company
                    };
                }
                else
                {
                    entity = await _sepDocumentationRepository.All().FirstOrDefaultAsync(x => x.Tc == model.Tc);
                    if (entity == null)
                    {
                        await _sepDocumentationRepository.RollbackTransactionAsync();
                        return new CommonReturnViewModel { Success = false, Message = "Job not found" };
                    }

                    var duplicate = await _sepDocumentationRepository.All()
                        .AnyAsync(x => x.Tc != model.Tc &&
                                      ((x.InvoiceNo == model.InvoiceNo && !string.IsNullOrEmpty(model.InvoiceNo))
                                    || (x.ExpNo == model.ExpNo && !string.IsNullOrEmpty(model.ExpNo))));
                    if (duplicate)
                    {
                        await _sepDocumentationRepository.RollbackTransactionAsync();
                        return new CommonReturnViewModel { Success = false, Message = "Exp. No or Invoice No already exists" };
                    }
                }

                entity.PrifixId = model.PrifixId;
                entity.IsCustomJobNo = model.IsCustomJobNo ? "1" : "0";
                entity.ExpenseTypeId = model.ShipmentModeId;
                entity.Date = model.Date;
                entity.CustomerId = model.CustomerId;
                entity.CustomerAddress = model.CustomerAddress;
                entity.DeliveryLocationCode = model.DeliveryLocationCode;
                entity.PortId = model.PortId;
                entity.PortNameId = model.PortNameId;
                entity.DocReceivedDate = model.DocReceivedDate;
                entity.ExpNo = model.ExpNo;
                entity.ExpDate = model.ExpDate;
                entity.Lcno = model.LCNo;
                entity.Lcdate = model.LCDate;
                entity.Lcvalue = model.LCValue;
                entity.CurrencyId = model.CurrencyId;
                entity.Bdtrate = model.BDTRate;
                entity.Ipno = model.IPNo;
                entity.Ipdate = model.IPDate;
                entity.ImporterId = model.ImporterID;
                entity.ExporterId = model.ExporterID;
                entity.InvoiceNo = model.InvoiceNo;
                entity.InvoiceDate = model.InvoiceDate;
                entity.InvoiceValue = model.InvoiceValue;
                entity.InvoiceCurrencyId = model.InvoiceCurrencyID;
                entity.AssessableRate = model.AssessableRate;
                entity.AssessableValue = model.AssessableValue;
                entity.Bldate = model.BLDate;
                entity.ContainerNo = model.ContainerNo;
                entity.ContainerSize = model.ContainerSize;
                entity.MaterialDescription = model.MaterialDescription;
                entity.Quntity1 = model.Quntity1;
                entity.Unit1 = model.Unit1;
                entity.Quntity2 = model.Quntity2;
                entity.Unit2 = model.Unit2;
                entity.FwdrShippingAgent = model.FWDR_ShippingAgent;
                entity.NameOfFreightForwarder = model.NameOfFreightForwarder;
                entity.VesselRottNo = model.VesselRottNo;
                entity.PlaceOfLoadingId = model.PlaceOfLoadingID;
                entity.ShedYardId = model.ShedYardID;
                entity.Hawb = model.HAWB;
                entity.Mawb = model.MAWB;
                entity.AssessmentNo = model.AssessmentNo;
                entity.Rno = model.RNo;
                entity.ClientStationwiseSlno = model.ClientStationwiseSLNo;
                entity.FreightCharge = model.FreightCharge;
                entity.Lcano = model.LCANo;
                entity.Remarks = model.Remarks;

                // shared Sea+Air fields
                entity.Etddate = model.ETDDate;
                //entity.DocsBankSignDate = model.DocsBankSignDate;
                entity.Beno = model.BENo;
                entity.Bedate = model.BEDate;
                entity.Ata = model.ActualDeliveryDate;

                // Sea-only fields (only persist if Sea, otherwise leave / clear)
                var transportType = (model.TransportModeTypeId ?? await GetTransportModeTypeAsync(model.ShipmentModeId) ?? "").ToUpperInvariant();

                if (transportType.Contains("SEA"))
                {
                    //entity.FinalIgmRecDate = model.FinalIgmRecDate;
                    entity.Shiparriveddate = model.ShipArrivedDate;
                    entity.UnStuffingLocationDate = model.UnstuffDischargeDate;
                    entity.Eta = model.EstimatedDeliveryDate;

                    // clear air-only
                    entity.Atb = null;
                    //entity.ArrivalNoticeDate = null;
                }
                else if (transportType.Contains("AIR"))
                {
                    entity.Atb = model.FlightArrivalDate;
                    //entity.ArrivalNoticeDate = model.ArrivalNoticeDate;

                    // clear sea-only
                    //entity.FinalIgmRecDate = null;
                    entity.Shiparriveddate = null;
                    entity.UnStuffingLocationDate = null;
                    entity.Eta = null;
                }

                entity.Luser = loginUser ?? entity.Luser;

                if (isNew)
                {
                    await _sepDocumentationRepository.AddAsync(entity);
                }
                else
                {
                    entity.ModifyDate = DateTime.Now;
                    await _sepDocumentationRepository.UpdateAsync(entity);
                }

                await _sepDocumentationRepository.CommitTransactionAsync();

                return new CommonReturnViewModel
                {
                    Success = true,
                    Message = isNew ? "Data added successfully" : "Record updated successfully",
                    Data = entity.JobNo
                };
            }
            catch (Exception ex)
            {
                await _sepDocumentationRepository.RollbackTransactionAsync();
                return new CommonReturnViewModel { Success = false, Message = "Failed to save data: " + ex.Message };
            }
        }

        public async Task<CommonReturnViewModel> DeleteDocumentationAsync(List<decimal> ids)
        {
            if (ids == null || ids.Count == 0)
                return new CommonReturnViewModel { Success = false, Message = "No records selected" };

            await _sepDocumentationRepository.BeginTransactionAsync();
            try
            {
                var records = await _sepDocumentationRepository.All()
                    .Where(x => ids.Contains(x.Tc))
                    .ToListAsync();

                if (records.Count == 0)
                {
                    await _sepDocumentationRepository.RollbackTransactionAsync();
                    return new CommonReturnViewModel { Success = false, Message = "No records found to delete" };
                }

                foreach (var r in records)
                    await _sepDocumentationRepository.DeleteAsync(r);

                await _sepDocumentationRepository.CommitTransactionAsync();

                return new CommonReturnViewModel
                {
                    Success = true,
                    Message = $"{records.Count} record(s) deleted successfully"
                };
            }
            catch (Exception ex)
            {
                await _sepDocumentationRepository.RollbackTransactionAsync();
                return new CommonReturnViewModel { Success = false, Message = "Error occurred while deleting: " + ex.Message };
            }
        }

        #endregion

        #region Transport mode

        public async Task<string> GetTransportModeTypeAsync(string shipmentModeId)
        {
            if (string.IsNullOrWhiteSpace(shipmentModeId)) return "";

            var mode = await _shipmentModeRepository.All()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ExpenseTypeId == shipmentModeId);

            return mode?.TransportModeTypeId ?? "";
        }

        #endregion

        #region Dropdowns

        public List<SelectListItem> GetCustomers()
        {
            return _customerRepository.All()
                .Select(c => new SelectListItem { Text = c.CustomerName, Value = c.CustomerId })
                .ToList();
        }

        public List<SelectListItem> GetAddresses()
        {
            return _deliveryLocationRepository.All()
                .Select(a => new SelectListItem { Text = a.LocationAddress, Value = a.DeliveryLocationCode })
                .ToList();
        }

        #endregion
    }

    public class SelectListItem
    {
        public string Text { get; set; }
        public string Value { get; set; }
    }
}