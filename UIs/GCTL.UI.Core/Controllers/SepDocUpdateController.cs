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
        private readonly IGenericRepository<SepDocumentInfo> _documentInfoRepository;
        private readonly IGenericRepository<SalesCustomer> _customerRepository;
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
            IGenericRepository<SepDocumentInfo> documentInfoRepository,
            IGenericRepository<SalesCustomer> customerRepository,
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
            _documentInfoRepository = documentInfoRepository;
            _customerRepository = customerRepository;
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
            try
            {
                if (string.IsNullOrWhiteSpace(jobNo))
                    return Json(new { success = false, message = "Job No is required." });

                var job = await _documentUploadRepository.All()
                    .Where(x => x.JobNo == jobNo)
                    .OrderByDescending(x => x.Tc)
                    .FirstOrDefaultAsync();

                if (job == null)
                    return Json(new { success = false, message = "Job not found." });

                string customerName = "";
                if (!string.IsNullOrEmpty(job.CustomerId))
                {
                    var customer = await _customerRepository.All()
                        .Where(c => c.CustomerId == job.CustomerId)
                        .Select(c => c.CustomerName)
                        .FirstOrDefaultAsync();
                    customerName = customer ?? "";
                }

                return Json(new
                {
                    success = true,
                    jobNo = job.JobNo,
                    date = job.Date.HasValue ? job.Date.Value.ToString("dd/MM/yyyy") : "",
                    customerName = customerName,
                    customerAddress = job.CustomerAddress ?? ""
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [ModulePermission]
        public async Task<IActionResult> GetJobDocuments(string jobNo)
        {
            if (string.IsNullOrWhiteSpace(jobNo))
                return Json(new List<object>());

            var result = await _documentInfoRepository.All()
                .Where(d => d.JobNo == jobNo)
                .OrderBy(d => d.DocCode)
                .Select(d => new
                {
                    autoId = d.AutoId,
                    docCode = d.DocCode,
                    jobNo = d.JobNo,
                    documentName = d.DocumentName ?? "",
                    documentDiscription = d.DocumentDiscription ?? "",
                    documentType = d.DocumentType ?? "",
                    filePath = !string.IsNullOrEmpty(d.DocumentType) ? "/DocumentInfo/" + d.DocumentType : "",
                    lDate = d.Ldate.HasValue ? d.Ldate.Value.ToString("dd/MM/yyyy hh:mm tt") : "",
                    modifyDate = d.ModifyDate.HasValue ? d.ModifyDate.Value.ToString("dd/MM/yyyy hh:mm tt") : ""
                })
                .ToListAsync();

            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ModulePermission]
        public async Task<IActionResult> UploadDocument(
            [FromForm] string JobNo, 
            [FromForm] string DocumentName,
            [FromForm] string Description, 
            IFormFile uploadFile)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(JobNo))
                    return Json(ApiResponse.Error("Job No is required."));

                if (string.IsNullOrWhiteSpace(DocumentName))
                    return Json(ApiResponse.Error("Document Name is required."));

                if (uploadFile == null || uploadFile.Length == 0)
                    return Json(ApiResponse.Error("Please select a file to upload."));

                if (uploadFile.Length > 15 * 1024 * 1024)
                    return Json(ApiResponse.Error("File size exceeds the 15 MB limit."));

                // 1. Generate 8-digit DocCode (e.g. 00000001, 00000002, ...)
                var allDocCodes = await _documentInfoRepository.All()
                    .Select(d => d.DocCode)
                    .ToListAsync();

                int maxNum = 0;
                foreach (var code in allDocCodes)
                {
                    if (int.TryParse(code, out int n) && n > maxNum)
                        maxNum = n;
                }
                string docCode = (maxNum + 1).ToString("D8");

                // 2. Save file to wwwroot/DocumentInfo
                var ext = Path.GetExtension(uploadFile.FileName);
                var fileName = $"{docCode}{ext}";
                var webRoot = _webHostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var folderPath = Path.Combine(webRoot, "DocumentInfo");

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var fullPath = Path.Combine(folderPath, fileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await uploadFile.CopyToAsync(stream);
                }

                // 3. Insert into Sep_DocumentInfo
                var entity = new SepDocumentInfo
                {
                    DocCode = docCode,
                    JobNo = JobNo,
                    DocumentName = DocumentName,
                    DocumentDiscription = Description ?? "",
                    DocumentType = fileName,
                    Doucment = null,
                    CompanyCode = LoginInfo?.CompanyCode ?? "001",
                    Luser = LoginInfo?.EmployeeId ?? LoginInfo?.Username ?? "Admin",
                    Ldate = DateTime.Now,
                    Lip = HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "",
                    Lmac = "",
                    UserInfoEmployeeId = LoginInfo?.EmployeeId ?? ""
                };

                await _documentInfoRepository.AddAsync(entity);

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
        public async Task<IActionResult> DeleteDocuments([FromBody] SepDocDeleteDocRequest request)
        {
            try
            {
                if (request?.DocCodes == null || request.DocCodes.Count == 0)
                    return Json(ApiResponse.Error("No documents selected for deletion."));

                var webRoot = _webHostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var folderPath = Path.Combine(webRoot, "DocumentInfo");

                var entities = await _documentInfoRepository.All()
                    .Where(d => request.DocCodes.Contains(d.DocCode))
                    .ToListAsync();

                foreach (var entity in entities)
                {
                    if (!string.IsNullOrEmpty(entity.DocumentType))
                    {
                        var filePath = Path.Combine(folderPath, entity.DocumentType);
                        if (System.IO.File.Exists(filePath))
                        {
                            try { System.IO.File.Delete(filePath); } catch { }
                        }
                    }
                    await _documentInfoRepository.DeleteAsync(entity);
                }

                return Json(ApiResponse.Success("Document(s) deleted successfully."));
            }
            catch (Exception ex)
            {
                return Json(ApiResponse.Error("Error occurred: " + ex.Message));
            }
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

    public class SepDocDeleteDocRequest
    {
        public List<string> DocCodes { get; set; } = new();
    }
}