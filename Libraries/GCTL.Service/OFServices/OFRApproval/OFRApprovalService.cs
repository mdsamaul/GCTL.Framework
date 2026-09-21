
using Azure.Core;
using GCTL.Core.Data;
using GCTL.Core.DataTables;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.OFRSection.OFRApproval;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Core_ServiceType = GCTL.Data.Models.CoreServiceType;
using Sep_Documentation = GCTL.Data.Models.SepDocumentation;

namespace GCTL.Service.OFRApproval
{
    public class OFRApprovalService : AppService<CfOperationalFundRequisitionEntry>, IOFRApproval
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
        private readonly IRepository<CoreBankAccountInformation> _bankAccountInfo;
        private readonly IRepository<Sep_Documentation> _sepDocumentation;


        public OFRApprovalService(
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
                IRepository<CoreBankAccountInformation> bankAccountInfo,
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
            _bankAccountInfo = bankAccountInfo;
            _sepDocumentation = sepDocumentation;
        }
        #endregion

        #region Get All Requisition Entry in Top Grid
        public async Task<PaginationResponse<OFRApprovalTopGridVM>> GetAllRequisition(PaginationRequest request, string customerid = "", string shipmentmodeid = "")
        {
            var query = (from main in _repository.All().AsNoTracking()
                        join doc in _documentation.All().AsNoTracking()
                            on main.JobNo equals doc.JobNo
                        join cust in _customer.All().AsNoTracking()
                            on doc.CustomerId equals cust.CustomerId into cusG
                        from cust in cusG.DefaultIfEmpty()
                        join exp in _shipmentmode.All().AsNoTracking()
                            on main.ExpenseTypeId equals exp.ExpenseTypeId into shipG
                        from exp in shipG.DefaultIfEmpty()
                        where string.IsNullOrEmpty(main.ApprovedUser)
                        select new OFRApprovalTopGridVM
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
                        }).AsNoTracking();

            //Filtering with Customer ID
            if (!string.IsNullOrEmpty(customerid))
            {
                query = query.Where(x => x.CustomerID == customerid);
            }

            if (!string.IsNullOrEmpty(shipmentmodeid))
            {
                query = query.Where(x => x.ShipmentModeID == shipmentmodeid);
            }

            if (string.IsNullOrEmpty(request.SortColumn))
                request.SortColumn = nameof(OFRApprovalTopGridVM.JobDate);
            if (string.IsNullOrEmpty(request.SortOrder))
                request.SortOrder = "desc";

            return await PaginationService<OFRApprovalTopGridVM>.GetPaginatedData(query, request);
        }
        #endregion

        #region When User Checked on radion then fill input fields and Detaisl data Copied to Tmp Details
        #region Get Requisition Info in Input fields
        public async Task<OFRApprovalMasterVM> GetRequisitionMasterByJobNo(string jobNo)
        {
            var query = from ofr in _repository.All()
                        join doc in _documentation.All() on ofr.JobNo equals doc.JobNo
                        join cust in _customer.All() on doc.CustomerId equals cust.CustomerId into custGroup
                        from cust in custGroup.DefaultIfEmpty()
                        join expType in _shipmentmode.All() on ofr.ExpenseTypeId equals expType.ExpenseTypeId into expTypeGroup
                        from expType in expTypeGroup.DefaultIfEmpty()
                        where ofr.JobNo == jobNo
                        select new OFRApprovalMasterVM
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

        #region Get Requisition Details in Expense Details
        public async Task<bool> CopyDetailsToTmp(string jobNo, string? currentUserId)
        {
            if (string.IsNullOrEmpty(currentUserId)) return false;

            var details = await _details.All().Where(d => d.JobNo == jobNo).ToListAsync();
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
                DifferentAmount2 = d.DifferentAmount2,
                BillAdjustAmount = d.BillAdjustAmount,
                Luser = currentUserId,
                Ldate = DateTime.Now,
                Lip = d.Lip,
                Lmac = d.Lmac,
                CompanyCode = d.CompanyCode,
                EmployeeId = d.EmployeeId,
                ExpenseHeadRemarks = d.ExpenseHeadRemarks
            }).ToList();

            await _Tmpdetails.AddRangeAsync(tmpDetails);
            return true;
        }
        #endregion
        #endregion

        #region Get All Tmp Details
        public async Task<List<OFRApprovalDetailsVM>> GetAllTmpAsync(string jobno, string? userId)
        {
            var result = await (from tmp in _Tmpdetails.All()
                                join head in _accountsHead.All()
                                    on tmp.ExpenseHeadId equals head.ExpenseHeadId into headGroup
                                from head in headGroup.DefaultIfEmpty()
                                join serv in _serviceType.All()
                                    on tmp.ServiceTypeId equals serv.ServiceTypeId into servGroup
                                from serv in servGroup.DefaultIfEmpty()
                                join ofrDet in _details.All()  
                                    on tmp.OfrDetailsId equals ofrDet.OfrDetailsId into ofrGroup
                                from ofrDet in ofrGroup.DefaultIfEmpty()
                                where tmp.Luser == userId && tmp.JobNo == jobno
                                orderby tmp.Tc
                                select new OFRApprovalDetailsVM
                                {
                                    Tc = tmp.Tc,
                                    OFRDetailsID = tmp.OfrDetailsId,
                                    SerialNo = head != null ? head.SerialNo : "",
                                    AccountHeadName = head != null ? head.ExpenseHead : "",
                                    IsReceivetable = head != null ? head.IsReceiptable : "",
                                    ServiceTypeName = serv != null ? serv.ServiceTypeName : "",
                                    Amount = tmp.EstimatedAmount,
                                    ApprovalAmount = tmp.ActualAmount,
                                    Remark = tmp.ExpenseHeadRemarks,
                                    CashBank = ofrDet != null ? ofrDet.DetailsCashBank : "",
                                    BankAccount = ofrDet != null ? ofrDet.DetailsBankAccount : ""
                                }).ToListAsync();

            return result;
        }
        //public async Task<List<OFRApprovalDetailsVM>> GetAllTmpAsync(int? currentUser)
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

        //                        select new OFRApprovalDetailsVM
        //                        {
        //                            Tc = tmp.TC,
        //                            OFRDetailsID = tmp.OFR_DetailsID,
        //                            SerialNo = head.SerialNo,
        //                            AccountHeadName = head.ExpenseHead,
        //                            IsReceivetable = head.IsReceiptable,
        //                            ServiceTypeName = servic.ServiceTypeName,
        //                            Amount = tmp.EstimatedAmount,
        //                            ApprovalAmount = tmp.ActualAmount,
        //                            Remark = tmp.ExpenseHeadRemarks
        //                        }).ToListAsync();

        //    return result;
        //}

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

        #region Approval Amount Update
        public async Task<bool> SaveApprovalAsync(List<OFRApprovalDetailsSaveVM> model, string? Approveduser)
        {
            await _repository.BeginTransactionAsync();
            try
            {
                if (model == null || !model.Any()) return false;

                var ofrNo = model.First().OFRNo;

                // ENTRY (HEADER)
                var entry = await _repository.All().FirstOrDefaultAsync(x => x.Ofrno == ofrNo);

                entry.CashBank = model.Select(x => x.DetailsCashBank).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

                entry.BankAccount = model.Select(x => x.DetailsBankAccount).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
                bool isFirstApproval = false;

                // DETAILS
                foreach (var item in model)
                {
                    var details = await _details.All().FirstOrDefaultAsync(x => x.OfrDetailsId == item.OFR_DetailsID);

                    if (details != null)
                    {
                        //Check first time approval
                        if (details.ActualAmount == 0.00m)
                        {
                            isFirstApproval = true;
                        }

                        details.ActualAmount = item.ActualAmount;
                        details.DetailsCashBank = item.DetailsCashBank;
                        details.DetailsBankAccount = item.DetailsBankAccount;

                        await _details.UpdateAsync(details);
                    }
                }

                // ENTRY UPDATE
                if (entry != null)
                {
                    if (isFirstApproval && entry.ApprovedDate == null)
                    {
                        //First time approve
                        entry.ApprovedUser = Approveduser.ToString();
                        entry.ApprovedDate = DateTime.Now;
                    }
                    else
                    {
                        //Modify approve
                        entry.ApprovedModifyDate = DateTime.Now;
                    }

                    await _repository.UpdateAsync(entry);
                }

                var tmpToDelete = await _Tmpdetails.All()
                                .Where(d => d.Luser == Approveduser.ToString() && d.Ofrno == ofrNo).ToListAsync();

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
            catch(Exception ex)
            {
                await _repository.RollbackTransactionAsync();
                Console.WriteLine($"Insertion Failed: {ex.Message}");
                return false;
            }           
        }
        #endregion

        #region Get All Requisition for Bottom Grid
        public async Task<PaginationResponse<OFRApprovalBottomGridVM>> AllRequisitionforBottomGrid(PaginationRequest request)
        {
            var query = (from main in _repository.All().AsNoTracking()
                        join doc in _documentation.All().AsNoTracking()
                            on main.JobNo equals doc.JobNo
                        join cust in _customer.All().AsNoTracking()
                            on doc.CustomerId equals cust.CustomerId into cusG
                        from cust in cusG.DefaultIfEmpty()
                        join exp in _shipmentmode.All().AsNoTracking()
                            on main.ExpenseTypeId equals exp.ExpenseTypeId into shipG
                        from exp in shipG.DefaultIfEmpty()
                        where !string.IsNullOrEmpty(main.ApprovedUser)
                        select new OFRApprovalBottomGridVM
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
                        }).AsNoTracking();

            if (string.IsNullOrEmpty(request.SortColumn))
                request.SortColumn = nameof(OFRApprovalBottomGridVM.OFRNo);
            if (string.IsNullOrEmpty(request.SortOrder))
                request.SortOrder = "desc";

            return await PaginationService<OFRApprovalBottomGridVM>.GetPaginatedData(query, request);
        }
        #endregion

        #region When click Edit Button

        #region Getting Master Data
        public async Task<OFRApprovalMasterVM> GetRequisitionInputData(string jobNo)
        {
            var data = await (from req in _repository.All()
                              join det in _details.All()
                               on req.Ofrno equals det.Ofrno into detGroup
                              from det in detGroup.DefaultIfEmpty()  // left join with details

                              join doc in _sepDocumentation.All()
                                on req.JobNo equals doc.JobNo into docGroup
                              from doc in docGroup.DefaultIfEmpty()  // left join with documentation

                              join st in _serviceType.All()
                                on det.ServiceTypeId equals st.ServiceTypeId into stGroup
                              from st in stGroup.DefaultIfEmpty()   // ServiceType lookup

                              join eh in _accountsHead.All()
                                on det.ExpenseHeadId equals eh.ExpenseHeadId into ehGroup
                              from eh in ehGroup.DefaultIfEmpty()   // ExpenseHead lookup


                              join cust in _customer.All()
                                on doc.CustomerId equals cust.CustomerId into custGroup
                              from cust in custGroup.DefaultIfEmpty()   // Customer

                              join ship in _shipmentmode.All()
                                on req.ExpenseTypeId equals ship.ExpenseTypeId into shipGroup
                              from ship in shipGroup.DefaultIfEmpty() //ShipmentMode

                              where req.JobNo == jobNo
                              select new OFRApprovalMasterVM
                              {
                                  OFRNo = req.Ofrno,
                                  OFRDate = req.Ofrdate,
                                  JobNo = req.JobNo,
                                  ShipmentMode = ship.ExpenseType,
                                  CustomerName = cust.CustomerName,
                                  LCValue = doc.Lcvalue,
                                  InvoiceNo = doc.InvoiceNo,
                                  MaterialDescription = doc.MaterialDescription,
                                  Quantity = doc.Quntity1,
                                  Weight = doc.Quntity2

                              }).FirstOrDefaultAsync();

            return data;
        }

        public async Task<OFRApprovalMasterVM> GetRequisitionInputDataByOFRNo(string ofrNo)
        {
            var data = await (from req in _repository.All()
                              join det in _details.All()
                               on req.Ofrno equals det.Ofrno into detGroup
                              from det in detGroup.DefaultIfEmpty()  // left join with details

                              join doc in _sepDocumentation.All()
                                on req.JobNo equals doc.JobNo into docGroup
                              from doc in docGroup.DefaultIfEmpty()  // left join with documentation

                              join st in _serviceType.All()
                                on det.ServiceTypeId equals st.ServiceTypeId into stGroup
                              from st in stGroup.DefaultIfEmpty()   // ServiceType lookup

                              join eh in _accountsHead.All()
                                on det.ExpenseHeadId equals eh.ExpenseHeadId into ehGroup
                              from eh in ehGroup.DefaultIfEmpty()   // ExpenseHead lookup

                              join cust in _customer.All()
                                on doc.CustomerId equals cust.CustomerId into custGroup
                              from cust in custGroup.DefaultIfEmpty()   // Customer

                              join ship in _shipmentmode.All()
                                on req.ExpenseTypeId equals ship.ExpenseTypeId into shipGroup
                              from ship in shipGroup.DefaultIfEmpty() //ShipmentMode

                              where req.Ofrno == ofrNo
                              select new OFRApprovalMasterVM
                              {
                                  OFRNo = req.Ofrno,
                                  OFRDate = req.Ofrdate,
                                  JobNo = req.JobNo,
                                  ShipmentMode = ship.ExpenseType,
                                  CustomerName = cust.CustomerName,
                                  LCValue = doc.Lcvalue,
                                  InvoiceNo = doc.InvoiceNo,
                                  MaterialDescription = doc.MaterialDescription,
                                  Quantity = doc.Quntity1,
                                  Weight = doc.Quntity2
                              }).FirstOrDefaultAsync();

            return data;
        }
        #endregion

        #region Copied To Tmp Details Table
        public async Task<bool> CopyDetailsToTmpTable(string jobNo, string? currentUserId, BaseViewModel model)
        {
            return await CopyDetailsToTmp(jobNo, currentUserId);
        }

        public async Task<bool> CopyDetailsToTmpTableByOFRNo(string ofrNo, string? currentUserId, BaseViewModel model)
        {
            if (string.IsNullOrEmpty(currentUserId)) return false;

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
                DifferentAmount2 = d.DifferentAmount2,
                BillAdjustAmount = d.BillAdjustAmount,
                Luser = currentUserId,
                Ldate = DateTime.Now,
                Lip = model.Lip,
                Lmac = model.Lmac,
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
    }
}
