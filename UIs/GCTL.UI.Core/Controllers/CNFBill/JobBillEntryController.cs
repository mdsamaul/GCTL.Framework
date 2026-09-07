using GCTL.Core.Data;
using GCTL.Core.DataTables;
using GCTL.Core.ViewModels.ClearAndF.BillEntry;
using GCTL.Data.Models;
using GCTL.Service.BillService;
using GCTL.Service.SepDocumentation;
using GCTL.UI.Core.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GCTL.UI.Core.Controllers
{
    public class JobBillEntryController : BaseController
    {
        private readonly IBillService _billService;
        private readonly ISepDocumentationService _sepDocumentationService;

        private readonly IRepository<CfDefExpenseType> _shipmentModeRepository;
        private readonly IRepository<SalesDefPortName> _portNameRepository;
        private readonly IRepository<CaDefCurrency> _currencyRepository;
        private readonly IRepository<RmgProdDefUnitType> _unitRepository;
        private readonly IRepository<CfDefPlaceOfLoading> _placeofLoadingRepository;
        private readonly IRepository<CfDefShedYard> _shedYardRepository;
        private readonly IRepository<CfDefExpenseType> _shipmentStatusRepository;
        private readonly IRepository<SalesDefImporterInfo> _importerInfoRepository;
        private readonly IRepository<SalesCustomer> _customerRepository;
        private readonly IRepository<Country> _countryRepository;
        private readonly IRepository<SalesContactPerson> _contactPersionRepository;
        private readonly IRepository<CustomerHandilingCharge> _handlingChargeRepository;

        public JobBillEntryController(
            IBillService billService,
            ISepDocumentationService sepDocumentationService,
            IRepository<CfDefExpenseType> shipmentModeRepository,
            IRepository<SalesDefPortName> portNameRepository,
            IRepository<CaDefCurrency> currencyRepository,
            IRepository<RmgProdDefUnitType> unitRepository,
            IRepository<CfDefPlaceOfLoading> placeofLoadingRepository,
            IRepository<CfDefShedYard> shedYardRepository,
            IRepository<CfDefExpenseType> shipmentStatusRepository,
            IRepository<SalesDefImporterInfo> importerInfoRepository,
            IRepository<SalesCustomer> customerRepository,
            IRepository<Country> countryRepository,
            IRepository<SalesContactPerson> contactPersionRepository,
            IRepository<CustomerHandilingCharge> handlingChargeRepository)
        {
            _billService = billService;
            _sepDocumentationService = sepDocumentationService;
            _shipmentModeRepository = shipmentModeRepository;
            _portNameRepository = portNameRepository;
            _currencyRepository = currencyRepository;
            _unitRepository = unitRepository;
            _placeofLoadingRepository = placeofLoadingRepository;
            _shedYardRepository = shedYardRepository;
            _shipmentStatusRepository = shipmentStatusRepository;
            _importerInfoRepository = importerInfoRepository;
            _customerRepository = customerRepository;
            _countryRepository = countryRepository;
            _contactPersionRepository = contactPersionRepository;
            _handlingChargeRepository = handlingChargeRepository;
        }

        [ModulePermission]
        public IActionResult Index()
        {
            ViewBag.ShipmentMode = new SelectList(_shipmentModeRepository.All().Select(e => new { id = e.ExpenseTypeId, name = e.ExpenseType }), "id", "name");
            ViewBag.CustomerName = new SelectList(_sepDocumentationService.GetCustomers(), "Value", "Text");
            ViewBag.CustomerDeliveryAddress = new SelectList(_sepDocumentationService.GetAddresses(), "Value", "Text");
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
            ViewBag.Head = new SelectList(_handlingChargeRepository.All().Select(e => new { id = e.Tc, name = e.Chchead }), "id", "name");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetJobs(
            int? draw = null,
            int pageNumber = 1,
            int pageSize = 5,
            int? page = null,
            int? start = null,
            int? length = null,
            string? searchTerm = null,
            string? search = null,
            string? sortColumn = null,
            string? sortOrder = null,
            string? customerName = null,
            string? shipmentMode = null,
            string? dateFrom = null,
            string? dateTo = null)
        {
            if (length.HasValue && length.Value > 0)
            {
                pageSize = length.Value;
                pageNumber = (start ?? 0) / pageSize + 1;
            }
            else if (page.HasValue && page.Value > 0)
            {
                pageNumber = page.Value;
            }

            var request = new PaginationRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = !string.IsNullOrWhiteSpace(searchTerm) ? searchTerm : search,
                SortColumn = sortColumn,
                SortOrder = sortOrder
            };

            var result = await _billService.GetBillPagedAsync(request, customerName, shipmentMode, dateFrom, dateTo);

            return Json(new
            {
                draw = draw,
                data = result.Data,
                recordsTotal = result.TotalCount,
                recordsFiltered = result.FilteredCount,
                totalRecords = result.TotalCount,
                filteredRecords = result.FilteredCount
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetExpDetailTbl(int id)
        {
            var details = await _billService.GetExpenseDetailsAsync(id);
            if (details == null)
            {
                return Json(new { success = false });
            }

            return Json(new { success = true, data = details });
        }

        [HttpGet]
        public async Task<IActionResult> GetChrgTblData(int id)
        {
            try
            {
                var data = await _billService.GetChargeTableDataAsync(id);
                if (data == null)
                {
                    return Json(new { success = false });
                }

                return Json(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetJobDetail(int id)
        {
            var job = await _billService.GetJobDetailAsync(id);
            return Json(job);
        }

        [HttpPost]
        public async Task<IActionResult> AddCost(int headId, decimal cost, string jobNo, string? customerId, decimal tc)
        {
            var request = new AddCostRequestDto
            {
                HeadId = headId,
                Cost = cost,
                JobNo = jobNo,
                CustomerId = customerId,
                Tc = tc
            };

            var result = await _billService.AddCostAsync(request, LoginInfo);
            if (!result.Success)
            {
                return Json(new { success = false, error = result.Message });
            }

            return Json(new { success = true, sepTC = result.Data });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCharge(decimal tc)
        {
            var result = await _billService.DeleteChargeAsync(tc, LoginInfo);
            if (!result.Success)
            {
                return Json(new { success = false, error = result.Message });
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> GetBillHistory(
            int? draw = null,
            int pageNumber = 1,
            int pageSize = 10,
            int? page = null,
            int? start = null,
            int? length = null,
            string? searchTerm = null,
            string? search = null,
            string? sortColumn = null,
            string? sortOrder = null)
        {
            if (length.HasValue && length.Value > 0)
            {
                pageSize = length.Value;
                pageNumber = (start ?? 0) / pageSize + 1;
            }
            else if (page.HasValue && page.Value > 0)
            {
                pageNumber = page.Value;
            }

            var request = new PaginationRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = !string.IsNullOrWhiteSpace(searchTerm) ? searchTerm : search,
                SortColumn = sortColumn,
                SortOrder = sortOrder
            };

            var result = await _billService.GetBillHistoryPagedAsync(request);

            return Json(new
            {
                draw = draw,
                data = result.Data,
                recordsTotal = result.TotalCount,
                recordsFiltered = result.FilteredCount,
                totalRecords = result.TotalCount,
                filteredRecords = result.FilteredCount
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetBillDetail(string billNo)
        {
            var bill = await _billService.GetBillDetailAsync(billNo);
            if (bill == null)
            {
                return Json(new { success = false, message = "Bill not found" });
            }
            return Json(bill);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteBills([FromBody] List<string> billNos)
        {
            var result = await _billService.DeleteBillsAsync(billNos, LoginInfo);
            return Json(new { success = result.Success, message = result.Message });
        }
    }
}
