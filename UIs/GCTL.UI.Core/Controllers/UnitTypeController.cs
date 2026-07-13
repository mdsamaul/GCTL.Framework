
using GCTL.Core.ViewModels.UnitType;
using GCTL.Service.Pagination;
using GCTL.Service.UnitType;
using GCTL.UI.Core.Controllers;
using GCTL_App.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Linq.Expressions;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace GCTL.Controllers
{
    public class UnitTypeController : BaseController
    {
        private readonly IUnitType _service;
        public UnitTypeController(IUnitType service) : base()
        {
            _service = service;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult UnitForm()
        {
             return PartialView();
        }
        public IActionResult UnitList()
        {
            return PartialView();
        }

        [HttpGet("next-unit-id")]
        public async Task<string> GetNextUnitId()
        {

            var lastId = await _service.GetLastUnitAsync();

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

        [HttpGet]
        [Route("units")]

        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllAsync();
            return Ok(new { data = data });
        }

        //API to get paginated data
        [HttpGet]
        [Route("unit-list")]
        public async Task<IActionResult> GetAllPaginatedUnits(int page = 1,int pageSize = 10,string searchTerm = "",string currentSortColumn = "UnitTypID", string sortOrder = "desc")
        {
            try
            {
                var result = await _service.GetPaginatedAsync(page, pageSize, searchTerm, currentSortColumn, sortOrder);

                if (result.Data == null || !result.Data.Any())
                {
                    return Ok(new
                    {
                        Data = new List<UnitTypeVM>(),
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

        [HttpPost]
        [Route("unit")]
        public async Task<IActionResult> AddUnit([FromBody] UnitTypeVM model)
        {
            try
            {
                if (model == null)
                    return BadRequest(new { message = "Unit Type is required." });

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
        [Route("unit/{id}")]
        public async Task<IActionResult> UpdateUnit(int id, [FromBody] UnitTypeVM model)
        {
            if (model == null || id != model.TC)
                return BadRequest(new { message = "Data is invalid." });

            var typeExists = await _service.IsExistAsync(id);
            if (!typeExists)
                return NotFound(new { message = $"Unit Type with ID {id} not found." });

            var result = await _service.UpdateAsync(model);

            if (!result)
                return BadRequest(new { message = "Updated Failed." });

            return Ok(new { success = true, message = "Data updated successfully." });
        }

        [HttpDelete("unit-list")]
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

        [HttpPost]
        public async Task<IActionResult> CheckDuplicate([FromBody] UnitTypeVM model)
        {
            var isDuplicate = await _service.IsDuplicateAsync(model);

            if (isDuplicate)
            {
                return Ok(new { isDuplicate = true, message = "Data Already Exists." });
            }

            return Ok(new { isDuplicate = false, message = "No duplicate found." });
        }
    }
}
