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
            IGenericRepository<CfDefExpenseType> shipmentModeRepository
            )
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

                        CustomerId = d.CustomerId,
                        ShipmentModeId = d.ExpenseTypeId,

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
                {
                    query = query.Where(x => x.CustomerId == customerId);
                }

                if (!string.IsNullOrWhiteSpace(shipmentMode))
                {
                    query = query.Where(x => x.ShipmentModeId == shipmentMode);
                }

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
                ETACTGPORT = h.Etactgport,
                DocsBankSignDate = h.DocsBankSignDate,
                BENo = h.Beno,
                BEDate = h.Bedate,
                ActualDeliveryDate = h.Ata,

                // sea
                FinalIgmRecDate = h.FinalIgmRecDate,
                ShipArrivedDate = h.Shiparriveddate,
                UnstuffDischargeDate = h.UnStuffingLocationDate,
                EstimatedDeliveryDate = h.Eta,

                // air
                FlightArrivalDate = h.Atb,
                ArrivalNoticeDate = h.ArrivalNoticeDate,

                CreatedAt = h.Ldate,
                UpdatedAt = h.ModifyDate
            };
        }

        #endregion

        #region Save / Delete

        //public async Task<CommonReturnViewModel> SaveDocumentationAsync(SepDocumentationEntryViewModel model, string company, string loginUser)
        //{
        //    await _sepDocumentationRepository.BeginTransactionAsync();
        //    try
        //    {
        //        bool isNew = model.Tc == 0;
        //        GCTL.Data.Models.SepDocumentation entity;

        //        if (isNew)
        //        {
        //            var dupExists = await _sepDocumentationRepository.All()
        //                .AnyAsync(x => (x.InvoiceNo == model.InvoiceNo && !string.IsNullOrEmpty(model.InvoiceNo))
        //                            || (x.ExpNo == model.ExpNo && !string.IsNullOrEmpty(model.ExpNo)));
        //            if (dupExists)
        //            {
        //                await _sepDocumentationRepository.RollbackTransactionAsync();
        //                return new CommonReturnViewModel { Success = false, Message = "Exp. No or Invoice No already exists" };
        //            }

        //            entity = new GCTL.Data.Models.SepDocumentation
        //            {
        //                JobNo = model.JobNo,
        //                Ldate = DateTime.Now,
        //                Luser = loginUser,
        //                CompanyCode = company
        //            };
        //        }
        //        else
        //        {
        //            entity = await _sepDocumentationRepository.All().FirstOrDefaultAsync(x => x.Tc == model.Tc);
        //            if (entity == null)
        //            {
        //                await _sepDocumentationRepository.RollbackTransactionAsync();
        //                return new CommonReturnViewModel { Success = false, Message = "Job not found" };
        //            }

        //            var duplicate = await _sepDocumentationRepository.All()
        //                .AnyAsync(x => x.Tc != model.Tc &&
        //                              ((x.InvoiceNo == model.InvoiceNo && !string.IsNullOrEmpty(model.InvoiceNo))
        //                            || (x.ExpNo == model.ExpNo && !string.IsNullOrEmpty(model.ExpNo))));
        //            if (duplicate)
        //            {
        //                await _sepDocumentationRepository.RollbackTransactionAsync();
        //                return new CommonReturnViewModel { Success = false, Message = "Exp. No or Invoice No already exists" };
        //            }
        //        }

        //        entity.PrifixId = model.PrifixId??"";
        //        entity.IsCustomJobNo = model.IsCustomJobNo?.ToString()??"";
        //        entity.ExpenseTypeId = model.ShipmentModeId??"";
        //        entity.Date = model.Date??null;
        //        entity.CustomerId = model.CustomerId??"";
        //        entity.CustomerAddress = model.CustomerAddress??"";
        //        entity.DeliveryLocationCode = model.DeliveryLocationCode??"";
        //        entity.PortId = model.PortId??"";
        //        entity.PortNameId = model.PortNameId??"";
        //        entity.DocReceivedDate = model.DocReceivedDate??null;
        //        entity.ExpNo = model.ExpNo??"";
        //        entity.ExpDate = model.ExpDate??null;
        //        entity.Lcno = model.LCNo??"";
        //        entity.Lcdate = model.LCDate??null;
        //        entity.Lcvalue = model.LCValue??0m;
        //        entity.CurrencyId = model.CurrencyId??"";
        //        entity.Bdtrate = model.BDTRate??0m;
        //        entity.Ipno = model.IPNo??"";
        //        entity.Ipdate = model.IPDate??null;
        //        entity.ImporterId = model.ImporterID??"";
        //        entity.ExporterId = model.ExporterID??"";
        //        entity.InvoiceNo = model.InvoiceNo??"";
        //        entity.InvoiceDate = model.InvoiceDate??null;
        //        entity.InvoiceValue = model.InvoiceValue??0m;
        //        entity.InvoiceCurrencyId = model.InvoiceCurrencyID??"";
        //        entity.AssessableRate = model.AssessableRate??0m;
        //        entity.AssessableValue = model.AssessableValue??0m;
        //        entity.Bldate = model.BLDate??null;
        //        entity.ContainerNo = model.ContainerNo??"";
        //        entity.ContainerSize = model.ContainerSize??"";
        //        entity.MaterialDescription = model.MaterialDescription??"";
        //        entity.Quntity1 = model.Quntity1??0m;
        //        entity.Unit1 = model.Unit1??"";
        //        entity.Quntity2 = model.Quntity2??0m;
        //        entity.Unit2 = model.Unit2??"";
        //        entity.FwdrShippingAgent = model.FWDR_ShippingAgent??"";
        //        entity.NameOfFreightForwarder = model.NameOfFreightForwarder??"";
        //        entity.VesselRottNo = model.VesselRottNo??"";
        //        entity.PlaceOfLoadingId = model.PlaceOfLoadingID??"";
        //        entity.ShedYardId = model.ShedYardID??"";
        //        entity.Hawb = model.HAWB??"";
        //        entity.Mawb = model.MAWB??"";
        //        entity.AssessmentNo = model.AssessmentNo??"";
        //        entity.Rno = model.RNo??"";
        //        entity.ClientStationwiseSlno = model.ClientStationwiseSLNo??"";
        //        entity.FreightCharge = model.FreightCharge??"";
        //        entity.Lcano = model.LCANo??"";
        //        entity.Remarks = model.Remarks??"";

        //        // shared Sea+Air fields
        //        entity.Etddate = model.ETDDate??null;
        //        //entity.DocsBankSignDate = model.DocsBankSignDate;
        //        entity.Beno = model.BENo??"";
        //        entity.Bedate = model.BEDate??null;
        //        entity.Ata = model.ActualDeliveryDate?? null;

        //        // Sea-only fields (only persist if Sea, otherwise leave / clear)
        //        var transportType = (model.TransportModeTypeId ?? await GetTransportModeTypeAsync(model.ShipmentModeId) ?? "").ToUpperInvariant();

        //        if (transportType.Contains("SEA"))
        //        {
        //            //entity.FinalIgmRecDate = model.FinalIgmRecDate;
        //            entity.Shiparriveddate = model.ShipArrivedDate ?? null;
        //            entity.UnStuffingLocationDate = model.UnstuffDischargeDate ?? null;
        //            entity.Eta = model.EstimatedDeliveryDate ?? null;

        //            // clear air-only
        //            entity.Atb = null;
        //            //entity.ArrivalNoticeDate = null;
        //        }
        //        else if (transportType.Contains("AIR"))
        //        {
        //            entity.Atb = model.FlightArrivalDate ?? null;
        //            //entity.ArrivalNoticeDate = model.ArrivalNoticeDate;

        //            // clear sea-only
        //            //entity.FinalIgmRecDate = null;
        //            entity.Shiparriveddate = model.ShipArrivedDate ?? null;
        //            entity.UnStuffingLocationDate = model.UnstuffDischargeDate ?? null;
        //            entity.Eta = model.ETDDate ?? null;
        //        }

        //        entity.Luser = loginUser ?? entity.Luser??"";

        //        if (isNew)
        //        {
        //            await _sepDocumentationRepository.AddAsync(entity);
        //        }
        //        else
        //        {
        //            entity.ModifyDate = DateTime.Now;
        //            await _sepDocumentationRepository.UpdateAsync(entity);
        //        }

        //        await _sepDocumentationRepository.CommitTransactionAsync();

        //        return new CommonReturnViewModel
        //        {
        //            Success = true,
        //            Message = isNew ? "Data added successfully" : "Record updated successfully",
        //            Data = entity.JobNo
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        await _sepDocumentationRepository.RollbackTransactionAsync();
        //        return new CommonReturnViewModel { Success = false, Message = "Failed to save data: " + ex.Message };
        //    }
        //}

        public async Task<CommonReturnViewModel> SaveDocumentationAsync(SepDocumentationEntryViewModel model, string company, string loginUser)
        {
            await _sepDocumentationRepository.BeginTransactionAsync();
            try
            {
                if (model == null)
                {
                    await _sepDocumentationRepository.RollbackTransactionAsync();
                    return new CommonReturnViewModel { Success = false, Message = "Invalid documentation data." };
                }

                bool isNew = !model.Tc.HasValue || model.Tc.Value == 0;
                GCTL.Data.Models.SepDocumentation entity;
                DateTime now = DateTime.Now;

                if (isNew)
                {
                    var duplicateExists = await _sepDocumentationRepository
                        .All()
                        .AnyAsync(x =>
                            (!string.IsNullOrWhiteSpace(model.InvoiceNo) && x.InvoiceNo == model.InvoiceNo) ||
                            (!string.IsNullOrWhiteSpace(model.ExpNo) && x.ExpNo == model.ExpNo));

                    if (duplicateExists)
                    {
                        await _sepDocumentationRepository.RollbackTransactionAsync();
                        return new CommonReturnViewModel { Success = false, Message = "Exp. No or Invoice No already exists." };
                    }

                    entity = new GCTL.Data.Models.SepDocumentation
                    {
                        Ldate = now,
                        Luser = loginUser ?? string.Empty,
                        CompanyCode = company ?? string.Empty,

                    };
                }
                else
                {
                    entity = await _sepDocumentationRepository.All().AsNoTracking().FirstOrDefaultAsync(x => x.Tc == model.Tc.Value);

                    if (entity == null)
                    {
                        await _sepDocumentationRepository.RollbackTransactionAsync();
                        return new CommonReturnViewModel { Success = false, Message = "Documentation record not found." };
                    }

                    var duplicateExists = await _sepDocumentationRepository
                        .All()
                        .AnyAsync(x =>
                            x.Tc != model.Tc.Value &&
                            ((!string.IsNullOrWhiteSpace(model.InvoiceNo) && x.InvoiceNo == model.InvoiceNo) ||
                             (!string.IsNullOrWhiteSpace(model.ExpNo) && x.ExpNo == model.ExpNo)));

                    if (duplicateExists)
                    {
                        await _sepDocumentationRepository.RollbackTransactionAsync();
                        return new CommonReturnViewModel { Success = false, Message = "Exp. No or Invoice No already exists." };
                    }
                }

                entity.JobNo = model.JobNo ?? entity.JobNo ?? string.Empty;
                entity.PrifixId = model.PrifixId ?? entity.PrifixId ?? string.Empty;
                entity.Date = model.Date ?? entity.Date;
                entity.IsCustomJobNo = model.IsCustomJobNo.HasValue ? model.IsCustomJobNo.Value.ToString() : (entity.IsCustomJobNo ?? string.Empty);
                entity.ExpenseTypeId = model.ShipmentModeId ?? entity.ExpenseTypeId ?? string.Empty;
                entity.CustomerId = model.CustomerId ?? entity.CustomerId ?? string.Empty;
                entity.CustomerAddress = model.CustomerAddress ?? entity.CustomerAddress ?? string.Empty;
                entity.DeliveryLocationCode = model.DeliveryLocationCode ?? entity.DeliveryLocationCode ?? string.Empty;
                entity.PortId = model.PortId ?? entity.PortId ?? string.Empty;
                entity.PortNameId = model.PortNameId ?? entity.PortNameId ?? string.Empty;
                entity.DocReceivedDate = model.DocReceivedDate ?? entity.DocReceivedDate;

                entity.ExpNo = model.ExpNo ?? entity.ExpNo ?? string.Empty;
                entity.ExpDate = model.ExpDate ?? entity.ExpDate;

                entity.Lcno = model.LCNo ?? entity.Lcno ?? string.Empty;
                entity.Lcdate = model.LCDate ?? entity.Lcdate;
                entity.Lcvalue = model.LCValue ?? entity.Lcvalue ?? 0m;
                entity.CurrencyId = model.CurrencyId ?? entity.CurrencyId ?? string.Empty;
                entity.Bdtrate = model.BDTRate ?? entity.Bdtrate ?? 0m;

                entity.Ipno = model.IPNo ?? entity.Ipno ?? string.Empty;
                entity.Ipdate = model.IPDate ?? entity.Ipdate;

                entity.ImporterId = model.ImporterID ?? entity.ImporterId ?? string.Empty;
                entity.ExporterId = model.ExporterID ?? entity.ExporterId ?? string.Empty;

                entity.InvoiceNo = model.InvoiceNo ?? entity.InvoiceNo ?? string.Empty;
                entity.InvoiceDate = model.InvoiceDate ?? entity.InvoiceDate;
                entity.InvoiceValue = model.InvoiceValue ?? entity.InvoiceValue ?? 0m;
                entity.InvoiceCurrencyId = model.InvoiceCurrencyID ?? entity.InvoiceCurrencyId ?? string.Empty;

                entity.AssessableRate = model.AssessableRate ?? entity.AssessableRate ?? 0m;
                entity.AssessableValue = model.AssessableValue ?? entity.AssessableValue ?? 0m;

                entity.ContainerNo = model.ContainerNo ?? entity.ContainerNo ?? string.Empty;
                entity.ContainerSize = model.ContainerSize ?? entity.ContainerSize ?? string.Empty;
                entity.MaterialDescription = model.MaterialDescription ?? entity.MaterialDescription ?? string.Empty;
                entity.Quntity1 = model.Quntity1 ?? entity.Quntity1 ?? 0m;
                entity.Unit1 = model.Unit1 ?? entity.Unit1 ?? string.Empty;
                entity.Quntity2 = model.Quntity2 ?? entity.Quntity2 ?? 0m;
                entity.Unit2 = model.Unit2 ?? entity.Unit2 ?? string.Empty;

                entity.FwdrShippingAgent = model.FWDR_ShippingAgent ?? entity.FwdrShippingAgent ?? string.Empty;
                entity.NameOfFreightForwarder = model.NameOfFreightForwarder ?? entity.NameOfFreightForwarder ?? string.Empty;
                entity.VesselRottNo = model.VesselRottNo ?? entity.VesselRottNo ?? string.Empty;
                entity.PlaceOfLoadingId = model.PlaceOfLoadingID ?? entity.PlaceOfLoadingId ?? string.Empty;
                entity.ShedYardId = model.ShedYardID ?? entity.ShedYardId ?? string.Empty;

                entity.Hawb = model.HAWB ?? entity.Hawb ?? string.Empty;
                entity.Mawb = model.MAWB ?? entity.Mawb ?? string.Empty;

                entity.AssessmentNo = model.AssessmentNo ?? entity.AssessmentNo ?? string.Empty;
                entity.Rno = model.RNo ?? entity.Rno ?? string.Empty;
                entity.ClientStationwiseSlno = model.ClientStationwiseSLNo ?? entity.ClientStationwiseSlno ?? string.Empty;

                entity.FreightCharge = model.FreightCharge ?? entity.FreightCharge ?? string.Empty;
                entity.Lcano = model.LCANo ?? entity.Lcano ?? string.Empty;
                entity.Remarks = model.Remarks ?? entity.Remarks ?? string.Empty;

                entity.Etddate = model.ETDDate ?? entity.Etddate;
                entity.Etactgport = model.ETACTGPORT ?? entity.Etactgport;
                entity.Beno = model.BENo ?? entity.Beno ?? string.Empty;
                entity.Bedate = model.BEDate ?? entity.Bedate;
                entity.Ata = model.ActualDeliveryDate ?? entity.Ata;

                var transportType = (model.TransportModeTypeId ?? await GetTransportModeTypeAsync(model.ShipmentModeId) ?? string.Empty)
                    .Trim()
                    .ToUpperInvariant();

                if (transportType == "002" || transportType.Contains("SEA"))
                {
                    entity.Shiparriveddate = model.ShipArrivedDate ?? entity.Shiparriveddate;
                    entity.UnStuffingLocationDate = model.UnstuffDischargeDate ?? entity.UnStuffingLocationDate;
                    entity.Eta = model.EstimatedDeliveryDate ?? entity.Eta;
                }
                else if (transportType == "001" || transportType.Contains("AIR"))
                {
                    entity.Atb = model.FlightArrivalDate ?? entity.Atb;
                    entity.Eta = model.EstimatedDeliveryDate ?? model.ETDDate ?? entity.Eta;
                }
                else
                {
                    entity.Shiparriveddate = model.ShipArrivedDate ?? entity.Shiparriveddate;
                    entity.UnStuffingLocationDate = model.UnstuffDischargeDate ?? entity.UnStuffingLocationDate;
                    entity.Eta = model.EstimatedDeliveryDate ?? model.ETDDate ?? entity.Eta;
                    entity.Atb = model.FlightArrivalDate ?? entity.Atb;
                }

                entity.FinalIgmRecDate = model.FinalIgmRecDate ?? entity.FinalIgmRecDate;
                entity.DocsBankSignDate = model.DocsBankSignDate ?? entity.DocsBankSignDate;
                entity.ArrivalNoticeDate = model.ArrivalNoticeDate ?? entity.ArrivalNoticeDate;

                entity.UndertakingNo = entity.UndertakingNo ?? string.Empty;
                entity.BillNo = entity.BillNo ?? string.Empty;
                entity.Pono = entity.Pono ?? string.Empty;
                entity.Etb = entity.Etb??null;
                entity.DateOfClearence = entity.DateOfClearence;
                entity.ShipmentStatus = entity.ShipmentStatus ?? string.Empty;
                entity.Lip = entity.Lip ?? string.Empty;
                entity.Lmac = entity.Lmac ?? string.Empty;
                entity.EmployeeId = entity.EmployeeId ?? string.Empty;
                entity.SupplierId = entity.SupplierId ?? string.Empty;
                entity.TransportCost = entity.TransportCost ?? 0m;
                entity.VehicleId = entity.VehicleId ?? string.Empty;
                entity.Vehicle = entity.Vehicle ?? string.Empty;
                entity.Vendor = entity.Vendor ?? string.Empty;
                entity.DocumentationId = entity.DocumentationId ?? string.Empty;
                entity.ConsoleNo = entity.ConsoleNo ?? string.Empty;
                entity.IsEdit = entity.IsEdit ?? false;
                entity.MinimumAgencyCom = entity.MinimumAgencyCom ?? 0m;
                entity.CommOnInvoiceValue = entity.CommOnInvoiceValue ?? 0m;
                entity.DocumentTypeId = entity.DocumentTypeId ?? string.Empty;
                entity.Dischargedate = entity.Dischargedate;
                entity.CustomerIdImporter = entity.CustomerIdImporter ?? string.Empty;
                entity.DocumentHandoverDate = entity.DocumentHandoverDate;
                entity.CargoHandoverDate = entity.CargoHandoverDate;
                entity.DepotId = entity.DepotId ?? string.Empty;
                entity.BookingConfirmationReference = entity.BookingConfirmationReference ?? string.Empty;
                entity.PackingQuantity = entity.PackingQuantity ?? 0m;
                entity.PackingUnitType = entity.PackingUnitType ?? string.Empty;
                entity.Etactgport = entity.Etactgport;
                entity.Bldate = entity.Bldate;

                entity.Luser = loginUser ?? entity.Luser ?? string.Empty;
                entity.Ldate = entity.Ldate ?? now;
                entity.CompanyCode = company ?? entity.CompanyCode ?? string.Empty;
                entity.ModifyDate = isNew ? entity.ModifyDate : now;

                if (isNew)
                {
                    await _sepDocumentationRepository.AddAsync(entity);
                }
                else
                {
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
                .AsNoTracking().Where(s => s.ExpenseTypeId == shipmentModeId)
                .FirstOrDefaultAsync();

            return mode?.TransportModeTypeId?.Trim() ?? "";
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
        public async Task<string> GenerateJobNoWithoutIdAsync()
        {
            try
            {
                var currentYear = DateTime.Now.ToString("yy");
                var prefix = $"{currentYear}_";

                var lastJobNo = await _sepDocumentationRepository.All()
                    .AsNoTracking()
                    .OrderByDescending(x => x.JobNo)
                    .Select(x => x.JobNo)
                    .FirstOrDefaultAsync();

                int nextSequence = 1;

                if (!string.IsNullOrEmpty(lastJobNo))
                {
                    var lastSequencePart = lastJobNo.Substring(prefix.Length);

                    if (int.TryParse(lastSequencePart, out int lastSequence))
                    {
                        nextSequence = lastSequence + 1;
                    }
                }

                return $"{prefix}{nextSequence:D6}";
            }
            catch
            {
                throw;
            }
        }
        public async Task<string> GenerateJobNoAsync(string shipmentModeId, string customerId)
        {
            try
            {               
                var shipmentModeShortName = await _shipmentModeRepository.All()
                    .Where(x => x.ExpenseTypeId == shipmentModeId)
                    .Select(x => x.ShortName)
                    .FirstOrDefaultAsync();

                var customerShortName = await _customerRepository.All()
                    .Where(x => x.CustomerId == customerId)
                    .Select(x => x.ShortName)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(shipmentModeShortName) && string.IsNullOrEmpty(customerShortName))
                {
                    return "";
                }

                var currentYear = DateTime.Now.ToString("yy");

                var parts = new List<string>();
                if (!string.IsNullOrEmpty(shipmentModeShortName)) parts.Add(shipmentModeShortName);
                if (!string.IsNullOrEmpty(customerShortName)) parts.Add(customerShortName);
                parts.Add(currentYear);

                var prefix = string.Join("_", parts) + "_";

                var lastJobNo = await _sepDocumentationRepository.All().AsNoTracking()
                    .Where(x => x.JobNo.StartsWith(prefix))
                    .OrderByDescending(x => x.JobNo)
                    .Select(x => x.JobNo)
                    .FirstOrDefaultAsync();

                int nextSequence = 1;

                if (!string.IsNullOrEmpty(lastJobNo))
                {
                    var lastSequencePart = lastJobNo.Substring(lastJobNo.LastIndexOf('_') + 1);
                    if (int.TryParse(lastSequencePart, out int lastSequence))
                    {
                        nextSequence = lastSequence + 1;
                    }
                }

                var jobNo = $"{prefix}{nextSequence.ToString("D6")}";

                return jobNo;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<CommonReturnViewModel> GetCustomerInfoAsync(string customerId)
        {
            try
            {
                var data = await _customerRepository.All()
                    .Where(x => x.CustomerId == customerId)
                    .Select(x => new CustomerInfoDto
                    {
                        CustomerId = x.CustomerId,
                        CustomerName = x.CustomerName ?? "",
                        ShortName = x.ShortName ?? "",
                        CustomerAddress = x.CustomerAddress ?? "",
                        ContactPerson = x.ContactPerson ?? "",
                        Phone = x.Phone ?? "",
                        Email = x.Email ?? ""
                    })
                    .FirstOrDefaultAsync();
                var delivaryAddress = await _deliveryLocationRepository.All().AsNoTracking()
                    .Where(x => x.CustomerId == customerId)
                    .Select(x => new DelivaryAddressDto
                    {
                        DelivaryId = x.DeliveryLocationCode,
                        DelivaryAddress = x.LocationAddress
                    })
                    .ToListAsync();
                if (data == null)
                {
                    return new CommonReturnViewModel
                    {
                        Success = false,
                        Message = $"No customer found with CustomerId: {customerId}"
                    };
                }

                data.DelivaryAddress = delivaryAddress;
                return new CommonReturnViewModel
                {
                    Success = true,
                    Message = "Customer info fetched successfully",
                    Data = data
                };
            }
            catch (Exception ex)
            {
                return new CommonReturnViewModel
                {
                    Success = false,
                    Message = "Error fetching customer info: " + ex.Message
                };
            }
        }
    }

    public class SelectListItem
    {
        public string Text { get; set; }
        public string Value { get; set; }
    }
}