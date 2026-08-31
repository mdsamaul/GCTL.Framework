using GCTL.Core.ViewModels.ChartOfAccount;
using GCTL.Service.ChartOfAccounts.ControlLedger;
using GCTL.UI.Core.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.Controllers.CharOfAccount
{
    public class ControlLedgerController : BaseController
    {
        private readonly IContrlLedger _service;
        public ControlLedgerController(IContrlLedger service) : base()
        {
            _service = service;
        }


        [HttpGet("next-control-ledger-code")]
        public async Task<IActionResult> GenerateNewIdAsync(string groupledgercode)
        {
            var newSubCode = await _service.GetLastControlLedgerCodeAsync(groupledgercode);
            return Ok(newSubCode);
        }


        [HttpGet]
        [Route("control-ledger/details/{id}")]
        public async Task<IActionResult> GetControlLedgerDetails(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid ID." });

            var data = await _service.GetByIdAsync(id);

            if (data == null)
                return NotFound(new { message = $"Control Ledger with ID {id} not found." });

            return Ok(data);
        }


        [HttpGet]
        [Route("control-ledger-list")]
        public async Task<IActionResult> GetAllPaginated(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string sortColumn = "SubControlLedgerCodeNo", string sortOrder = "desc", string groupLedgerCode = "")
        {
            try
            {
                var result = await _service.GetPaginatedAsync(pageNumber, pageSize, searchTerm, sortColumn, sortOrder, groupLedgerCode);

                if (result.Data == null || !result.Data.Any())
                {
                    return Ok(new
                    {
                        Data = new List<ControlLedgerVM>(),
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
                        Message = "No Control Ledger Found."
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
        [Route("control-ledger/check-duplicate")]
        public async Task<IActionResult> CheckDuplicate([FromBody] ControlLedgerVM model)
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
        [Route("control-ledger")]
        public async Task<ActionResult> Create([FromBody] ControlLedgerVM model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (model == null)
                    return BadRequest(new { message = "Control Ledger is Required." });

                var result = await _service.SaveAsync(model);


                if (!result)
                    return BadRequest(new { message = "Insertion Failed." });



                return Ok(new { success = true, message = "Data Saved Successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An Error Occurred.", error = ex.Message });
            }
        }


        [HttpPut]
        [Route("control-ledger/{id}")]
        public async Task<IActionResult> Edit(int id, [FromBody] ControlLedgerVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (model == null || id != model.autoId)
                return BadRequest(new { message = "Data Is Invalid." });

            var typeExists = await _service.IsExistAsync(id);
            if (!typeExists)
                return NotFound(new { message = $"Control Ledger with ID {id} Not Found." });

            var result = await _service.UpdateAsync(model);

            if (!result)
                return BadRequest(new { message = "Updated Failed." });

            return Ok(new { success = true, message = "Data Updated Successfully." });
        }



        [HttpDelete]
        [Route("control-ledger-all-delete")]
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


        [HttpPost]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllAsync();
            return Ok(new { data = data });
        }


        #region Group Ledger Dropdown

        [HttpGet]
        [Route("group-ledger-dropdown")]
        public async Task<IActionResult> GetAllGroupLedger()
        {
            var list = await _service.GroupLedgerDropdown();
            return Ok(new { data = list });
        }

        [HttpGet]
        [Route("group-ledger-info")]
        public async Task<IActionResult> GetGroupLedger(string groupcode)
        {
            var ledger = await _service.GetAllGroupLedger(groupcode);

            if (ledger == null)
                return NotFound();

            return Ok(ledger);
        }

        #endregion
    }
}
