using GCTL.Core.Data;
using GCTL.Data.Models;
using GCTL.Service.ShipmentMode;
using GCTL.UI.Core.Controllers;
using GCTL.UI.Core.ViewModels.ShipmentMode;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

using GCTL.Core.DataTables;

namespace GCTL.Controllers.MasterSetup
{
    public class ShipmentStatusController : BaseController
    {
        private readonly IShipmentStatusService _service;
        private readonly IGenericRepository<TransportModeType> transportModeTypeRepo;

        public ShipmentStatusController(IShipmentStatusService service, IGenericRepository<TransportModeType> transportModeTypeRepo) : base()
        {
            _service = service;
            this.transportModeTypeRepo = transportModeTypeRepo;
        }
        [HttpGet]
        [Route("/ShipmentStatus/ShipmentStatusForm")]
        public IActionResult ShipmentStatusForm()
        {
            ViewBag.TransportModeTypes = new SelectList(transportModeTypeRepo.All().Select(e => new { id = e.TransportModeId, name = e.TransportMode }), "id", "name");
            return PartialView("_ShipmentStatusForm");
        }

        [HttpGet]
        [Route("/ShipmentStatus/ShipmentStatusList")]
        public IActionResult ShipmentStatusList()
        {
            return PartialView("_ShipmentStatusList");
        }

        [HttpGet("next-shipment-status-id")]
        public async Task<IActionResult> GetNextShipmentStatusId()
        {
            var lastId = await _service.GetLastIdAsync();

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

            return Ok(newId);
        }

        [HttpPost]
        [Route("shipment-status/get-grid-data")]
        public async Task<IActionResult> GetGridData(
            int draw = 1,
            int pageNumber = 1,
            int pageSize = 10,
            string searchTerm = "",
            string sortColumn = "ShipmentStatusId",
            string sortOrder = "desc")
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

                var result = await _service.GetPaginatedAsync(request);

                if (result.Data == null || !result.Data.Any())
                {
                    return Ok(new
                    {
                        draw,
                        data = new List<ShipmentStatusVM>(),
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

        [HttpGet("shipment-status/get-by-id/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _service.GetByIdAsync(id);
            if (data == null)
                return NotFound();
            return Ok(new { data });
        }

        [HttpPost]
        [Route("shipment-status")]
        public async Task<IActionResult> AddShipmentStatus([FromBody] ShipmentStatusVM model)
        {
            try
            {
                if (model == null)
                    return BadRequest(new { message = "Shipment Status is required." });

                model.Luser = LoginInfo?.EmployeeId ?? "";
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
        [Route("shipment-status/{id}")]
        public async Task<IActionResult> UpdateShipmentStatus(int id, [FromBody] ShipmentStatusVM model)
        {
            if (model == null || id != model.AutoId)
                return BadRequest(new { message = "Data is invalid." });

            var exists = await _service.IsExistAsync(id);
            if (!exists)
                return NotFound(new { message = $"Shipment Status with ID {id} not found." });

            var result = await _service.UpdateAsync(model);

            if (!result)
                return BadRequest(new { message = "Update Failed." });

            return Ok(new { success = true, message = "Data updated successfully." });
        }

        [HttpDelete]
        [Route("shipment-status-multi")]
        public async Task<IActionResult> MultipleDeleteShipmentStatus([FromBody] List<int> ids)
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
        [Route("shipment-status/is-duplicate")]
        public async Task<IActionResult> IsDuplicateAsync([FromBody] ShipmentStatusVM model)
        {
            var isDuplicate = await _service.IsDuplicateAsync(model);

            if (isDuplicate)
                return Ok(new { isDuplicate = true, message = "Data already exists." });

            return Ok(new { isDuplicate = false });
        }
        [HttpGet]
        public async Task<IActionResult> GetShipmentStatusDetails(string transportModeTypeId)
        {
            var result = await _service.GetShipmentStatusDetailsAsync(transportModeTypeId);

            if (result == null || !result.Any())
            {
                return NotFound();
            }

            return Json(result);
        }

    }
}