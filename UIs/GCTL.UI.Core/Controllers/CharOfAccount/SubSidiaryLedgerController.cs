using GCTL.Core.ViewModels.ChartOfAccount;
using GCTL.Service.ChartOfAccounts.ControlLedger;
using GCTL.Service.ChartOfAccounts.SubControlLedger;
using GCTL.Service.ChartOfAccounts.SubSidiaryLedger;
using GCTL.UI.Core.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace GCTL.Controllers.CharOfAccount
{
    public class SubSidiaryLedgerController : BaseController
    {
        private readonly ISubSidiaryLedger _service;
        private readonly ISubControlLedger _subcontrolLedger;
        private readonly IContrlLedger _controlLedger;
        public SubSidiaryLedgerController( ISubSidiaryLedger service, ISubControlLedger subControlLedger, IContrlLedger controlLedger) : base()
        {
            _service = service;
            _subcontrolLedger = subControlLedger;
            _controlLedger = controlLedger;
        }



        [HttpGet]
        [Route("subsidiary-ledger/details/{id}")]
        public async Task<IActionResult> GetSubSidiaryLedgerDetails(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid ID." });

            var data = await _service.GetByIdAsync(id);

            if (data == null)
                return NotFound(new { message = $"Sub-Sidiary Ledger with ID {id} not found." });

            return Ok(data);
        }


        [HttpGet("next-subsidiary-ledger-code")]
        public async Task<IActionResult> GenerateNewIdAsync(string subcontrolLedgercode)
        {
            var newCode = await _service.GetLastSubSidiaryLedgerCodeAsync(subcontrolLedgercode);
            return Ok(newCode);
        }


        [HttpGet]
        [Route("subsidiary-ledger-list")]
        public async Task<IActionResult> GetAllPaginated(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string sortColumn = "SusidiaryLedgerCodeNo", string sortOrder = "desc", string groupLedgerCode = "", string controlLedgerCode = "", string subcontrolLedgerCode = "")
        {
            try
            {
                var result = await _service.GetPaginatedAsync(pageNumber, pageSize, searchTerm, sortColumn, sortOrder, groupLedgerCode, controlLedgerCode, subcontrolLedgerCode);

                if (result.Data == null || !result.Data.Any())
                {
                    return Ok(new
                    {
                        Data = new List<SubsidiaryLedgerVM>(),
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
                        Message = "No Sub-Sidiary Ledger Found."
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
        [Route("subsidiary-ledger/check-duplicate")]
        public async Task<IActionResult> CheckDuplicate([FromBody] SubsidiaryLedgerVM model)
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
        [Route("subsidiary-ledger")]
        public async Task<ActionResult> Create([FromBody] SubsidiaryLedgerVM model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (model == null)
                    return BadRequest(new { message = "Sub-Sidiary Ledger is Required." });

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
        [Route("subsidiary-ledger/{id}")]
        public async Task<IActionResult> Edit(int id, [FromBody] SubsidiaryLedgerVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (model == null || id != model.autoId)
                return BadRequest(new { message = "Data Is Invalid." });

            var typeExists = await _service.IsExistAsync(id);
            if (!typeExists)
                return NotFound(new { message = $"Sub-Sidiary Ledger with ID {id} Not Found." });

            var result = await _service.UpdateAsync(model);

            if (!result)
                return BadRequest(new { message = "Updated Failed." });

            return Ok(new { success = true, message = "Data Updated Successfully." });
        }


        [HttpDelete]
        [HttpPost]
        [Route("subsidiary-ledger-all-delete")]
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
        public async Task<IActionResult> GetAll(string groupLedgerCode = "", string controlLedgerCode = "", string subcontrolLedgerCode = "", string subSididaryLedger = "", string subsidiaryLedgerCode = "")
        {
            var subLedger = !string.IsNullOrEmpty(subSididaryLedger) ? subSididaryLedger : subsidiaryLedgerCode;
            var data = await _service.GetAllAsync(groupLedgerCode, controlLedgerCode, subcontrolLedgerCode, subLedger);
            return Ok(new { data = data });
        }


        #region Dropdown Section

        #region Group Ledger 

        //Group Ledger Dropdown

        [HttpGet]
        [Route("forsubsidiary-group-ledger-dropdown")]
        public async Task<IActionResult> GetAllGroupLedger()
        {
            var list = await _controlLedger.GroupLedgerDropdown();
            return Ok(new { data = list });
        }

        //Group Ledger related Info

        [HttpGet]
        [Route("forsubsidiary-group-ledger-info")]
        public async Task<IActionResult> GetGroupLedger(string groupcode)
        {
            var ledger = await _controlLedger.GetAllGroupLedger(groupcode);

            if (ledger == null)
                return NotFound();

            return Ok(ledger);
        }

        #endregion


        #region Control Ledger

        //Control Ledger Dropdown
        [HttpGet]
        [Route("forSubsidiary-control-ledger-dropdown")]
        public async Task<IActionResult> GetControlLedgerDropdown(string groupcode)
        {
            var list = await _subcontrolLedger.ControlLedgerDropdown(groupcode);
            return Ok(new { data = list });
        }

        //Control Ledger Info

        [HttpGet]
        [Route("forSubsidiary-control-ledger-info")]
        public async Task<IActionResult> GetControlLedgerInfo(string contrlcode)
        {
            var ledger = await _subcontrolLedger.GetAllControlInfo(contrlcode);

            if (ledger == null)
                return NotFound();

            return Ok(ledger);
        }

        #endregion


        #region Sub-Control Ledger

        //Sub Control Ledger
        [HttpGet]
        [Route("subcontrol-ledger-dropdown")]
        public async Task<IActionResult> GetSubControlLedgerDropdown(string controlcode)
        {
            var list = await _service.SubControlLedgerDropdown(controlcode);
            return Ok(new { data = list });
        }

        //Sub Control Info
        [HttpGet]
        [Route("forsudsidiary-subcontrol-ledger-info")]
        public async Task<IActionResult> GetSubControlLedgerInfo(string subcontrlcode)
        {
            var ledger = await _service.SubControlLedgerInfo(subcontrlcode);

            if (ledger == null)
                return NotFound();

            return Ok(ledger);
        }

        #endregion

        #endregion
    }
}
