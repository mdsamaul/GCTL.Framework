using GCTL.Core.ViewModels.ExpenseHead;
using GCTL_App.Controllers;
using Microsoft.AspNetCore.Mvc;
using GCTL.Service.ShipmentMode;
using GCTL.Service.CoreServiceType;
using GCTL.UI.Core.Controllers;
using GCTL.Service.ExpenseHead;

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
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult ExpenseForm()
        {
            return PartialView();
        }
        public IActionResult ExpenseList()
        {
            return PartialView();
        }
        #endregion

        #region
        [HttpDelete]
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
        [Route("expense-head-list")]
        public async Task<IActionResult> GetAllPaginated(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string sortColumn = "ExpenseHeadID", string sortOrder = "desc", string expenseType = "",string isReceiptable = "",string serviceType = "")
        {
            try
            {
                var result = await _service.GetPaginatedAsync(pageNumber, pageSize, searchTerm, sortColumn, sortOrder,expenseType,isReceiptable,serviceType);

                if (result.Data == null || !result.Data.Any())
                {
                    return Ok(new
                    {
                        Data = new List<ExpenseHeadVM>(),
                        TotalCount = 0,
                        PaginationInfo = new
                        {
                            StartItem = 0,
                            EndItem = 0,
                            TotalItems = 0,
                            PageNumbers = new List<int>(),
                            TotalPages = 0,
                            CurrentPage = 0
                        },
                        Message = "No Expense Head Found."
                    });
                }

                return Ok(result);
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
        public async Task<IActionResult> GetExpenseHeadReport([FromQuery] string expenseTypeId, [FromQuery] string format)
        {
            try
            {
                byte[] content;

                if (format?.ToLower() == "pdf")
                {
                    content = await _service.GeneratePdfReportAsync(expenseTypeId);
                    return File(content, "application/pdf", "ExpenseHeadReport.pdf");
                }
                else // default Excel
                {
                    content = await _service.GenerateExcelReportAsync(expenseTypeId);
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ExpenseHeadReport.xlsx");
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
