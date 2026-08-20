
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.OFRSection.OFRApproval;
using GCTL.Core.ViewModels.OFRSection.OFRBillAdjust;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using Microsoft.EntityFrameworkCore;

using Sep_Documentation = GCTL.Data.Models.SepDocumentation;
using Core_ServiceType = GCTL.Data.Models.CoreServiceType;
using GCTL.Core.Data;
namespace GCTL.Service.OFRBillAdjust
{
    public class OFRBillAdjustService : AppService<CfOperationalFundRequisitionEntry>, IOFRBillAdjust
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


        public OFRBillAdjustService
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

        #region When User Checked on radion then fill input fields and Detaisl data Copied to Tmp Details
        #region Get Requisition Info in Input fields
        public async Task<OFRBillAdjustMasterVM> GetRequisitionMasterByJobNo(string jobNo)
        {
            var query = from ofr in _repository.All()
                        join doc in _documentation.All() on ofr.JobNo equals doc.JobNo
                        join cust in _customer.All() on doc.CustomerId equals cust.CustomerId into custGroup
                        from cust in custGroup.DefaultIfEmpty()
                        join expType in _shipmentmode.All() on ofr.ExpenseTypeId equals expType.ExpenseTypeId into expTypeGroup
                        from expType in expTypeGroup.DefaultIfEmpty()
                        where ofr.JobNo == jobNo
                        select new OFRBillAdjustMasterVM
                        {
                            OFRNo = ofr.Ofrno,
                            JobNo = ofr.JobNo,
                            OFRDate = ofr.Ofrdate,
                            PortCharge = ofr.PortCharge,
                            AsycodaCharge = ofr.AsycodaCharge,
                            DutyChalanCharge = ofr.DutyByChallanCharge,
                            ShipingCharge = ofr.ShippingCharge,
                            BillAdjustModifiedDate = ofr.BillAdjustApproveModifyDate,
                            BillAdjustApprovedDate = ofr.BillAdjustApproveDate,
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
        public async Task<List<OFRBillAdjustDetailsVM>> GetAllTmpAsync(int? currentUser)
        {
            var result = await (from tmp in _Tmpdetails.All()
                                join head in _accountsHead.All()
                                    on tmp.ExpenseHeadId equals head.ExpenseHeadId
                                    into headJoin
                                from head in headJoin.DefaultIfEmpty()
                                join serv in _serviceType.All()
                              on tmp.ServiceTypeId equals serv.ServiceTypeId
                              into servicJoin
                                from servic in servicJoin.DefaultIfEmpty()
                                where tmp.Luser == currentUser.ToString()
                                orderby tmp.Tc

                                select new OFRBillAdjustDetailsVM
                                {
                                    Tc = tmp.Tc,
                                    OFRDetailsID = tmp.OfrDetailsId,
                                    SerialNo = head.SerialNo,
                                    AccountHeadName = head.ExpenseHead,
                                    IsReceivetable = head.IsReceiptable,
                                    ServiceTypeName = servic.ServiceTypeName,
                                    Amount = tmp.EstimatedAmount,
                                    ApprovalAmount = tmp.ActualAmount,
                                    AdjustAmount = tmp.BillAdjustAmount,
                                    DiffentAmount = tmp.DifferentAmount,
                                    Remark = tmp.ExpenseHeadRemarks
                                }).ToListAsync();

            return result;
        }

        #endregion

        #region Approval Bill Adjust Amount Update
        public async Task<bool> SaveApprovalBillAdjustAsync(List<OFRBillAdjustDetailsSaveVM> model, int? Approveduser)
        {
            await _repository.BeginTransactionAsync();
            try
            {
                if (model == null || !model.Any()) return false;

                var ofrNo = model.First().OFRNo;

                // ENTRY (HEADER)
                var entry = await _repository.All().FirstOrDefaultAsync(x => x.Ofrno == ofrNo);

                bool isFirstApproval = false;

                // DETAILS
                foreach (var item in model)
                {
                    var details = await _details.All().FirstOrDefaultAsync(x => x.OfrDetailsId == item.OFR_DetailsID);

                    if (details != null)
                    {
                        //Check first time approval
                        if (details.BillAdjustAmount == 0.00m)
                        {
                            isFirstApproval = true;
                        }

                        details.BillAdjustAmount = item.AdjustAmount;
                        details.DifferentAmount = item.DifferentAmount;

                        await _details.UpdateAsync(details);
                    }
                }

                // ENTRY UPDATE
                if (entry != null)
                {
                    var firstItem = model.First();
                    entry.PortCharge = firstItem.PortCharge;
                    entry.AsycodaCharge = firstItem.AsycodaCharge;
                    entry.DutyByChallanCharge = firstItem.DutyChalanCharge;
                    entry.ShippingCharge = firstItem.ShipingCharge;

                    if (isFirstApproval && entry.BillAdjustApproveDate == null)
                    {

                        //First time approve
                        entry.BillAdjustApprovalUser = Approveduser.ToString();
                        entry.BillAdjustApproveDate = DateTime.Now;
                    }
                    else
                    {
                        //Modify approve
                        entry.BillAdjustApproveModifyDate = DateTime.Now;
                    }

                    await _repository.UpdateAsync(entry);
                }

                var tmpToDelete = await _Tmpdetails.All().Where(d => d.Luser == Approveduser.ToString() && d.Ofrno == ofrNo).ToListAsync();

                if (tmpToDelete.Any())
                {
                    foreach (var tmp in tmpToDelete)
                    {
                        await _Tmpdetails.DeleteAsync(tmp.Tc);
                    }
                }
                await _repository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                await _repository.RollbackTransactionAsync();
                Console.WriteLine($"Insertion Failed: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Get All Requisition for Bottom Grid
        public async Task<PaginationService<OFRBillAdjustBottomGridVM, OFRBillAdjustBottomGridVM>.PaginationResult<OFRBillAdjustBottomGridVM>> GetBottomGrid(int pageNumber = 1, int pageSize = 5, string searchTerm = "", string sortColumn = "JobNo", string sortOrder = "desc")
        {
            var query = from main in _repository.All()
                        join doc in _documentation.All()
                            on main.JobNo equals doc.JobNo
                        join cust in _customer.All()
                            on doc.CustomerId equals cust.CustomerId
                        join exp in _shipmentmode.All()
                            on main.ExpenseTypeId equals exp.ExpenseTypeId
                        where _details.All().Where(det => det.Ofrno == main.Ofrno)
                                .All(det => string.IsNullOrWhiteSpace(det.AdjustUser))
                        //where _details.All().Any(det => det.OFRNo == main.OFRNo
                        //                        && !string.IsNullOrWhiteSpace(det.AdjustUser))
                        select new OFRBillAdjustBottomGridVM
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
            var paginatedResult = await PaginationService<OFRBillAdjustBottomGridVM, OFRBillAdjustBottomGridVM>.GetPaginatedData(query, pageNumber, pageSize, searchTerm, sortColumn, sortOrder,
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
        public async Task<PaginationService<OFRApprovalTopGridVM, OFRApprovalTopGridVM>.PaginationResult<OFRApprovalTopGridVM>> GetAllRequisition(int pageNumber = 1, int pageSize = 5, string searchTerm = "", string sortColumn = "JobNo", string sortOrder = "desc", string customerid = "", string shipmentmodeid = "")
        {
            var query = from main in _repository.All()
                        join doc in _documentation.All()
                            on main.JobNo equals doc.JobNo
                        join cust in _customer.All()
                            on doc.CustomerId equals cust.CustomerId
                        join exp in _shipmentmode.All()
                            on main.ExpenseTypeId equals exp.ExpenseTypeId
                        where _details.All().Any(det => det.Ofrno == main.Ofrno
                                                    && string.IsNullOrWhiteSpace(det.AdjustUser))
                        select new OFRApprovalTopGridVM
                        {
                            TC = main.Tc,
                            JobNo = main.JobNo,
                            CustomerID = cust.CustomerId,
                            CustomerName = cust.CustomerName,
                            ShipmentModeID = exp.ExpenseTypeId,
                            ShipmentMode = exp.ExpenseType
                        };
            //Filtering with Customer ID
            int totalCount = 0;
            totalCount = await query.CountAsync();
            bool changed = false;

            if (!string.IsNullOrEmpty(customerid))
            {
                query = query.Where(x => x.CustomerID == customerid);
                changed = true;
            }

            if (!string.IsNullOrEmpty(shipmentmodeid))
            {
                query = query.Where(x => x.ShipmentModeID == shipmentmodeid);
                changed = true;
            }

            if (pageSize == -1)
            {
                pageSize = await query.CountAsync();
                pageNumber = 1;
            }

            // PaginationService
            var paginatedResult = await PaginationService<OFRApprovalTopGridVM, OFRApprovalTopGridVM>.GetPaginatedData(query, pageNumber, pageSize, searchTerm, sortColumn, sortOrder,
                    term => sc =>
                        EF.Functions.Like(sc.JobNo ?? "", $"%{term}%") ||
                        EF.Functions.Like(sc.CustomerName ?? "", $"%{term}%") ||
                        EF.Functions.Like(sc.ShipmentMode ?? "", $"%{term}%"),
                    ob => ob
             );

            if (changed)
            {
                paginatedResult.TotalCount = totalCount;
            }

            return paginatedResult;
        }
        #endregion

        #region When click Edit Button

        #region Getting Master Data
        //public async Task<OFRBillAdjustMasterVM> GetRequisitionInputData(string jobNo)
        //{
        //    var data = await (from req in _repository.All()
        //                      join det in _details.All()
        //                       on req.OFRNo equals det.OFRNo into detGroup
        //                      from det in detGroup.DefaultIfEmpty()  // left join with details

        //                      join doc in _sepDocumentation.All()
        //                        on req.JobNo equals doc.JobNo into docGroup
        //                      from doc in docGroup.DefaultIfEmpty()  // left join with documentation

        //                      join st in _serviceType.All()
        //                        on det.ServiceTypeID equals st.ServiceTypeID into stGroup
        //                      from st in stGroup.DefaultIfEmpty()   // ServiceType lookup

        //                      join eh in _accountsHead.All()
        //                        on det.ExpenseHeadID equals eh.ExpenseHeadID into ehGroup
        //                      from eh in ehGroup.DefaultIfEmpty()   // ExpenseHead lookup


        //                      join cust in _customer.All()
        //                        on doc.CustomerID equals cust.CustomerID into custGroup
        //                      from cust in custGroup.DefaultIfEmpty()   // Customer

        //                      join ship in _shipmentmode.All()
        //                        on req.ExpenseTypeID equals ship.ExpenseTypeID into shipGroup
        //                      from ship in shipGroup.DefaultIfEmpty() //ShipmentMode

        //                      where req.JobNo == jobNo
        //                      select new OFRBillAdjustMasterVM
        //                      {
        //                          OFRNo = req.OFRNo,
        //                          OFRDate = req.OFRDate,
        //                          JobNo = req.JobNo,
        //                          Por
        //                          ShipmentMode = ship.ExpenseType,
        //                          CustomerName = cust.CustomerName,
        //                          LCValue = doc.LCValue,
        //                          InvoiceNo = doc.InvoiceNo,
        //                          MaterialDescription = doc.MaterialDescription,
        //                          Quantity = doc.Quntity1,
        //                          Weight = doc.Quntity2

        //                      }).FirstOrDefaultAsync();

        //    return data;
        //}
        #endregion

        #region Copied To Tmp Details Table
        //public async Task<bool> CopyDetailsToTmpTable(string jobNo, int? currentUserId, BaseViewModel model)
        //{
        //    var details = await _details.All().Where(d => d.JobNo == jobNo).ToListAsync();

        //    if (!details.Any()) return false;

        //    // Map to temporary table (use the Temp model)
        //    var tmpDetails = details.Select(d => new CF_OperationalFundRequisitionDetailsTemp
        //    {
        //        OFR_DetailsID = d.OFR_DetailsID,
        //        OFRNo = d.OFRNo,
        //        JobNo = d.JobNo,
        //        ExpenseHeadID = d.ExpenseHeadID,
        //        ServiceTypeID = d.ServiceTypeID,
        //        EstimatedAmount = d.EstimatedAmount,
        //        ActualAmount = d.ActualAmount,
        //        DifferentAmount = d.DifferentAmount,
        //        DifferentAmount2 = d.DifferentAmount2,
        //        BillAdjustAmount = d.BillAdjustAmount,
        //        LUser = currentUserId.ToString(),
        //        LDate = DateTime.Now,
        //        LIP = model.LIP,
        //        LMAC = model.LMAC,
        //        CompanyCode = d.CompanyCode,
        //        EmployeeID = d.EmployeeID,
        //        ExpenseHeadRemarks = d.ExpenseHeadRemarks
        //    }).ToList();

        //    await _Tmpdetails.AddRangeAsync(tmpDetails);
        //    return true;
        //}
        #endregion

        #endregion
    }
}
