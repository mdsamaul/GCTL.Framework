using GCTL.Core.ViewModels.CurrencyType;
using GCTL.Service.CurrencyType;
using GCTL.UI.Core.Controllers;
using GCTL_App.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.Controllers
{
    public class CurrencyTypeController : BaseController
    {
        private readonly ICurrencyType _service;

        public CurrencyTypeController( ICurrencyType service) : base()
        {
            _service = service;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult CurrencyForm()
        {
            return PartialView();
        }
        public IActionResult CurrencyList()
        {
            return PartialView();
        }
        [HttpGet]
        [Route("currency-list")]
        public async Task<IActionResult> GetAllPaginated(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string sortColumn = "CurrencyId", string sortOrder = "desc")
        {
            try
            {
                var result = await _service.GetPaginatedAsync(pageNumber, pageSize, searchTerm, sortColumn, sortOrder);

                if (result.Data == null || !result.Data.Any())
                {
                    result.Data = new List<CurrencyTypeVM>();
                    result.TotalCount = 0;
                }
                return Ok(result);

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
            }
        }

        public async Task<ActionResult> Details(int id)
        {
            try
            {
                var data = await _service.GetByIdAsync(id);

                if (data == null)
                    return NotFound(new { message = $"CurrencyType with ID {id} not found." });

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
            }
        }

        [HttpPost]
        [Route("currency")]
        public async Task<ActionResult> Create([FromBody] CurrencyTypeVM model)
        {
            try
            {
                if (model == null)
                    return BadRequest(new { message = "CurrencyType data is required." });

                var result = await _service.SaveAsync(model);


                if (!result)
                    return BadRequest(new { message = "Insertion Failed." });



                return Json(new { isSuccess = true, message = "Data Saved Successfully.", });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
            }
        }
        [HttpPut]
        [Route("currency/{id}")]
        public async Task<IActionResult> Edit(int id, [FromBody] CurrencyTypeVM model)
        {
            try
            {
                if (model == null || id != model.TC)
                    return BadRequest(new { message = "CurrencyType data is invalid." });

                var typeExists = await _service.IsExistAsync(id);
                if (!typeExists)
                    return NotFound(new { message = $"CurrencyType with ID {id} not found." });

                var result = await _service.UpdateAsync(model);

                if (!result)
                    return BadRequest(new { message = "Updated Failed" });

                return Ok(new { isSuccess =true, message = "Data updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
            }
        }

        [HttpDelete]
        [Route("currency/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _service.DeleteAsync(id);

                if (!result)
                    return NotFound(new { message = $"CurrencyType with ID {id} not found." });

                return Ok(new { message = "Data deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
            }
        }

        [HttpDelete]
        [Route("currency-list-bulkdelete")]
        public async Task<ActionResult> BulkDelete([FromBody] List<decimal> ids)
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
                return Json(new { isSuccess = true, message = $" Data Deleted Successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { isSuccess = false, message = ex.Message });
            }
        }

        [HttpGet]
        [Route("next-currency-id")]
        public async Task<string> GenerateNewIdAsync()
        {
            var lastId = await _service.GetLastCurrencyAsync();

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

        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllAsync();
            return Ok(new { data = data });
        }

        [Route("check-duplicate")]
        public async Task<IActionResult> CheckDuplicate([FromForm] CurrencyTypeVM model)
        {
            var isDuplicate = await _service.IsDuplicateAsync(
                model
            );

            if (isDuplicate)
            {
                return Ok(new { isDuplicate = true, message = "Data Already Exixts." });
            }

            return Ok(new { isDuplicate = false, message = "No duplicate found." });
        }
    }
}
