using GCTL.Core.ViewModels.ExpenseHead;

using GCTL_App.Controllers;

using Microsoft.AspNetCore.Mvc;

using GCTL.Service.ShipmentMode;

using GCTL.Service.CoreServiceType;

using GCTL.UI.Core.Controllers;

using GCTL.Service.ExpenseHead;

using GCTL.Service.Pagination;

using GCTL.Core.DataTables;

using Microsoft.AspNetCore.Mvc.Rendering;

using GCTL.Core.Helpers;

namespace GCTL.Controllers

{

    public class ExpenseHeadController : BaseController

    {

        #region

        private readonly IExpenseHead _service;

        private readonly IShipmentMode _shipmentMode;

        private readonly IServiceType _serviceType;

        public ExpenseHeadController(IExpenseHead service, IShipmentMode shipmentMode, IServiceType serviceType) : base()

        {

            _service = service;

            _shipmentMode = shipmentMode;

            _serviceType = serviceType;

        }

        #endregion

        #region

        public async Task<IActionResult> Index()

        {

            ViewData["Title"] = "Expense Head";

            ViewData["HideBreadcrumb"] = true;

            try

            {

                var dropdowns = await _service.GetDropdownsSpAsync();

                ViewBag.ShipmentModes = dropdowns.ShipmentModes;

                ViewBag.ServiceProviders = dropdowns.ServiceProviders;

                ViewBag.DropdownsJson = System.Text.Json.JsonSerializer.Serialize(dropdowns, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });

                var initialList = await _service.GetAllDataAsync();

                var options = new System.Text.Json.JsonSerializerOptions

                {

                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase

                };

                ViewBag.InitialDataJson = System.Text.Json.JsonSerializer.Serialize(initialList, options);

            }

            catch (Exception ex)

            {

                ViewBag.InitialDataJson = "[]";

                ViewBag.DropdownsJson = "{}";

                Console.WriteLine($"Error preloading ExpenseHead data: {ex.Message}");

            }

            return View();

        }

        public async Task<IActionResult> ExpenseForm()

        {

            await PopulateDropdowns();

            return PartialView();

        }

        public IActionResult ExpenseList()

        {

            return PartialView();

        }

        private async Task PopulateDropdowns()
        {
            var dropdowns = await _service.GetDropdownsSpAsync();
            ViewBag.ShipmentModes = dropdowns.ShipmentModes;
            ViewBag.ServiceProviders = dropdowns.ServiceProviders;
        }

        #endregion

        #region

        [HttpGet]

        [Route("ExpenseHead/GetAllData")]

        [Route("expense-head/all-data")]

        [Route("expense-head-all")]

        public async Task<IActionResult> GetAllData(string expenseType = "", string serviceType = "", string isReceiptable = "", string status = "", string searchTerm = "")

        {

            try

            {

                var list = await _service.GetAllDataAsync(expenseType, serviceType, isReceiptable, status, searchTerm);

                var dropdowns = await _service.GetDropdownsSpAsync();

                return Ok(new

                {

                    success = true,

                    data = list,

                    shipmentModes = dropdowns.ShipmentModes.Select(m => new { id = m.Code, name = m.Name }),

                    serviceTypes = dropdowns.ServiceProviders.Select(s => new { id = s.Code, name = s.Name })

                });

            }

            catch (Exception ex)

            {

                return StatusCode(500, new { success = false, message = ex.Message });

            }

        }

        [HttpPost]

        [Route("ExpenseHead/FastSave")]

        [Route("expense-head/fast-save")]

        public async Task<IActionResult> FastSave([FromBody] ExpenseHeadVM model)

        {

            try

            {

                if (model == null) return BadRequest(new { success = false, message = "Expense Head data is required." });

                if (string.IsNullOrWhiteSpace(model.ExpenseHead)) return BadRequest(new { success = false, message = "Expense Head name is required." });

                if (string.IsNullOrWhiteSpace(model.ExpenseTypeID) && !string.IsNullOrWhiteSpace(model.ExpenseType))
                    model.ExpenseTypeID = model.ExpenseType;

                if (string.IsNullOrWhiteSpace(model.ServiceTypeID) && !string.IsNullOrWhiteSpace(model.ServiceTypeName))
                    model.ServiceTypeID = model.ServiceTypeName;

                if (string.IsNullOrWhiteSpace(model.ExpenseTypeID)) return BadRequest(new { success = false, message = "Shipment Mode is required." });

                if (string.IsNullOrWhiteSpace(model.ServiceTypeID)) return BadRequest(new { success = false, message = "Service Provider is required." });

                model.ToAudit(LoginInfo, model.TC.HasValue && model.TC.Value > 0);

                var saved = await _service.SaveOrUpdateSpAsync(model, LoginInfo?.Username, Request.HttpContext.Connection.RemoteIpAddress?.ToString());
                if (saved == null) saved = model;

                return Ok(new { success = true, data = saved, message = $"Saved: {model.ExpenseHead}" });

            }

            catch (Exception ex)

            {

                return StatusCode(500, new { success = false, message = ex.Message });

            }

        }

        [HttpPost]

        [HttpDelete]

        [Route("ExpenseHead/BulkDeleteExpenseHead")]

        [Route("expense-head-bulk")]

        public async Task<IActionResult> BulkDeleteExpenseHead([FromBody] List<int> ids)

        {

            try

            {

                if (ids == null || !ids.Any())

                    return Json(new { isSuccess = false, message = "No Data is selected to delete" });

                var result = await _service.BulkDeleteAsync(ids);

                if (!result)

                    return Json(new { isSuccess = false, message = "No Data found to delete" });

                return Json(new { isSuccess = true, message = "Data Deleted Successfully." });

            }

            catch (Exception ex)

            {

                return Json(new { isSuccess = false, message = ex.Message });

            }

        }

        [HttpGet("next-expense-head-id")]

        public async Task<string> GetNextExpenseHeadId()

        {

                var lastId = await _service.GetLastExpenseHeadAsync();

                string newId;

                if (!string.IsNullOrEmpty(lastId) && int.TryParse(lastId, out int lastNumericId))

                {

                    int nextId = lastNumericId + 1;

                    newId = nextId.ToString("D3");

                }

                else

                {

                    newId = "001";

                }

                return newId;

        }

        [HttpPost]

        [Route("expense-head/check-duplicate")]

        public async Task<IActionResult> CheckDuplicate([FromBody] ExpenseHeadVM model)

        {

            if (model == null) return BadRequest(new { message = "Invalid data." });

            var isDuplicate = await _service.IsDuplicateAsync(model);

            if (isDuplicate)

            {

                return Ok(new { isDuplicate = true, message = "Data Already Exists." });

            }

            return Ok(new { isDuplicate = false, message = "No duplicate found." });

        }

        [HttpGet]

        [Route("expense-head/get-by-id/{id}")]

        public async Task<IActionResult> GetById(int id)

        {

            var result = await _service.GetByIdAsync(id);

            if (result == null)

                return NotFound(new { message = "Data not found" });

            return Ok(new { data = result });

        }

        [HttpPost]

        public async Task<IActionResult> GetGridData(

            int draw = 1,

            int pageNumber = 1,

            int pageSize = 10,

            string searchTerm = "",

            string sortColumn = "",

            string sortOrder = "asc",

            string expenseType = "",

            string isReceiptable = "",

            string serviceType = "")

        {

            try

            {

                var request = new PaginationRequest

                {

                    PageNumber = pageNumber,

                    PageSize = pageSize,

                    SearchTerm = searchTerm,

                    SortColumn = sortColumn,

                    SortOrder = sortOrder

                };

                var result = await _service.GetPaginatedAsync(request, expenseType, isReceiptable, serviceType);

                if (result.Data == null || !result.Data.Any())

                {

                    return Ok(new

                    {

                        draw,

                        data = new List<ExpenseHeadVM>(),

                        recordsTotal = 0,

                        recordsFiltered = 0

                    });

                }

                return Ok(new

                {

                    draw,

                    data = result.Data,

                    recordsTotal = result.TotalCount,

                    recordsFiltered = result.FilteredCount

                });

            }

            catch (Exception ex)

            {

                return StatusCode(500, new { message = "An error occurred.", error = ex.Message });

            }

        }

        [HttpGet]

        [Route("expense-head-list")]

        public async Task<IActionResult> GetAllPaginated(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string sortColumn = "ExpenseHeadID", string sortOrder = "desc", string expenseType = "",string isReceiptable = "",string serviceType = "")

        {

            try

            {

                //var result = await _service.GetPaginatedAsync(pageNumber, pageSize, searchTerm, sortColumn, sortOrder,expenseType,isReceiptable,serviceType);

                //if (result.Data == null || !result.Data.Any())

                //{

                //    return Ok(new

                //    {

                //        Data = new List<ExpenseHeadVM>(),

                //        TotalCount = 0,

                //        PaginationInfo = new

                //        {

                //            StartItem = 0,

                //            EndItem = 0,

                //            TotalItems = 0,

                //            PageNumbers = new List<int>(),

                //            TotalPages = 0,

                //            CurrentPage = 0

                //        },

                //        Message = "No Expense Head Found."

                //    });

                //}

                return Ok();

            }

            catch (Exception ex)

            {

                return StatusCode(500, new { message = "An error occurred.", error = ex.Message });

            }

        }

        [HttpPost]

        [Route("expense-head")]

        public async Task<ActionResult> Create([FromBody] ExpenseHeadVM model)

        {

            try

            {

                if (!ModelState.IsValid)

                {

                    return BadRequest(ModelState);

                }

                if (model == null)

                    return BadRequest(new { message = "Expense Head is required." });

                model.ToAudit(LoginInfo, false);

                var result = await _service.SaveAsync(model);

                if (!result)

                    return BadRequest(new { message = "Insertion Failed." });

                return Ok(new { success = true, message = "Data Saved Successfully." });

            }

            catch (Exception ex)

            {

                return StatusCode(500, new { message = "An error occurred.", error = ex.Message });

            }

        }

        [HttpPut]

        [Route("expense-head/{id}")]

        public async Task<IActionResult> Edit(int id, [FromBody] ExpenseHeadVM model)

        {

            if (!ModelState.IsValid)

            {

                return BadRequest(ModelState);

            }

            if (model == null || id != model.TC)

                return BadRequest(new { message = "Data is invalid." });

            var typeExists = await _service.IsExistAsync(id);

            if (!typeExists)

                return NotFound(new { message = $"Expense Head With ID {id} Not Found." });

            model.ToAudit(LoginInfo, true);

            var result = await _service.UpdateAsync(model);

            if (!result)

                return BadRequest(new { message = "Updated Failed." });

            return Ok(new { success = true, message = "Data updated successfully." });

        }

        [HttpDelete]

        [Route("expense-head-list")]

        public async Task<ActionResult> BulkDelete([FromBody] List<int> ids)

        {

            try

            {

                if (ids == null || !ids.Any() || ids.Count == 0)

                {

                    return Json(new { isSuccess = false, message = "No Data is selected to delete" });

                }

                var result = await _service.BulkDeleteAsync(ids);

                if (!result)

                {

                    return Json(new { isSuccess = false, message = "No Data found to delete" });

                }

                return Json(new { isSuccess = true, message = $"Data Deleted Successfully." });

            }

            catch (Exception ex)

            {

                return Json(new { isSuccess = false, message = ex.Message });

            }

        }

        public async Task<IActionResult> GetAll()

        {

            var data = await _service.GetAllAsync();

            return Ok(new { data });

        }

        [HttpGet("expense-type-dropdown")]

        public async Task<IActionResult> GetExpenseType()

        {

            var list = await _shipmentMode.GetAllExpenseTypesAsync();

            return Ok(list);

        }

        [HttpGet("core-service-dropdown")]

        public async Task<IActionResult> GetCoreServiceType()

        {

            var list = await _serviceType.GetAllCoreServiceAsync();

            return Ok(list);

        }

        [HttpGet("serial-no")]

        public async Task<IActionResult> GetSerialNo(string expenseType,string serviceType,string isReceiptable,int? requestedSerial = null)

        {

            try

            {

                int serial = await _service.GetNextSerialNoAsync(expenseType, serviceType, isReceiptable, requestedSerial);

                return Ok(new { serialNo = serial.ToString() });

            }

            catch (ArgumentException ex)

            {

                return BadRequest(new { message = ex.Message });

            }

        }

        #endregion

        #region Report

        [HttpGet("/expense-head/report")]

        public async Task<IActionResult> GetExpenseHeadReport(
            [FromQuery] string expenseTypeId,
            [FromQuery] string serviceType,
            [FromQuery] string isReceiptable,
            [FromQuery] string status,
            [FromQuery] string search,
            [FromQuery] string format)

        {

            try

            {

                byte[] content;

                if (format?.ToLower() == "pdf")

                {

                    content = await _service.GeneratePdfReportAsync(expenseTypeId, serviceType, isReceiptable, status, search);

                    string fileName = $"ExpenseHeadReport_{DateTime.Now:yyyyMMddHHmmss}.pdf";

                    return File(content, "application/pdf", fileName);

                }

                else // default Excel

                {

                    content = await _service.GenerateExcelReportAsync(expenseTypeId, serviceType, isReceiptable, status, search);

                    string fileName = $"ExpenseHeadReport_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);

                }

            }

            catch (Exception ex)

            {

                return StatusCode(500, $"Internal server error: {ex.Message}");

            }

        }

        #endregion

    }

}

