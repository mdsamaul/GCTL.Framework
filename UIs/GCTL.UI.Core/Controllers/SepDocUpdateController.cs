using GCTL.Core.Data;
using GCTL.Core.ViewModels.SepDocumentation;
using GCTL.Data.Models;
using GCTL.Service.SepDocumentation;
using GCTL.Service.Update;
using GCTL.UI.Core.Controllers;
using GCTL.UI.Core.Filters;
using GCTL.UI.Core.Helpers;
using GCTL.UI.Core.ViewModels.SepDocUpdate;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GCTL_App.Controllers
{
    public class SepDocUpdateController : BaseController
    {
        #region CTOR

        private readonly ISepDocumentationService _service;
        private readonly IGenericRepository<CfDefExpenseType> _shipmentModeRepository;
        private readonly IGenericRepository<SalesDefPortName> _portNameRepository;
        private readonly IGenericRepository<CaDefCurrency> _currencyRepository;
        private readonly IGenericRepository<RmgProdDefUnitType> _unitRepository;
        private readonly IGenericRepository<CfDefPlaceOfLoading> _placeofLoadingRepository;
        private readonly IGenericRepository<CfDefShedYard> _shedYardRepository;
        private readonly IGenericRepository<SalesDefImporterInfo> _importerInfoRepository;
        private readonly IGenericRepository<SepDocumentation> _documentUploadRepository;

        public SepDocUpdateController(
            ISepDocumentationService service,
            IGenericRepository<CfDefExpenseType> shipmentModeRepository,
            IGenericRepository<SalesDefPortName> portNameRepository,
            IGenericRepository<CaDefCurrency> currencyRepository,
            IGenericRepository<RmgProdDefUnitType> unitRepository,
            IGenericRepository<CfDefPlaceOfLoading> placeofLoadingRepository,
            IGenericRepository<CfDefShedYard> shedYardRepository,
            IGenericRepository<SalesDefImporterInfo> importerInfoRepository,
            IGenericRepository<SepDocumentation> documentUploadRepository) : base()
        {
            _service = service;
            _shipmentModeRepository = shipmentModeRepository;
            _portNameRepository = portNameRepository;
            _currencyRepository = currencyRepository;
            _unitRepository = unitRepository;
            _placeofLoadingRepository = placeofLoadingRepository;
            _shedYardRepository = shedYardRepository;
            _importerInfoRepository = importerInfoRepository;
            _documentUploadRepository = documentUploadRepository;
        }

        #endregion

        #region Index

        [ModulePermission]
        public IActionResult Index()
        {
            ShipmentUpdateSetupViewModel model = new ShipmentUpdateSetupViewModel
            {
                PageUrl = Url.Action(nameof(Index))
            };

            LoadDropdowns();

            return View(model);
        }

        private void LoadDropdowns()
        {
            ViewBag.ShipmentMode = new SelectList(_shipmentModeRepository.All().Select(e => new { id = e.ExpenseTypeId, name = e.ExpenseType }), "id", "name");
            ViewBag.CustomerName = new SelectList(_service.GetCustomers(), "Value", "Text");
            ViewBag.CustomerDeliveryAddress = new SelectList(_service.GetAddresses(), "Value", "Text");
            ViewBag.PortName = new SelectList(_portNameRepository.All().Select(e => new { id = e.PortNameId, name = e.PortName }), "id", "name");
            ViewBag.Currency = new SelectList(_currencyRepository.All().Select(e => new { id = e.CurrencyId, name = e.CurrencyName }), "id", "name");
            ViewBag.QuantityUnit = new SelectList(_unitRepository.All().Select(e => new { id = e.UnitTypId, name = e.UnitTypeName }), "id", "name");
            ViewBag.PlaceOfLoading = new SelectList(_placeofLoadingRepository.All().Select(e => new { id = e.PlaceOfLoadingId, name = e.PlaceOfLoadingName }), "id", "name");
            ViewBag.ShedYard = new SelectList(_shedYardRepository.All().Select(e => new { id = e.ShedYardId, name = e.ShedYardName }), "id", "name");
            ViewBag.ImporterName = new SelectList(_importerInfoRepository.All().Select(e => new { id = e.ImporterId, name = e.Name }), "id", "name");
            ViewBag.FreightCharge = new SelectList(new List<string> { "Prepaid", "Collect" }, null);
        }

        #endregion

        #region Grid / Detail

        [HttpGet]
        [ModulePermission]
        public async Task<IActionResult> GetJobs(int page = 1, int pageSize = 10, string? customerId = null,
            string? shipmentMode = null, DateTime? dateFrom = null, DateTime? dateTo = null, string? search = null)
        {
            var result = await _service.GetJobsPagedAsync(page, pageSize, customerId, shipmentMode, dateFrom, dateTo, search);

            return Json(new
            {
                data = result.Data,
                totalRecords = result.TotalRecords,
                filteredRecords = result.FilteredRecords
            });
        }

        [HttpGet]
        [ModulePermission]
        public async Task<IActionResult> GetJobDetail(decimal tc)
        {
            var job = await _service.GetJobDetailAsync(tc);
            return Json(job);
        }

        /// <summary>
        /// Returns SEA / AIR (or whatever code you use) for a given shipment mode,
        /// used by the client to toggle which delivery-tracking field group is shown.
        /// </summary>
        [HttpGet]
        [ModulePermission]
        public async Task<IActionResult> GetTransportModeType(string shipmentModeId)
        {
            try
            {
                var transportModeTypeId = await _service.GetTransportModeTypeAsync(shipmentModeId);
                return Json(new { success = true, transportModeTypeId });
            }
            catch
            {
                return Json(new { success = false, message = "Error retrieving transport mode" });
            }
        }

        #endregion

        #region Save / Delete

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(SepDocumentationEntryViewModel model)
        {
            bool isNewRecord = model.Tc == 0;
            try
            {
                var permissionResult = await CheckAddOrEditPermissionAsync(isNewRecord);
                if (permissionResult != null)
                    return permissionResult;

                if (!ModelState.IsValid)
                    return Json(ApiResponse.Error("Please correct the highlighted fields."));

                string company = "001";
                var result = await _service.SaveDocumentationAsync(model, company, LoginInfo?.EmployeeId);

                return Json(new { success = result.Success, message = result.Message, data = result.Data });
            }
            catch (Exception)
            {
                return Json(ApiResponse.Error(isNewRecord ? AppMessages.SaveFailed : AppMessages.UpdateFailed));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ModulePermission(PermissionType.Delete)]
        public async Task<IActionResult> DeleteJobs([FromBody] SepDocDeleteRequest request)
        {
            try
            {
                if (request?.Ids == null || request.Ids.Count == 0)
                    return Json(ApiResponse.Error("No records selected"));

                var result = await _service.DeleteDocumentationAsync(request.Ids);
                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception)
            {
                return Json(ApiResponse.Error(AppMessages.DeleteFailed));
            }
        }

        #endregion

        #region Document Upload  (Document Upload Info tab)

        [HttpGet]
        [ModulePermission]
        public async Task<IActionResult> GetJobDocuments(string jobNo)
        {
            var result = await _documentUploadRepository.All()
                .Where(d => d.JobNo == jobNo)
                .OrderBy(d => d.JobNo)
                .Select(d => new
                {
                    tc = d.Tc,
                    jobNo = d.JobNo,
                    documentName = d.InvoiceNo,
                    //description = d.dis,
                    //fileName = d.FileName,
                    //filePath = d.FilePath
                })
                .ToListAsync();

            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ModulePermission]
        public async Task<IActionResult> UploadDocument([FromForm] string JobNo, [FromForm] string DocumentName,
            [FromForm] string Description, IFormFile uploadFile)
        {
            try
            {
                if (string.IsNullOrEmpty(JobNo))
                    return Json(ApiResponse.Error("Job No is required."));

                if (string.IsNullOrEmpty(DocumentName))
                    return Json(ApiResponse.Error("Document Name is required."));

                if (uploadFile == null || uploadFile.Length == 0)
                    return Json(ApiResponse.Error("Please select a file to upload."));

                if (uploadFile.Length > 15 * 1024 * 1024)
                    return Json(ApiResponse.Error("File size exceeds the 15 MB limit."));

                var uploadsFolder = Path.Combine("wwwroot", "uploads", "SepDocuments", JobNo);
                Directory.CreateDirectory(uploadsFolder);

                var savedFileName = $"{Guid.NewGuid()}_{Path.GetFileName(uploadFile.FileName)}";
                var fullPath = Path.Combine(uploadsFolder, savedFileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await uploadFile.CopyToAsync(stream);
                }

                var maxSl = await _documentUploadRepository.All()
                    .Where(d => d.JobNo == JobNo)
                    .Select(d => (int?)d.Tc)
                    .MaxAsync() ?? 0;

                var entity = new SepDocumentation
                {
                    JobNo = JobNo,
                    //SlNo = maxSl + 1,
                    //DocumentName = DocumentName,
                    //Description = Description ?? "",
                    //FileName = uploadFile.FileName,
                    //FilePath = $"/uploads/SepDocuments/{JobNo}/{savedFileName}",
                    //FileSize = uploadFile.Length,
                    Luser = LoginInfo?.EmployeeId ?? "",
                    Ldate = DateTime.Now
                };

                await _documentUploadRepository.AddAsync(entity);

                return Json(ApiResponse.Success("Document uploaded successfully."));
            }
            catch (Exception ex)
            {
                return Json(ApiResponse.Error("Error occurred: " + ex.Message));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ModulePermission(PermissionType.Delete)]
        public async Task<IActionResult> DeleteDocument(decimal tc)
        {
            try
            {
                var entity = await _documentUploadRepository.GetByIdAsync(tc);
                if (entity == null)
                    return Json(ApiResponse.Error("Document not found."));
                var FilePath = "";
                if (!string.IsNullOrEmpty(FilePath))
                {
                    //var physicalPath = Path.Combine("wwwroot", entity.FilePath.TrimStart('/'));
                    var physicalPath = Path.Combine("wwwroot", FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(physicalPath))
                        System.IO.File.Delete(physicalPath);
                }

                await _documentUploadRepository.DeleteAsync(entity);

                return Json(ApiResponse.Success("Document deleted successfully."));
            }
            catch (Exception ex)
            {
                return Json(ApiResponse.Error("Error occurred: " + ex.Message));
            }
        }

        #endregion

        [HttpGet]
        public async Task<IActionResult> GenerateJobNo(string shipmentModeId, string customerId)
        {
            if (string.IsNullOrEmpty(shipmentModeId) && string.IsNullOrEmpty(customerId))
            {
                return BadRequest("ShipmentModeId and CustomerId are required.");
            }

            var jobNo = await _service.GenerateJobNoAsync(shipmentModeId, customerId);

            return Json(jobNo);
        }
        [HttpGet]
        public async Task<IActionResult> CustomerInfo(string customerId)
        {
            if (string.IsNullOrEmpty(customerId))
            {
                return BadRequest("CustomerId is required.");
            }

            var result = await _service.GetCustomerInfoAsync(customerId);

            if (!result.Success)
            {
                return NotFound(result.Message);
            }

            return Json(result);
        }
    }
}