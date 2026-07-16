
using GCTL.Core.Data;
using GCTL.Core.ViewModels.ServiceType;
using GCTL.Core.ViewModels.Update;
using GCTL.Data.Models;
using GCTL.Service.Common;
using GCTL.Service.SepDocumentation;
using GCTL.Service.Update;
using GCTL.UI.Core.Controllers;
using GCTL.UI.Core.Filters;
using GCTL.UI.Core.Helpers;
using GCTL.UI.Core.ViewModels.BankInformations;
using GCTL.UI.Core.ViewModels.SepDocUpdate;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL_App.Controllers
{
    public class SepDocUpdateController : BaseController
    {

        #region CTOR

        private readonly IJobService _jobService;

        private readonly IGenericRepository<CfDefExpenseType> _shipmentModeRepository;
        private readonly IGenericRepository<SalesDefPortName> _portNameRepository;
        private readonly IGenericRepository<CaDefCurrency> _currencyRepository;
        private readonly IGenericRepository<RmgProdDefUnitType> _unitRepository;
        private readonly IGenericRepository<CfDefPlaceOfLoading> _placeofLoadingRepository;
        private readonly IGenericRepository<CfDefShedYard> _shedYardRepository;
        private readonly IGenericRepository<CfDefExpenseType> _shipmentStatusRepository;
        private readonly IGenericRepository<SalesDefImporterInfo> _importerInfoRepository;
        private readonly IGenericRepository<SepDocumentation> _sepDocumentionRepository;
        private readonly IGenericRepository<SalesDefShipmentStatusInfo> shipmentStatusRepo;
        private readonly IGenericRepository<SalesCustomer> _customerRepository;
        private readonly IGenericRepository<Country> _countryRepository;
        private readonly IGenericRepository<SalesContactPerson> _contactPersionRepository;
        private readonly ISepDocumentationService _service;

        public SepDocUpdateController(IJobService jobService, IGenericRepository<CfDefExpenseType> shipmentModeRepository, IGenericRepository<SalesDefPortName> portNameRepository, IGenericRepository<CaDefCurrency> currencyRepository, IGenericRepository<RmgProdDefUnitType> unitRepository, IGenericRepository<CfDefPlaceOfLoading> placeofLoadingRepository, IGenericRepository<CfDefShedYard> shedYardRepository, IGenericRepository<CfDefExpenseType> shipmentStatusRepository, IGenericRepository<SalesDefImporterInfo> importerInfoRepository, IGenericRepository<SepDocumentation> sepDocumentionRepository,  IGenericRepository<SalesDefShipmentStatusInfo> shipmentStatusRepo, IGenericRepository<SalesCustomer> customerRepository, IGenericRepository<Country> countryRepository, IGenericRepository<SalesContactPerson> contactPersionRepository, ISepDocumentationService service) : base()
        {
            _jobService = jobService;
            _shipmentModeRepository = shipmentModeRepository;
            _portNameRepository = portNameRepository;
            _currencyRepository = currencyRepository;
            _unitRepository = unitRepository;
            _placeofLoadingRepository = placeofLoadingRepository;
            _shedYardRepository = shedYardRepository;
            _shipmentStatusRepository = shipmentStatusRepository;
            _importerInfoRepository = importerInfoRepository;
            _sepDocumentionRepository = sepDocumentionRepository;
            this.shipmentStatusRepo = shipmentStatusRepo;
            _customerRepository = customerRepository;
            _countryRepository = countryRepository;
            _contactPersionRepository = contactPersionRepository;
            _service = service;
        }
        #endregion

        #region Index
        [ModulePermission]
        public IActionResult Index()
        {

            ShipmentUpdateSetupViewModel model = new ShipmentUpdateSetupViewModel()
            {
                PageUrl = Url.Action(nameof(Index))
            };
        



            ViewBag.ShipmentMode = new SelectList(_shipmentModeRepository.All().Select(e => new { id = e.ExpenseTypeId, name = e.ExpenseType }), "id", "name");
            ViewBag.CustomerName = new SelectList(_service.GetCustomers(), "Value", "Text");
            ViewBag.CustomerDeliveryAddress = new SelectList(_service.GetAddresses(), "Value", "Text");
            ViewBag.PortName = new SelectList(_portNameRepository.All().Select(e => new { id = e.PortNameId, name = e.PortName }), "id", "name");
            ViewBag.CurrencyForLC = new SelectList(_currencyRepository.All().Select(e => new { id = e.CurrencyId, name = e.CurrencyName }), "id", "name");
            ViewBag.CurrencyForInvoice = new SelectList(_currencyRepository.All().Select(e => new { id = e.CurrencyId, name = e.CurrencyName }), "id", "name");
            ViewBag.QuantityUnit = new SelectList(_unitRepository.All().Select(e => new { id = e.UnitTypId, name = e.UnitTypeName }), "id", "name");
            ViewBag.WeightUnit = new SelectList(_unitRepository.All().Select(e => new { id = e.UnitTypId, name = e.UnitTypeName }), "id", "name");
            ViewBag.PlaceOfLoading = new SelectList(_placeofLoadingRepository.All().Select(e => new { id = e.PlaceOfLoadingId, name = e.PlaceOfLoadingName }), "id", "name");
            ViewBag.ShedYard = new SelectList(_shedYardRepository.All().Select(e => new { id = e.ShedYardId, name = e.ShedYardName }), "id", "name");
            ViewBag.ShipmentStatus = new SelectList(_shipmentStatusRepository.All().Select(e => new { id = e.ExpenseTypeId, name = e.ExpenseType }), "id", "name");
            ViewBag.ImporterName = new SelectList(_importerInfoRepository.All().Select(e => new { id = e.ImporterId, name = e.Name }), "id", "name");
            ViewBag.FreightCharge = new SelectList(new List<string> { "Prepaid", "Collect" }, null);

            ViewBag.CountryList = new SelectList(_countryRepository.All().Select(e => new { id = e.CountryCode, name = e.CountryName }), "id", "name");
            ViewBag.CustomerList = new SelectList(_customerRepository.All().Select(e => new { id = e.CustomerCode, name = e.CustomerName }), "id", "name");
            ViewBag.ContactP = new SelectList(_contactPersionRepository.All().Select(e => new { id = e.Cpid, name = e.ContactPersonName }), "id", "name");
            ViewBag.shipStatus = new SelectList(shipmentStatusRepo.All().Select(e => new { id = e.ShipmentStatusId, name = e.ShipmentStatus }), "id", "name");

            return View(model);
        }

        #endregion

        [HttpGet]
        [ModulePermission]
        public async Task<IActionResult> GetJobs(int page = 1, int pageSize = 5, string? customerId = null, string? shipmentMode = null, DateTime? dateFrom = null, DateTime? dateTo = null, string? search = null)
        {
            var result = await _jobService.GetJobsPagedAsync(page, pageSize, customerId, shipmentMode, dateFrom, dateTo, search);

            return Json(new
            {
                data = result.Data,
                totalRecords = result.TotalRecords,
                filteredRecords = result.FilteredRecords // usually same as total if no search, but good to have
            });
        }

        [HttpGet]
        [ModulePermission]
        public async Task<IActionResult> GetJobDetail(int id)
        {
            var job = await _jobService.GetJobDetailAsync(id);
            return Json(job);
        }


        [HttpGet]
        [ModulePermission]
        public async Task<IActionResult> GetJobDetails(string jobNo , decimal id)
        {
            var jobTop = await _jobService.GetJobTopDetailsAsync(jobNo);
            var jobBottom = await _jobService.GetJobBottomDetailsAsync(jobNo , id);

            // Anonymous object হিসেবে JSON রিটার্ন করা হবে
            return Ok(new { jobTop, jobBottom });
        }




        [HttpGet]
        [ModulePermission]
        public async Task<IActionResult> GetJobStatuses(int page = 1,int pageSize = 10,string? search = null,string? customerCode = null,DateTime? fromDate = null,DateTime? toDate = null)
        {
            var result = await _jobService.GetJobStatusesPagedAsync(
                page,
                pageSize,
                search,
                customerCode,
                fromDate,
                toDate);

            return Json(new
            {
                data = result.Data,
                filteredRecords = result.FilteredRecords
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ShipmentUpdateViewModel model)
        {
            bool isNewRecord = model.AutoId == 0;
            try
            {               

                var permissionResult = await CheckAddOrEditPermissionAsync(isNewRecord);
                if (permissionResult != null)
                    return permissionResult;

                if (!ModelState.IsValid)
                    return Json(ApiResponse.Error("Please correct the highlighted fields."));

                string company = "001";
                var result = await _jobService.UpdateJobs(model, company);

                var successMessage = isNewRecord ? AppMessages.SaveSuccess : AppMessages.UpdateSuccess;
                return Json(ApiResponse.Success(successMessage, result));
            }
            catch (Exception)
            {
                return Json(ApiResponse.Error(isNewRecord ? AppMessages.SaveFailed : AppMessages.UpdateFailed));
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ModulePermission(PermissionType.Delete)]
        public async Task<IActionResult> DeleteJobs([FromBody] DeleteJobsRequest request)
        {
            try
            {
                if (request?.Ids == null || request.Ids.Count == 0)
                    return Json(ApiResponse.Error("No records selected"));

                var result = await _jobService.DeleteJobs(request.Ids);
                return Json(ApiResponse.Success(AppMessages.DeleteSuccess, result));
            }
            catch (Exception ex)
            {
                return Json(ApiResponse.Error(AppMessages.DeleteFailed));
            }
        }
        [HttpGet]
        [ModulePermission]
        public async Task<IActionResult> GetJobTimeline(string jobNo)
        {
            var timeline = await _jobService.GetJobTimelineAsync(jobNo);
            return Json(timeline);
        }
    }
}
