using GCTL.Core.Data;
using GCTL.Core.Helpers;
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
        private readonly IWebHostEnvironment _webHostEnvironment;

        public SepDocUpdateController(
            ISepDocumentationService service,
            IGenericRepository<CfDefExpenseType> shipmentModeRepository,
            IGenericRepository<SalesDefPortName> portNameRepository,
            IGenericRepository<CaDefCurrency> currencyRepository,
            IGenericRepository<RmgProdDefUnitType> unitRepository,
            IGenericRepository<CfDefPlaceOfLoading> placeofLoadingRepository,
            IGenericRepository<CfDefShedYard> shedYardRepository,
            IGenericRepository<SalesDefImporterInfo> importerInfoRepository,
            IGenericRepository<SepDocumentation> documentUploadRepository,
            IWebHostEnvironment webHostEnvironment) : base()
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
            _webHostEnvironment = webHostEnvironment;
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
            ViewBag.Currency = new SelectList(_currencyRepository.All().Select(e => new { id = e.CurrencyId, name = e.ShortName }), "id", "name");
            ViewBag.QuantityUnit = new SelectList(_unitRepository.All().Select(e => new { id = e.UnitTypId, name = e.UnitTypeName }), "id", "name");
            ViewBag.PlaceOfLoading = new SelectList(_placeofLoadingRepository.All().Select(e => new { id = e.PlaceOfLoadingId, name = e.PlaceOfLoadingName }), "id", "name");
            ViewBag.ShedYard = new SelectList(_shedYardRepository.All().Select(e => new { id = e.ShedYardId, name = e.ShedYardName }), "id", "name");
            ViewBag.ImporterName = new SelectList(_importerInfoRepository.All().Select(e => new { id = e.ImporterId, name = e.Name }), "id", "name");
            ViewBag.FreightCharge = new SelectList(new List<string> { "Prepaid", "Collect" }, null);
            ViewBag.JobNos = new SelectList(
                _documentUploadRepository.All()
                    .Where(e => !string.IsNullOrEmpty(e.JobNo))
                    .OrderByDescending(e => e.Tc)
                    .Select(e => new { id = e.JobNo, name = e.JobNo })
                    .Distinct(), "id", "name");
        }

        #endregion

        #region Grid / Detail

        [HttpPost]
        [ModulePermission]
        public async Task<IActionResult> GetJobs(int pageNumber = 1, int pageSize = 10, string? customerId = null,
            string? shipmentMode = null, DateTime? dateFrom = null, DateTime? dateTo = null, string? searchTerm = null, string sortColumn = "JobNo", string sortOrder = "desc")
        {
            var result = await _service.GetJobsPagedAsync(pageNumber, pageSize, customerId, shipmentMode, dateFrom, dateTo, searchTerm);

            return Json(new
            {
                data = result.Data,
                recordsTotal = result.TotalRecords,
                recordsFiltered = result.FilteredRecords
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
        //public async Task<IActionResult> Save(SepDocumentationEntryViewModel model)
        //{
        //    bool isNewRecord = model.Tc == 0;
        //    try
        //    {
        //        var permissionResult = await CheckAddOrEditPermissionAsync(isNewRecord);
        //        if (permissionResult != null)
        //            return permissionResult;

        //        if (!ModelState.IsValid)
        //        {
        //            var errors = ModelState
        //                .Where(x => x.Value.Errors.Count > 0)
        //                .Select(x => new { Field = x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage) })
        //                .ToList();

        //            return Json(new { success = false, message = "Please correct the highlighted fields.", errors });
        //        }


        //        model.ToAudit(LoginInfo);
        //        var result = await _service.SaveDocumentationAsync(model, LoginInfo.CompanyCode, LoginInfo?.EmployeeId);

        //        return Json(new { success = result.Success, message = result.Message, data = result.Data });
        //    }
        //    catch (Exception)
        //    {
        //        return Json(ApiResponse.Error(isNewRecord ? AppMessages.SaveFailed : AppMessages.UpdateFailed));
        //    }
        //}
        public async Task<IActionResult> Save(SepDocumentationEntryViewModel model)
        {
            bool isNewRecord = model.Tc == 0;
            try
            {
                var permissionResult = await CheckAddOrEditPermissionAsync(isNewRecord);
                if (permissionResult != null)
                    return permissionResult;

                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0).Select(x => new
                        {
                            Field = x.Key,
                            Errors = x.Value.Errors
                                .Select(e => e.ErrorMessage)
                        }).ToList();

                    return Json(new
                    {
                        success = false,
                        message = "Please correct the highlighted fields.",
                        errors
                    });
                }

                model.ToAudit(LoginInfo);

                var result = await _service.SaveDocumentationAsync(
                    model,
                    LoginInfo.CompanyCode,
                    LoginInfo?.EmployeeId);

                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    data = result.Data
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = isNewRecord
                        ? AppMessages.SaveFailed
                        : AppMessages.UpdateFailed
                });
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
        public async Task<IActionResult> GetJobDetailsForUpload(string jobNo)
        {
            var result = await _service.GetJobDetailsForUploadAsync(jobNo);
            return Json(result);
        }

        [HttpGet]
        [ModulePermission]
        public async Task<IActionResult> GetJobDocuments(string jobNo)
        {
            var result = await _service.GetJobDocumentsAsync(jobNo);
            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ModulePermission]
        public async Task<IActionResult> UploadDocument(
            [FromForm] string JobNo, 
            [FromForm] string DocumentName,
            [FromForm] string Description, 
            [FromForm] string? DocCode,
            IFormFile? uploadFile)
        {
            var contentRoot = _webHostEnvironment.ContentRootPath ?? Directory.GetCurrentDirectory();

            var request = new SepDocumentUploadRequest
            {
                JobNo = JobNo,
                DocumentName = DocumentName,
                Description = Description,
                DocCode = DocCode,
                UploadFile = uploadFile,
                CompanyCode = LoginInfo?.CompanyCode ?? "001",
                UserId = LoginInfo?.EmployeeId ?? LoginInfo?.Username ?? "Admin",
                IpAddress = HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "",
                WebRootPath = contentRoot
            };

            var result = await _service.UploadOrUpdateDocumentAsync(request);
            if (result.Success)
                return Json(ApiResponse.Success(result.Message));
            else
                return Json(ApiResponse.Error(result.Message));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ModulePermission(PermissionType.Delete)]
        public async Task<IActionResult> DeleteDocuments([FromBody] SepDocDeleteDocRequest request)
        {
            if (request == null)
                return Json(ApiResponse.Error("Invalid request."));

            request.WebRootPath = _webHostEnvironment.ContentRootPath ?? Directory.GetCurrentDirectory();
            var result = await _service.DeleteDocumentsAsync(request);

            if (result.Success)
                return Json(ApiResponse.Success(result.Message));
            else
                return Json(ApiResponse.Error(result.Message));
        }

        #endregion

        [HttpGet]
        public async Task<IActionResult> GenerateJobNo(string? shipmentModeId, string? customerId)
        {
            string jobNo;

            if (string.IsNullOrEmpty(shipmentModeId) && string.IsNullOrEmpty(customerId))
            {
                jobNo = await _service.GenerateJobNoWithoutIdAsync();
            }
            else
            {
                jobNo = await _service.GenerateJobNoAsync(shipmentModeId, customerId);
            }

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