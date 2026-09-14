using GCTL.Core.Data;
using GCTL.Core.DataTables;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.OFRSection.OFRAdjustApproval;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using Sep_Documentation = GCTL.Data.Models.SepDocumentation;
using Core_ServiceType = GCTL.Data.Models.CoreServiceType;

namespace GCTL.Service.OFRAdjustApproval
{
    public class OFRAdjustApprovalService : AppService<CfOperationalFundRequisitionEntry>, IOFRAdjustApproval
    {
        #region Service
        private readonly IRepository<CfOperationalFundRequisitionEntry> _repository;
        private readonly IRepository<CfOperationalFundRequisitionDetail> _details;
        private readonly IRepository<CfOperationalFundRequisitionDetailsTemp> _Tmpdetails;
        private readonly IRepository<CfDefExpenseType> _shipmentmode;
        private readonly IRepository<SalesCustomer> _customer;
        private readonly IRepository<Sep_Documentation> _documentation;
        private readonly IRepository<CfDefExpenseHead> _accountsHead;
        private readonly IRepository<Core_ServiceType> _serviceType;
        private readonly IRepository<AccSubSubsidiaryLedger> _generalLedger;
        private readonly IRepository<CoreBankAccountInformation> _bankAccountInfo;
        private readonly IRepository<Sep_Documentation> _sepDocumentation;

        public OFRAdjustApprovalService
            (
                IRepository<CfOperationalFundRequisitionEntry> genericRepository,
                IRepository<CfOperationalFundRequisitionEntry> repository,
                IRepository<CfDefExpenseType> shipmentmode,
                IRepository<SalesCustomer> customer,
                IRepository<Sep_Documentation> documentation,
                IRepository<CfOperationalFundRequisitionDetail> details,
                IRepository<CfDefExpenseHead> accountsHead,
                IRepository<Core_ServiceType> serviceType,
                IRepository<CfOperationalFundRequisitionDetailsTemp> tmpdetails,
                IRepository<AccSubSubsidiaryLedger> generalLedger,
                IRepository<CoreBankAccountInformation> bankAccountInfo,
                IRepository<Sep_Documentation> sepDocumentation
            ) : base(genericRepository)
        {
            _repository = repository;
            _shipmentmode = shipmentmode;
            _customer = customer;
            _documentation = documentation;
            _details = details;
            _accountsHead = accountsHead;
            _serviceType = serviceType;
            _Tmpdetails = tmpdetails;
            _generalLedger = generalLedger;
            _bankAccountInfo = bankAccountInfo;
            _sepDocumentation = sepDocumentation;
        }
        #endregion

        #region Grid Section

        #region Get All Requisition for Bottom Grid
        public async Task<PaginationResponse<OFRAdjustApprovalBottomGridVM>> AllRequisitionforBottomGrid(PaginationRequest request)
        {
            var query = from main in _repository.All()
                        join doc in _documentation.All()
                            on main.JobNo equals doc.JobNo
                        join cust in _customer.All()
                            on doc.CustomerId equals cust.CustomerId into cusG
                        from cust in cusG.DefaultIfEmpty()
                        join exp in _shipmentmode.All()
                            on main.ExpenseTypeId equals exp.ExpenseTypeId into shipG
                        from exp in shipG.DefaultIfEmpty()
                        where !string.IsNullOrEmpty(main.BillAdjustApprovalUser)
                        select new OFRAdjustApprovalBottomGridVM
                        {
                            TC = main.Tc,
                            OFRNo = main.Ofrno,
                            OFRDate = main.Ofrdate,
                            JobNo = main.JobNo,
                            CustomerID = cust != null ? cust.CustomerId : "",
                            CustomerName = cust != null ? cust.CustomerName : "",
                            BillingAddress = cust != null ? cust.CustomerAddress : "",
                            ShipmentModeID = exp != null ? exp.ExpenseTypeId : "",
                            ShipmentMode = exp != null ? exp.ExpenseType : ""
                        };

            if (request.PageSize == -1)
            {
                request.PageSize = await query.CountAsync();
                request.PageNumber = 1;
            }

            if (string.IsNullOrEmpty(request.SortColumn))
                request.SortColumn = nameof(OFRAdjustApprovalBottomGridVM.OFRNo);
            if (string.IsNullOrEmpty(request.SortOrder))
                request.SortOrder = "desc";

            return await PaginationService<OFRAdjustApprovalBottomGridVM>.GetPaginatedData(query, request);
        }
        #endregion

        #region Get All Requisition Entry in Top Grid
        public async Task<PaginationResponse<OFRAdjustApprovalTopGridVM>> GetAllRequisition(PaginationRequest request, string customerid = "", string shipmentmodeid = "")
        {
            var query = from main in _repository.All()
                        join doc in _documentation.All()
                            on main.JobNo equals doc.JobNo
                        join cust in _customer.All()
                            on doc.CustomerId equals cust.CustomerId into cusG
                        from cust in cusG.DefaultIfEmpty()
                        join exp in _shipmentmode.All()
                            on main.ExpenseTypeId equals exp.ExpenseTypeId into shipG
                        from exp in shipG.DefaultIfEmpty()
                        where string.IsNullOrEmpty(main.BillAdjustApprovalUser)
                        select new OFRAdjustApprovalTopGridVM
                        {
                            TC = main.Tc,
                            JobNo = main.JobNo,
                            JobDate = doc.Date,
                            DocReceivedDate = doc.DocReceivedDate,
                            CustomerID = doc.CustomerId,
                            CustomerName = cust != null ? cust.CustomerName : "",
                            CustomerAddress = cust != null ? cust.CustomerAddress : "",
                            Invoice = doc.InvoiceNo,
                            BENo = doc.Beno,
                            ShipmentModeID = doc.ExpenseTypeId,
                            ShipmentMode = exp != null ? exp.ExpenseType : ""
                        };

            int a = await query.CountAsync();

            // Filtering with Customer ID
            if (!string.IsNullOrEmpty(customerid))
            {
                query = query.Where(x => x.CustomerID == customerid);
            }

            if (!string.IsNullOrEmpty(shipmentmodeid))
            {
                query = query.Where(x => x.ShipmentModeID == shipmentmodeid);
            }

            if (request.PageSize == -1)
            {
                request.PageSize = await query.CountAsync();
                request.PageNumber = 1;
            }

            if (string.IsNullOrEmpty(request.SortColumn))
                request.SortColumn = nameof(OFRAdjustApprovalTopGridVM.JobDate);
            if (string.IsNullOrEmpty(request.SortOrder))
                request.SortOrder = "desc";

            var result = await PaginationService<OFRAdjustApprovalTopGridVM>.GetPaginatedData(query, request);
            result.TotalCount = a;
            return result;
        }
        #endregion
        #endregion

        #region When User Checked on radio then fill input fields and Details data Copied to Tmp Details
        #region Get Requisition Info in Input fields
        public async Task<OFRAdjustApprovalMasterVM> GetRequisitionMasterByJobNo(string jobNo)
        {
            var query = from ofr in _repository.All()
                        join doc in _documentation.All() on ofr.JobNo equals doc.JobNo
                        join cust in _customer.All() on doc.CustomerId equals cust.CustomerId into custGroup
                        from cust in custGroup.DefaultIfEmpty()
                        join expType in _shipmentmode.All() on ofr.ExpenseTypeId equals expType.ExpenseTypeId into expTypeGroup
                        from expType in expTypeGroup.DefaultIfEmpty()
                        where ofr.JobNo == jobNo
                        select new OFRAdjustApprovalMasterVM
                        {
                            OFRNo = ofr.Ofrno,
                            JobNo = ofr.JobNo,
                            OFRDate = ofr.Ofrdate,
                            CustomerID = cust != null ? cust.CustomerId : "",
                            CustomerName = cust != null ? cust.CustomerName : "",
                            InvoiceNo = doc.InvoiceNo,
                            Quantity = doc.Quntity1,
                            ShipmentModeID = expType != null ? expType.ExpenseTypeId : "",
                            ShipmentMode = expType != null ? expType.ExpenseType : "",
                            LCValue = doc.Lcvalue,
                            MaterialDescription = doc.MaterialDescription,
                            Weight = doc.Quntity2
                        };

            return await query.FirstOrDefaultAsync();
        }

        public async Task<OFRAdjustApprovalMasterVM> GetRequisitionMasterByOFRNo(string ofrNo)
        {
            var query = from ofr in _repository.All()
                        join doc in _documentation.All() on ofr.JobNo equals doc.JobNo
                        join cust in _customer.All() on doc.CustomerId equals cust.CustomerId into custGroup
                        from cust in custGroup.DefaultIfEmpty()
                        join expType in _shipmentmode.All() on ofr.ExpenseTypeId equals expType.ExpenseTypeId into expTypeGroup
                        from expType in expTypeGroup.DefaultIfEmpty()
                        where ofr.Ofrno == ofrNo
                        select new OFRAdjustApprovalMasterVM
                        {
                            OFRNo = ofr.Ofrno,
                            JobNo = ofr.JobNo,
                            OFRDate = ofr.Ofrdate,
                            CustomerID = cust != null ? cust.CustomerId : "",
                            CustomerName = cust != null ? cust.CustomerName : "",
                            InvoiceNo = doc.InvoiceNo,
                            Quantity = doc.Quntity1,
                            ShipmentModeID = expType != null ? expType.ExpenseTypeId : "",
                            ShipmentMode = expType != null ? expType.ExpenseType : "",
                            LCValue = doc.Lcvalue,
                            MaterialDescription = doc.MaterialDescription,
                            Weight = doc.Quntity2
                        };

            return await query.FirstOrDefaultAsync();
        }
        #endregion

        #region Get Requisition Details into Tmp Details
        public async Task<bool> CopyDetailsToTmp(string jobNo, string? currentUserId, BaseViewModel model)
        {
            var details = await _details.All().Where(d => d.JobNo == jobNo).ToListAsync();

            if (!details.Any()) return false;

            // Map to temporary table (use the Temp model)
            var tmpDetails = details.Select(d => new CfOperationalFundRequisitionDetailsTemp
            {
                OfrDetailsId = d.OfrDetailsId,
                Ofrno = d.Ofrno,
                JobNo = d.JobNo,
                ExpenseHeadId = d.ExpenseHeadId,
                ServiceTypeId = d.ServiceTypeId,
                EstimatedAmount = d.EstimatedAmount,
                ActualAmount = d.ActualAmount,
                DifferentAmount = d.DifferentAmount,
                BillAdjustAmount = d.BillAdjustAmount,
                Luser = currentUserId,
                Ldate = DateTime.Now,
                Lmac = model.Lmac,
                Lip = model.Lip,
                CompanyCode = d.CompanyCode,
                EmployeeId = currentUserId,
                ExpenseHeadRemarks = d.ExpenseHeadRemarks
            }).ToList();

            if (tmpDetails == null || !tmpDetails.Any())
            {
                throw new InvalidOperationException("Failed to map details to temporary table");
            }
            await _Tmpdetails.AddRangeAsync(tmpDetails);
            return true;
        }

        public async Task<bool> CopyDetailsToTmpByOFRNo(string ofrNo, string? currentUserId, BaseViewModel model)
        {
            var details = await _details.All().Where(d => d.Ofrno == ofrNo).ToListAsync();

            if (!details.Any()) return false;

            var existingTmp = await _Tmpdetails.All()
                .Where(x => x.Luser == currentUserId)
                .ToListAsync();

            if (existingTmp.Any())
                await _Tmpdetails.DeleteRangeAsync(existingTmp);

            var tmpDetails = details.Select(d => new CfOperationalFundRequisitionDetailsTemp
            {
                OfrDetailsId = d.OfrDetailsId,
                Ofrno = d.Ofrno,
                JobNo = d.JobNo,
                ExpenseHeadId = d.ExpenseHeadId,
                ServiceTypeId = d.ServiceTypeId,
                EstimatedAmount = d.EstimatedAmount,
                ActualAmount = d.ActualAmount,
                DifferentAmount = d.DifferentAmount,
                BillAdjustAmount = d.BillAdjustAmount,
                Luser = currentUserId,
                Ldate = DateTime.Now,
                Lmac = model.Lmac,
                Lip = model.Lip,
                CompanyCode = d.CompanyCode,
                EmployeeId = currentUserId,
                ExpenseHeadRemarks = d.ExpenseHeadRemarks
            }).ToList();

            if (tmpDetails == null || !tmpDetails.Any())
            {
                throw new InvalidOperationException("Failed to map details to temporary table");
            }
            await _Tmpdetails.AddRangeAsync(tmpDetails);
            return true;
        }
        #endregion
        #endregion

        #region Get All Tmp Details
        public async Task<List<OFRAdjustApprovalDetailsVM>> GetDetailsWithCashBank(string jobno, string? userId)
        {
            var result = await (from tmp in _Tmpdetails.All()
                                join head in _accountsHead.All()
                                    on tmp.ExpenseHeadId equals head.ExpenseHeadId
                                join serv in _serviceType.All()
                                    on tmp.ServiceTypeId equals serv.ServiceTypeId
                                join ofrDet in _details.All()   // Original table for Cash/Bank
                                    on tmp.OfrDetailsId equals ofrDet.OfrDetailsId
                                where tmp.Luser == userId && tmp.JobNo == jobno
                                orderby tmp.Tc
                                select new OFRAdjustApprovalDetailsVM
                                {
                                    Tc = tmp.Tc,
                                    OFRDetailsID = tmp.OfrDetailsId,
                                    SerialNo = head.SerialNo,
                                    AccountHeadName = head.ExpenseHead,
                                    IsReceivetable = head.IsReceiptable,
                                    ServiceTypeName = serv.ServiceTypeName,
                                    Amount = tmp.EstimatedAmount,
                                    ApprovalAmount = tmp.ActualAmount,
                                    AdjustAmount = tmp.BillAdjustAmount,
                                    DiffentAmount = tmp.DifferentAmount,
                                    Remark = tmp.ExpenseHeadRemarks,
                                    CashBank = ofrDet.AdjustDetailsCashBank,
                                    BankAccount = ofrDet.AdjustDetailsBankAccount
                                }).ToListAsync();

            return result;
        }
        #endregion

        #region Bank Account Dropdown
        public async Task<IEnumerable<SelectListItem>> GetBankAccount(string value)
        {
            if (value == "Employee" || value == "10200300004")
            {
                return await _generalLedger.All()
                    .Where(e => e.SubsidiaryLedgerCodeNo == "10200300004" && e.IsActive == "Y")
                    .Select(e => new SelectListItem
                    {
                        Value = e.SubSusidiaryLedgerCodeNo.ToString(),
                        Text = e.SubSubsidiaryLedgerName
                    }).ToListAsync();
            }

            var query = from a in _generalLedger.All()
                        join b in _bankAccountInfo.All()
                            on a.SubSubsidiaryLedgerName equals b.AccountNo
                        where a.SubsidiaryLedgerCodeNo == value || (value == "Bank" && a.SubsidiaryLedgerCodeNo == "10200400003")
                        select new SelectListItem
                        {
                            Value = a.SubSubsidiaryLedgerName,
                            Text = a.SubSubsidiaryLedgerName + " - " + b.AccountName
                        };

            return await query.ToListAsync();
        }
        #endregion

        #region Approval Adjust Amount Update
        public async Task<bool> SaveAdjustApprovalAsync(List<OFRAdjustApprovalDetailsSaveVM> model, string? Approveduser)
        {
            if (model == null || !model.Any()) return false;

            await _repository.BeginTransactionAsync();

            try
            {
                string ofrNo = model.First().OFRNo;

                // ENTRY (HEADER) UPDATE
                var entry = await _repository.All().FirstOrDefaultAsync(x => x.Ofrno == ofrNo);

                if (entry == null) throw new Exception("OFR Entry not found");

                // Header-level Cash/Bank (first non-empty from details)
                entry.CashBank2 = model.Select(x => x.CashBank).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
                entry.BankAccount2 = model.Select(x => x.BankAcount).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
                entry.BillAdjustApprovalUser = Approveduser;
                entry.BillAdjustApproveDate = DateTime.Now;

                await _repository.UpdateAsync(entry);

                // DETAILS UPDATE (ROW-WISE)
                foreach (var item in model)
                {
                    var details = await _details.All().FirstOrDefaultAsync(x => x.OfrDetailsId == item.OFR_DetailsID && x.Ofrno == ofrNo);

                    if (details == null) throw new Exception($"Details not found: {item.OFR_DetailsID}");

                    details.AdjustApprovalAmount = item.ConfirmAmount;
                    details.DifferentAmount2 = item.DifferentAmount;

                    // Row-wise Cash / Bank
                    details.AdjustDetailsCashBank = item.CashBank;
                    details.AdjustDetailsBankAccount = item.BankAcount;
                    details.AdjustUser = Approveduser;

                    await _details.UpdateAsync(details);
                }

                // TEMP DATA CLEANUP
                var tmpToDelete = await _Tmpdetails.All().Where(x => x.Ofrno == ofrNo && x.Luser == Approveduser).ToListAsync();

                foreach (var tmp in tmpToDelete)
                {
                    await _Tmpdetails.DeleteAsync(tmp.Tc);
                }

                await _repository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                await _repository.RollbackTransactionAsync();
                Console.WriteLine("SaveAdjustApproval Failed: " + ex.Message);
                return false;
            }
        }
        #endregion
    }
}
