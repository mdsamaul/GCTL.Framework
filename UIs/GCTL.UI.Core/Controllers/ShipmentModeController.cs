
using GCTL.Core.Data;
using GCTL.Core.DataTables;
using GCTL.Core.Helpers;
using GCTL.Core.ViewModels;
using GCTL.Data.Models;
using GCTL.Service.ShipmentMode;
using GCTL.UI.Core.Controllers;
using GCTL.UI.Core.ViewModels.SepDocUpdate;
using GCTL.UI.Core.ViewModels.ShipmentMode;
using GCTL_App.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;



namespace GCTL.Controllers.MasterSetup
{
    public class ShipmentModeController : BaseController
    {
        private readonly IShipmentMode _service;
        private readonly IGenericRepository<TransportModeType> transportModeTypeRepo;

        public ShipmentModeController( IShipmentMode service, IGenericRepository<TransportModeType> transportModeTypeRepo) : base()
        {
            _service = service;
            this.transportModeTypeRepo = transportModeTypeRepo;
        }
        public IActionResult Index()
        {
            ShipmentModeSetupVm model = new ShipmentModeSetupVm()
            {
                PageUrl = Url.Action(nameof(Index))
            };

            ViewBag.TransportModeTypes = new SelectList(transportModeTypeRepo.All().Select(e => new { id = e.TransportModeId, name = e.TransportMode }), "id", "name");


            return View(model);
        }
        public IActionResult ShipmentForm()
        {
            ViewBag.TransportModeTypes = new SelectList(transportModeTypeRepo.All().Select(e => new { id = e.TransportModeId, name = e.TransportMode }), "id", "name");
            return PartialView();
        }
        public IActionResult ShipmentList()
        {
            return PartialView();
        }

        [HttpGet("next-expense-type-id")]
        public async Task<IActionResult> GetNextExpenseHeadId()
        {
            var lastId = await _service.GetLastUnitAsync();

            string newId;
            if (!string.IsNullOrEmpty(lastId) && int.TryParse(lastId, out int lastNumericId))
            {
                int nextId = lastNumericId + 1;
                newId = nextId.ToString("D4");
            }
            else
            {
                newId = "0001";
            }

            return Ok(newId); // Send as JSON/string
        }

        [HttpGet]
        [Route("get-grid-data")]
        public async Task<IActionResult> GetAllPaginatedExpense(int page = 1, int pageSize = 10, string searchTerm = "", string currentSortColumn = "ExpenseTypeID", string sortDirection = "desc")
        {
            try
            {
                var request = new PaginationRequest
                {
                    PageNumber = page,
                    PageSize = pageSize,
                    SearchTerm = searchTerm,
                    SortColumn = currentSortColumn,
                    SortOrder = sortDirection
                };

                var result = await _service.GetPaginatedAsync(request);

                if (result.Data == null || !result.Data.Any())
                {
                    return Ok(new
                    {
                        Data = new List<ShipmentModeVM>(),
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
                        Message = "No Unit Found."
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
            }
        }

        [HttpGet("shipment-mode/get-by-id/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null)
                return NotFound();
            return Ok(new { data });
        }

        [HttpPost]
        [Route("shipment-mode")]
        public async Task<IActionResult> AddShipmentMode([FromBody] ShipmentModeVM model)
        {
            try
            {
                if (model == null)
                    return BadRequest(new { message = "Shipment Mode is required." });

                model.ToAudit(LoginInfo, model.TC>0);
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
        [Route("shipment-mode/{id}")]
        public async Task<IActionResult> UpdateShipmentMode(int id, [FromBody] ShipmentModeVM model)
        {
            if (model == null || id != model.TC)
                return BadRequest(new { message = "Data is invalid." });

            var typeExists = await _service.IsExistAsync(id);
            if (!typeExists)
                return NotFound(new { message = $"Shipment Mode with ID {id} not found." });

            model.ToAudit(LoginInfo, true);
            var result = await _service.UpdateAsync(model);

            if (!result)
                return BadRequest(new { message = "Updated Failed." });

            return Ok(new { success = true, message = "Data updated successfully." });
        }

        [HttpDelete]
        [Route("shipment-mode-multi")]
        public async Task<IActionResult> MultipleDeleteShipmentMode([FromBody] List<int> ids)
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
        [HttpPost]
        [Route("shipment-mode/is-duplicate")]
        public async Task<IActionResult> IsDuplicateAsync([FromBody] ShipmentModeVM model)
        {
            var isDuplicate = await _service.IsDuplicateAsync(model);

            if (isDuplicate)
                return Ok(new { isDuplicate = true, message = "Data already exists." });

            return Ok(new { isDuplicate = false });
        }

        [HttpGet]
        public async Task<IActionResult> GetShipmentModeDetails(string transportModeTypeId)
        {
            var result = await _service.GetShipmentModeDetailsAsync(transportModeTypeId);

            if (result == null || !result.Any())
            {
                return NotFound();
            }

            return Json(result);
        }

    }
}
