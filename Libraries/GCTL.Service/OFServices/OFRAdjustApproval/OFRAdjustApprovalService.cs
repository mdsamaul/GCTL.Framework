
using GCTL.Core.Data;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.OFRSection.OFRAdjustApproval;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using Microsoft.EntityFrameworkCore;

using Sep_Documentation = GCTL.Data.Models.SepDocumentation;
using Core_ServiceType = GCTL.Data.Models.CoreServiceType;

namespace GCTL.Service.OFRAdjustApproval
{
    public class OFRAdjustApprovalService : AppService<CfOperationalFundRequisitionEntry>, IOFRAdjustApproval
    {
        #region Service
        //private readonly IUserInfoService _userInfoService;
        private readonly IRepository<CfOperationalFundRequisitionEntry> _repository;
        private readonly IRepository<CfOperationalFundRequisitionDetail> _details;
        private readonly IRepository<CfOperationalFundRequisitionDetailsTemp> _Tmpdetails;
        private readonly IRepository<CfDefExpenseType> _shipmentmode;
        private readonly IRepository<SalesCustomer> _customer;
        private readonly IRepository<Sep_Documentation> _documentation;
        private readonly IRepository<CfDefExpenseHead> _accountsHead;
        private readonly IRepository<Core_ServiceType> _serviceType;
        private readonly IRepository<AccSubSubsidiaryLedger> _generalLedger;
        private readonly IRepository<Sep_Documentation> _sepDocumentation;


        public OFRAdjustApprovalService
            (
                IRepository<CfOperationalFundRequisitionEntry> genericRepository,
                //IUserInfoService userInfoService,
                IRepository<CfOperationalFundRequisitionEntry> repository,
                IRepository<CfDefExpenseType> shipmentmode, 
                IRepository<SalesCustomer> customer,
                IRepository<Sep_Documentation> documentation,
                IRepository<CfOperationalFundRequisitionDetail> details, 
                IRepository<CfDefExpenseHead> accountsHead,
                IRepository<Core_ServiceType> serviceType,
                IRepository<CfOperationalFundRequisitionDetailsTemp> tmpdetails,
                IRepository<AccSubSubsidiaryLedger> generalLedger, 
                IRepository<Sep_Documentation> sepDocumentation
            ) : base(genericRepository)
        {
            //_userInfoService = userInfoService;
            _repository = repository;
            _shipmentmode = shipmentmode;
            _customer = customer;
            _documentation = documentation;
            _details = details;
            _accountsHead = accountsHead;
            _serviceType = serviceType;
            _Tmpdetails = tmpdetails;
            _generalLedger = generalLedger;
            _sepDocumentation = sepDocumentation;
        }
        #endregion

        #region Grid Section

        #region Get All Requisition for Bottom Grid
        public async Task<PaginationService<OFRAdjustApprovalBottomGridVM, OFRAdjustApprovalBottomGridVM>.PaginationResult<OFRAdjustApprovalBottomGridVM>> AllRequisitionforBottomGrid(int pageNumber = 1, int pageSize = 5, string searchTerm = "", string sortColumn = "JobNo", string sortOrder = "desc")
        {
            var query = from main in _repository.All()
                        join doc in _documentation.All()
                            on main.JobNo equals doc.JobNo
                        join cust in _customer.All()
                            on doc.CustomerId equals cust.CustomerId
                        join exp in _shipmentmode.All()
                            on main.ExpenseTypeId equals exp.ExpenseTypeId
                        select new OFRAdjustApprovalBottomGridVM
                        {
                            TC = main.Tc,
                            JobNo = main.JobNo,
                            CustomerID = cust.CustomerId,
                            CustomerName = cust.CustomerName,
                            ShipmentModeID = exp.ExpenseTypeId,
                            ShipmentMode = exp.ExpenseType
                        };

            if (pageSize == -1)
            {
                pageSize = await query.CountAsync();
                pageNumber = 1;
            }

            // PaginationService
            var paginatedResult = await PaginationService<OFRAdjustApprovalBottomGridVM, OFRAdjustApprovalBottomGridVM>.GetPaginatedData(query, pageNumber, pageSize, searchTerm, sortColumn, sortOrder,
                    term => sc =>
                        EF.Functions.Like(sc.JobNo ?? "", $"%{term}%") ||
                        EF.Functions.Like(sc.CustomerName ?? "", $"%{term}%") ||
                        EF.Functions.Like(sc.ShipmentMode ?? "", $"%{term}%"),
                    ob => ob
             );

            return paginatedResult;
        }
        #endregion

        #region Get All Requisition Entry in Top Grid
        public async Task<PaginationService<OFRAdjustApprovalTopGridVM, OFRAdjustApprovalTopGridVM>.PaginationResult<OFRAdjustApprovalTopGridVM>> GetAllRequisition(int pageNumber = 1, int pageSize = 5, string searchTerm = "", string sortColumn = "JobNo", string sortOrder = "desc", string customerid = "", string shipmentmodeid = "")
        {
            var query = from main in _repository.All()
                        join doc in _documentation.All()
                            on main.JobNo equals doc.JobNo
                        join cust in _customer.All()
                            on doc.CustomerId equals cust.CustomerId
                        join exp in _shipmentmode.All()
                            on main.ExpenseTypeId equals exp.ExpenseTypeId
                        select new OFRAdjustApprovalTopGridVM
                        {
                            TC = main.Tc,
                            JobNo = main.JobNo,
                            CustomerID = cust.CustomerId,
                            CustomerName = cust.CustomerName,
                            ShipmentModeID = exp.ExpenseTypeId,
                            ShipmentMode = exp.ExpenseType
                        };
            //Filtering with Customer ID
            if (!string.IsNullOrEmpty(customerid))
            {
                query = query.Where(x => x.CustomerID == customerid);
            }

            if (!string.IsNullOrEmpty(shipmentmodeid))
            {
                query = query.Where(x => x.ShipmentModeID == shipmentmodeid);
            }
            if (pageSize == -1)
            {
                pageSize = await query.CountAsync();
                pageNumber = 1;
            }

            // PaginationService
            var paginatedResult = await PaginationService<OFRAdjustApprovalTopGridVM, OFRAdjustApprovalTopGridVM>.GetPaginatedData(query, pageNumber, pageSize, searchTerm, sortColumn, sortOrder,
                    term => sc =>
                        EF.Functions.Like(sc.JobNo ?? "", $"%{term}%") ||
                        EF.Functions.Like(sc.CustomerName ?? "", $"%{term}%") ||
                        EF.Functions.Like(sc.ShipmentMode ?? "", $"%{term}%"),
                    ob => ob
             );

            return paginatedResult;
        }
        #endregion
        #endregion

        #region When User Checked on radion then fill input fields and Detaisl data Copied to Tmp Details
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
                            CustomerID = cust.CustomerId,
                            CustomerName = cust.CustomerName ?? "",
                            InvoiceNo = doc.InvoiceNo,
                            Quantity = doc.Quntity1,
                            ShipmentModeID = expType.ExpenseTypeId,
                            ShipmentMode = expType.ExpenseType,
                            LCValue = doc.Lcvalue,
                            MaterialDescription = doc.MaterialDescription,
                            Weight = doc.Quntity2
                        };

            return await query.FirstOrDefaultAsync();
        }
        #endregion

        #region Get Requisition Details into Tmp Details
        public async Task<bool> CopyDetailsToTmp(string jobNo, int? currentUserId, BaseViewModel model)
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
                Luser = currentUserId?.ToString(),
                Ldate = DateTime.Now,
                Lmac = model.Lmac,
                Lip = model.Lip,
                CompanyCode = d.CompanyCode,
                EmployeeId = currentUserId.ToString(),
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
        public async Task<List<OFRAdjustApprovalDetailsVM>> GetDetailsWithCashBank(string jobno, int? userId)
        {
            var result = await (from tmp in _Tmpdetails.All()
                                join head in _accountsHead.All()
                                    on tmp.ExpenseHeadId equals head.ExpenseHeadId
                                join serv in _serviceType.All()
                                    on tmp.ServiceTypeId equals serv.ServiceTypeId
                                join ofrDet in _details.All()   // Original table for Cash/Bank
                                    on tmp.OfrDetailsId equals ofrDet.OfrDetailsId
                                where tmp.Luser == userId.ToString() && tmp.JobNo == jobno
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

        //public async Task<List<OFRAdjustApprovalDetailsVM>> GetAllTmpAsync(int? currentUser)
        //{
        //    var result = await (from tmp in _Tmpdetails.All()
        //                        join head in _accountsHead.All()
        //                            on tmp.ExpenseHeadID equals head.ExpenseHeadID
        //                            into headJoin
        //                        from head in headJoin.DefaultIfEmpty()
        //                        join serv in _serviceType.All()
        //                      on tmp.ServiceTypeID equals serv.ServiceTypeID
        //                      into servicJoin
        //                        from servic in servicJoin.DefaultIfEmpty()
        //                        where tmp.LUser == currentUser.ToString()
        //                        orderby tmp.TC

        //                        select new OFRAdjustApprovalDetailsVM
        //                        {
        //                            Tc = tmp.TC,
        //                            OFRDetailsID = tmp.OFR_DetailsID,
        //                            SerialNo = head.SerialNo,
        //                            AccountHeadName = head.ExpenseHead,
        //                            IsReceivetable = head.IsReceiptable,
        //                            ServiceTypeName = servic.ServiceTypeName,
        //                            Amount = tmp.EstimatedAmount,
        //                            ApprovalAmount = tmp.ActualAmount,
        //                            AdjustAmount = tmp.BillAdjustAmount,
        //                            DiffentAmount = tmp.DifferentAmount,
        //                            Remark = tmp.ExpenseHeadRemarks
        //                        }).ToListAsync();

        //    return result;
        //}

        #endregion

        #region Approval  Adjust Amount Update
        public async Task<bool> SaveAdjustApprovalAsync(List<OFRAdjustApprovalDetailsSaveVM> model, int? Approveduser)
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

                await _repository.UpdateAsync(entry);

                //DETAILS UPDATE (ROW-WISE)
                foreach (var item in model)
                {
                    var details = await _details.All().FirstOrDefaultAsync(x => x.OfrDetailsId == item.OFR_DetailsID && x.Ofrno == ofrNo);

                    if (details == null) throw new Exception($"Details not found: {item.OFR_DetailsID}");

                    details.AdjustApprovalAmount = item.ConfirmAmount;
                    details.DifferentAmount2 = item.DifferentAmount;

                    // Row-wise Cash / Bank
                    details.AdjustDetailsCashBank = item.CashBank;
                    details.AdjustDetailsBankAccount = item.BankAcount;

                    await _details.UpdateAsync(details);
                }

                //TEMP DATA CLEANUP
                var tmpToDelete = await _Tmpdetails.All().Where(x => x.Ofrno == ofrNo && x.Luser == Approveduser.ToString()).ToListAsync();

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
