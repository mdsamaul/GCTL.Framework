using GCTL.Core.ViewModels.ChartOfAccount;
using GCTL.Service.ChartOfAccounts.GroupLedger;
using GCTL.UI.Core.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.Controllers.CharOfAccount
{
    public class GroupLedgerController : BaseController
    {
        private readonly IGroupLedger _service;
        public GroupLedgerController(IGroupLedger service) : base()
        {
            _service = service;
        }

        [HttpGet("next-group-ledger-code")]
        public async Task<string> GenerateNewIdAsync()
        {
            var lastId = await _service.GetLastGroupLedgerCodeAsync();

            int nextId;

            if (!string.IsNullOrEmpty(lastId) && int.TryParse(lastId, out int lastNumericId))
            {
                nextId = lastNumericId + 1;
            }
            else
            {
                nextId = 1; 
            }

            return nextId.ToString(); 
        }

        [HttpGet]
        [Route("group-ledger/details/{id}")]
        public async Task<IActionResult> GetGroupLedgerDetails(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid ID." });

            var data = await _service.GetByIdAsync(id);

            if (data == null)
                return NotFound(new { message = $"Group Ledger with ID {id} not found." });

            return Ok(data);
        }

        [HttpGet]
        [Route("group-ledger-list")]
        public async Task<IActionResult> GetAllPaginated(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string sortColumn = "ControlLedgerCodeNo", string sortOrder = "desc")
        {
            try
            {
                var result = await _service.GetPaginatedAsync(pageNumber, pageSize, searchTerm, sortColumn, sortOrder);

                if (result.Data == null || !result.Data.Any())
                {
                    return Ok(new
                    {
                        Data = new List<GroupLedgerVM>(),
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
                        Message = "No Group Ledger Found."
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An Error Occurred.", error = ex.Message });
            }
        }

        [HttpPost]
        [Route("group-ledger/check-duplicate")]
        public async Task<IActionResult> CheckDuplicate([FromBody] GroupLedgerVM model)
        {
            if (model == null) return BadRequest(new { message = "Invalid Data." });

            var isDuplicate = await _service.IsDuplicateAsync(model);

            if (isDuplicate)
            {
                return Ok(new { isDuplicate = true, message = "Data Already Exists." });
            }

            return Ok(new { isDuplicate = false, message = "No Duplicate Found." });
        }

        [HttpPost]
        [Route("group-ledger")]
        public async Task<ActionResult> Create([FromBody] GroupLedgerVM model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (model == null)
                    return BadRequest(new { message = "Group Ledger is Required." });

                var result = await _service.SaveAsync(model);

                if (!result)
                    return BadRequest(new { message = "Insertion Failed." });


                return Ok(new { success = true, message = "Data Saved Successfully.",
                    savedId = model.ControlLedgerCodeNo,
                    savedName = model.ControlLedgerName
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An Error Occurred.", error = ex.Message });
            }
        }

        [HttpPut]
        [Route("group-ledger/{id}")]
        public async Task<IActionResult> Edit(int id, [FromBody] GroupLedgerVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (model == null || id != model.autoId)
                return BadRequest(new { message = "Data Is Invalid." });

            var typeExists = await _service.IsExistAsync(id);
            if (!typeExists)
                return NotFound(new { message = $"Group Ledger with ID {id} Not Found." });

            var result = await _service.UpdateAsync(model);

            if (!result)
                return BadRequest(new { message = "Updated Failed." });

            return Ok(new { success = true, message = "Data Updated Successfully." });
        }

        [HttpDelete]
        [Route("group-ledger-all-delete")]
        public async Task<ActionResult> BulkDelete([FromBody] List<decimal> ids)
        {
            try
            {
                if (ids == null || !ids.Any() || ids.Count == 0)
                {
                    return Json(new { isSuccess = false, message = "No Data Is Selected To Delete" });
                }

                var result = await _service.BulkDeleteAsync(ids);
                if (!result)
                {
                    return Json(new { isSuccess = false, message = "No Data Found to Delete" });
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
            return Ok(new { data = data });
        }

    }
}
