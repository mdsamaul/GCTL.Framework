
using GCTL.Core.Data;
using GCTL.Core.DataTables;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.OFRSection.OperationalFund;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Core_ServiceType = GCTL.Data.Models.CoreServiceType;
using Sep_Documentation = GCTL.Data.Models.SepDocumentation;
namespace GCTL.Service.OperationalFund
{
    public class OperationalService : AppService<CfOperationalFundRequisitionEntry>, Ioperational
    {
        #region Service
        //private readonly IUserInfoService _userInfoService;
        private readonly IRepository<CfOperationalFundRequisitionEntry> _repository;
        private readonly IRepository<CfOperationalFundRequisitionDetail> _details;
        private readonly IRepository<CfOperationalFundRequisitionDetailsTemp> _Tmpdetails;
        private readonly IRepository<Sep_Documentation> _genericRepository;
        private readonly IRepository<CfDefExpenseHead> _accountsHead;
        private readonly IRepository<Core_ServiceType> _serviceType;
        private readonly IRepository<CfDefExpenseType> _shipmentmode;
        private readonly IRepository<SalesCustomer> _customer;
        private readonly IRepository<CaDefCurrency> _currencies;

        public OperationalService(IRepository<Sep_Documentation> genericRepository,/* IUserInfoService userInfoService, */IRepository<CfOperationalFundRequisitionEntry> repository, IRepository<CfDefExpenseHead> accountsHead, IRepository<Core_ServiceType> serviceType, IRepository<CfDefExpenseType> shipmentmode, IRepository<SalesCustomer> customer, IRepository<CaDefCurrency> currencies, IRepository<CfOperationalFundRequisitionDetail> details, IRepository<CfOperationalFundRequisitionDetailsTemp> tmpdetails) : base(repository)
        {
            /*_userInfoService = userInfoService;*/
            _repository = repository;
            _genericRepository = genericRepository;
            _accountsHead = accountsHead;
            _serviceType = serviceType;
            _shipmentmode = shipmentmode;
            _customer = customer;
            _currencies = currencies;
            _details = details;
            _Tmpdetails = tmpdetails;
        }

        #endregion

        #region Get All Job Entry List
        public async Task<PaginationResponse<JobEntryListVM>> GetAllJonEntryList(
    PaginationRequest request, string customerid = "", string shipmentmodeid = "")
        {
            var query = from d in _genericRepository.All()          // Sep_Documentation
                        join r in _repository.All()                  // CF_OperationalFundRequisitionEntry
                            on d.JobNo equals r.JobNo into rr
                        from r in rr.DefaultIfEmpty()

                            //Customer LEFT JOIN
                        join c in _customer.All()
                            on d.CustomerId equals c.CustomerId into cc
                        from c in cc.DefaultIfEmpty()

                            //Expense Type LEFT JOIN
                        join e in _shipmentmode.All()
                            on d.ExpenseTypeId equals e.ExpenseTypeId into ee
                        from e in ee.DefaultIfEmpty()

                            //MAIN CONDITION
                        where r == null

                        select new JobEntryListVM
                        {
                            JobNo = d.JobNo,
                            JobDate = d.Date,
                            DocReceivedDate = d.DocReceivedDate,

                            CustomerID = d.CustomerId,
                            Customer = c != null ? c.CustomerName : "",

                            ShipmentModeID = e != null ? e.ExpenseTypeId : "",
                            ShipmentMode = e != null ? e.ExpenseType : ""
                        };
            int a = await query.CountAsync();
            if (!string.IsNullOrEmpty(customerid))
                query = query.Where(x => x.CustomerID == customerid);

            if (!string.IsNullOrEmpty(shipmentmodeid))
                query = query.Where(x => x.ShipmentModeID == shipmentmodeid);

            if (request.PageSize == -1)
            {
                request.PageSize = await query.CountAsync();
                request.PageNumber = 1;
            }

            if (string.IsNullOrEmpty(request.SortColumn))
                request.SortColumn = nameof(JobEntryListVM.JobNo);

            var result = await PaginationService<JobEntryListVM>.GetPaginatedData(query, request);
            result.TotalCount = a;
            return result;
        }

        public async Task<PaginationService<JobEntryListVM, JobEntryListVM>.PaginationResult<JobEntryListVM>> GetAllJonEntryList(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string sortColumn = "JobNo", string sortOrder = "desc", string customerid = "", string shipmentmodeid = "")
        {
            var query = from d in _genericRepository.All()          // Sep_Documentation
                        join r in _repository.All()                  // CF_OperationalFundRequisitionEntry
                            on d.JobNo equals r.JobNo into rr
                        from r in rr.DefaultIfEmpty()

                            //Customer LEFT JOIN
                        join c in _customer.All()
                            on d.CustomerId equals c.CustomerId into cc
                        from c in cc.DefaultIfEmpty()

                            //Expense Type LEFT JOIN
                        join e in _shipmentmode.All()
                            on d.ExpenseTypeId equals e.ExpenseTypeId into ee
                        from e in ee.DefaultIfEmpty()

                            //MAIN CONDITION
                        where r == null

                        select new JobEntryListVM
                        {
                            JobNo = d.JobNo,
                            JobDate = d.Date,
                            DocReceivedDate = d.DocReceivedDate,

                            CustomerID = d.CustomerId,
                            Customer = c != null ? c.CustomerName : "",

                            ShipmentModeID = e != null ? e.ExpenseTypeId : "",
                            ShipmentMode = e != null ? e.ExpenseType : ""
                        };


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

            DateTime? parsedSearchDate = null;

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var dateFormats = new[]
                {
                    "dd/MM/yyyy", "d/M/yyyy", "dd/M/yy",
                    "dd-MM-yyyy", "d-M-yyyy", "dd-M-yy",
                    "yyyy-MM-dd", "yyyy/M/d", "yy/M/dd",
                    "yyyy/MM/dd", "yyyy-M-d", "yy-M-dd"
                };

                if (DateTime.TryParseExact(searchTerm, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exactDate))
                {
                    parsedSearchDate = exactDate.Date;
                }
                else if (DateTime.TryParse(searchTerm, CultureInfo.InvariantCulture, DateTimeStyles.None, out var looseDate))
                {
                    parsedSearchDate = looseDate.Date;
                }
            }

            var paginatedResult = await PaginationService<JobEntryListVM, JobEntryListVM>.GetPaginatedData(
                query, pageNumber, pageSize, searchTerm, sortColumn, sortOrder,
                term => sc =>
                    EF.Functions.Like(sc.JobNo ?? "", $"%{term}%") ||
                    EF.Functions.Like(sc.Customer ?? "", $"%{term}%") ||
                    EF.Functions.Like(sc.ShipmentMode ?? "", $"%{term}%") ||

                    // Exact-date match when the term parsed successfully as a date
                    (parsedSearchDate.HasValue && sc.JobDate.HasValue && sc.JobDate.Value.Date == parsedSearchDate.Value) ||
                    (parsedSearchDate.HasValue && sc.DocReceivedDate.HasValue && sc.DocReceivedDate.Value.Date == parsedSearchDate.Value),

                sc => sc
            );

            return paginatedResult;
        }

        #endregion

        #region Get Requisition Info
        public async Task<RequisitionHeaderVM> GetRequisitionHeaderAsync(string jobno)
        {
            var data = await (from d in _genericRepository.All()
                              join c in _customer.All()
                                  on d.CustomerId equals c.CustomerId into cc
                              from c in cc.DefaultIfEmpty()
                              join e in _shipmentmode.All()
                                  on d.ExpenseTypeId equals e.ExpenseTypeId into ee
                              from e in ee.DefaultIfEmpty()

                              join curr in _currencies.All()
                                on d.InvoiceCurrencyId equals curr.CurrencyId into currs
                              from curr in currs.DefaultIfEmpty()
                              where d.JobNo == jobno
                              select new RequisitionHeaderVM
                              {
                                  TC = d.Tc,
                                  JobNo = d.JobNo,
                                  ShipmentModeID = e.ExpenseTypeId,
                                  CustomerID = c.CustomerId,
                                  ShipmentMode = e != null ? e.ExpenseType : "",
                                  CustomerName = c != null ? c.CustomerName : "",
                                  LcValue = d.Lcvalue,
                                  InvoiceNo = d.InvoiceNo,
                                  InvoiceValue = d.InvoiceValue,
                                  HAWB = d.Hawb,
                                  MaterialDescription = d.MaterialDescription,
                                  Weight = d.Quntity2,
                                  Qty = d.Quntity1,
                                  CurrencyID = curr != null ? curr.CurrencyId : ""

                              }).FirstOrDefaultAsync();

            return data;
        }

        #endregion

        #region Generate Requisition No
        public async Task<string> GenerateRequisitionNoAsync(string jobNo)
        {
            if (string.IsNullOrEmpty(jobNo))
                return string.Empty;

            // Take only part before first '_'
            var firstPart = jobNo.Split('_').First();
            // firstPart = "CouImp/Dha"

            var prefix = firstPart + "/Ofr";
            // CouImp/Dha/Ofr

            var monthYear = DateTime.Now.ToString("MMyy"); // 1225

            var lastRequisition = await _repository.All()
                .Where(r =>
                    r.Ofrno.StartsWith(prefix) &&
                    r.Ofrno.Contains($"_{monthYear}_"))
                .OrderByDescending(r => r.Ofrno)
                .FirstOrDefaultAsync();

            int serial = 1;

            if (lastRequisition != null)
            {
                var lastSerialStr = lastRequisition.Ofrno.Split('_').Last();
                if (int.TryParse(lastSerialStr, out int lastSerial))
                    serial = lastSerial + 1;
            }

            return $"{prefix}_{monthYear}_{serial:D6}";
        }
        #endregion

        #region Get All Requisition Entry
        public async Task<PaginationResponse<RequisitionVM>> GetAllRequisition(
    PaginationRequest request)
        {
            var query = from main in _repository.All()                 // CF_OperationalFundRequisitionEntry
                        join doc in _genericRepository.All()               // Sep_Documentation
                            on main.JobNo equals doc.JobNo
                        join cust in _customer.All()                   // Sales_Customer
                            on doc.CustomerId equals cust.CustomerId
                        join exp in _shipmentmode.All()                // CF_Def_ExpenseType
                            on main.ExpenseTypeId equals exp.ExpenseTypeId
                        select new RequisitionVM
                        {
                            TC = main.Tc,
                            OFRNo = main.Ofrno,
                            OFRDate = main.Ofrdate,
                            JobNo = main.JobNo,

                            CustomerName = cust.CustomerName,
                            BillingAddress = cust.CustomerAddress,

                            ShipmentMode = exp.ExpenseType
                        };

            if (request.PageSize == -1)
            {
                request.PageSize = await query.CountAsync();
                request.PageNumber = 1;
            }

            if (string.IsNullOrEmpty(request.SortColumn))
                request.SortColumn = nameof(RequisitionVM.JobNo);

            return await PaginationService<RequisitionVM>.GetPaginatedData(query, request);
        }

        public async Task<PaginationService<RequisitionVM, RequisitionVM>.PaginationResult<RequisitionVM>> GetAllRequisition(int pageNumber = 1, int pageSize = 5, string searchTerm = "", string sortColumn = "OFRNo", string sortOrder = "desc")
        {

            //var query = from main in _repository.All()                     // CF_OperationalFundRequisitionEntry
            //            join cust in _customer.All()                        // Sales_Customer
            //                on main.EmployeeID equals cust.EmployeeID
            //            join exp in _shipmentmode.All()                     // CF_Def_ExpenseType
            //                on main.ExpenseTypeID equals exp.ExpenseTypeID
            //            select new RequisitionVM
            //            {
            //                TC = main.TC,
            //                OFRNo = main.OFRNo,
            //                OFRDate = main.OFRDate,
            //                JobNo = main.JobNo,

            //                CustomerName = cust.CustomerName,
            //                BillingAddress = cust.CustomerAddress,

            //                ShipmentMode = exp.ExpenseType
            //            };
            var query = from main in _repository.All()                 // CF_OperationalFundRequisitionEntry
                        join doc in _genericRepository.All()               // Sep_Documentation
                            on main.JobNo equals doc.JobNo
                        join cust in _customer.All()                   // Sales_Customer
                            on doc.CustomerId equals cust.CustomerId
                        join exp in _shipmentmode.All()                // CF_Def_ExpenseType
                            on main.ExpenseTypeId equals exp.ExpenseTypeId
                        select new RequisitionVM
                        {
                            TC = main.Tc,
                            OFRNo = main.Ofrno,
                            OFRDate = main.Ofrdate,
                            JobNo = main.JobNo,

                            CustomerName = cust.CustomerName,
                            BillingAddress = cust.CustomerAddress,

                            ShipmentMode = exp.ExpenseType
                        };

            if (pageSize == -1)
            {
                pageSize = await query.CountAsync();
                pageNumber = 1;
            }

            DateTime? parsedSearchDate = null;

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var dateFormats = new[]
                {
                    "dd/MM/yyyy", "d/M/yyyy", "dd/M/yy",
                    "dd-MM-yyyy", "d-M-yyyy", "dd-M-yy",
                    "yyyy-MM-dd", "yyyy/M/d", "yy/M/dd",
                    "yyyy/MM/dd", "yyyy-M-d", "yy-M-dd"
                };

                if (DateTime.TryParseExact(searchTerm, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exactDate))
                {
                    parsedSearchDate = exactDate.Date;
                }
                else if (DateTime.TryParse(searchTerm, out var looseDate))
                {
                    parsedSearchDate = looseDate.Date;
                }
            }

            // PaginationService
            var paginatedResult = await PaginationService<RequisitionVM, RequisitionVM>.GetPaginatedData(query, pageNumber, pageSize, searchTerm, sortColumn, sortOrder,
                    term => sc =>
                        EF.Functions.Like(sc.OFRNo ?? "", $"%{term}%") ||
                        EF.Functions.Like(sc.JobNo ?? "", $"%{term}%") ||
                        EF.Functions.Like(sc.CustomerName ?? "", $"%{term}%") ||
                        EF.Functions.Like(sc.BillingAddress ?? "", $"%{term}%") ||
                        (parsedSearchDate.HasValue && sc.OFRDate.HasValue && sc.OFRDate.Value.Date == parsedSearchDate.Value) ||
                        EF.Functions.Like(sc.ShipmentMode ?? "", $"%{term}%"),
                    ob => ob
             );

            return paginatedResult;
        }
        #endregion

        #region Dropdown

        #region Accounts Head Dropdown
        public async Task<IEnumerable<SelectListItem>> GetAllAccountsHead(string serviceTypeId)
        {
            var typelist = await _accountsHead.All().Where(e => e.ServiceTypeId == serviceTypeId).Select(t => new SelectListItem
            {
                Value = t.ExpenseHeadId.ToString(),
                Text = t.ExpenseHead
            }).ToListAsync();

            return typelist;
        }
        #endregion

        #region Ship Mode Dropdown
        public async Task<IEnumerable<SelectListItem>> GetAllShipmentMode()
        {
            var typelist = await _shipmentmode.All().Select(t => new SelectListItem
            {
                Value = t.ExpenseTypeId.ToString(),
                Text = t.ExpenseType
            }).ToListAsync();

            return typelist;
        }
        #endregion

        #region Service Type Dropdown
        public async Task<IEnumerable<SelectListItem>> GetAllServiceType()
        {
            var typelist = await _serviceType.All().Select(t => new SelectListItem
            {
                Value = t.ServiceTypeId.ToString(),
                Text = t.ServiceTypeName,
            }).ToListAsync();

            return typelist;
        }
        #endregion

        #region  Customer Dropdown
        public async Task<IEnumerable<SelectListItem>> GetAllCustomer()
        {
            return await _customer.All()
                .Select(e => new SelectListItem
                {
                    Value = e.CustomerId.ToString(),
                    Text = e.CustomerName
                }).ToListAsync();
        }
        #endregion

        #region  Currencies Dropdown
        public async Task<IEnumerable<SelectListItem>> GetAllCurencies()
        {
            return await _currencies.All()
                .Select(e => new SelectListItem
                {
                    Value = e.CurrencyId.ToString(),
                    Text = e.CurrencyName
                }).ToListAsync();
        }


        #endregion

        #endregion

        #region Generate Ofr Details ID
        private async Task<string> GenerateOfrDetailsId()
        {
            string lastId = await _details.All().OrderByDescending(x => x.OfrDetailsId).Select(x => x.OfrDetailsId).MaxAsync();

            if (string.IsNullOrEmpty(lastId))
            {
                return "00000001";
            }

            int nextId = int.Parse(lastId) + 1;
            return nextId.ToString("D8");
        }

        #endregion

        #region TMP Section

        #region Tmp Details Requisition Save and Update
        public async Task<CommonReturnViewModel> TmpDetailsSaveAsync(RequistionDetailsTmpVM model)
        {
            await _Tmpdetails.BeginTransactionAsync();
            try
            {
                if (string.IsNullOrEmpty(model.ReqNo) || model.RequDate == DateTime.MinValue || string.IsNullOrEmpty(model.ServiceTypeID) || string.IsNullOrEmpty(model.CustomerNameID) || string.IsNullOrEmpty(model.ShipmentModeID))
                {
                    await _Tmpdetails.RollbackTransactionAsync();
                    return new CommonReturnViewModel
                    {
                        Success = false, Message = "Validation Failed."
                    };
                }

                bool accountHeadAlreadyAdded = await _Tmpdetails.All()
                    .AnyAsync(x => x.Luser == model.Luser
                        && x.JobNo == model.JobNo
                        && x.ExpenseHeadId == model.AccountHeadID);

                if (accountHeadAlreadyAdded)
                {
                    await _Tmpdetails.RollbackTransactionAsync();
                    return new CommonReturnViewModel
                    {
                        Success = false, Message = "Already been added for this job."
                    };
                }

                string newOffrNO = await GenerateRequisitionNoAsync(model.JobNo);
                string newOFRDetailsID = await GenerateOfrDetailsId();

                var entity = new CfOperationalFundRequisitionDetailsTemp
                {
                    //OFR_DetailsID = newOFRDetailsID,
                    //OFRNo = newOffrNO,
                    OfrDetailsId = "",
                    Ofrno = "",
                    JobNo = model.JobNo,
                    ExpenseHeadId = model.AccountHeadID,
                    ServiceTypeId = model.ServiceTypeID,
                    EstimatedAmount = model.Amount,
                    ActualAmount = 0,
                    DifferentAmount = 0,
                    DifferentAmount2 = 0,
                    BillAdjustAmount = 0,
                    Ldate = DateTime.Now,
                    Luser = model.Luser,        //CreatedBy.ToString(),
                    Lip = model.Lip,
                    Lmac = model.Lmac,
                    CompanyCode = "",
                    ExpenseHeadRemarks = model.Remark,
                    EmployeeId = model.CustomerNameID
                };

                await _Tmpdetails.AddAsync(entity);
                await _Tmpdetails.CommitTransactionAsync();

                return new CommonReturnViewModel
                {
                    Success = false
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Insertion Failed: {ex.Message}");
                await _Tmpdetails.RollbackTransactionAsync();

                return new CommonReturnViewModel
                {
                    Success = false, Message = "Insertation Failed!"
                };
            }
        }


        //public async Task<bool> UpdateAsync(RequistionDetailsTmpVM model)
        //{
        //    await _Tmpdetails.BeginTransactionAsync();
        //    try
        //    {
        //        if (string.IsNullOrEmpty(model.ReqNo) || model.RequDate == DateTime.MinValue || string.IsNullOrEmpty(model.ServiceTypeID) || string.IsNullOrEmpty(model.CustomerName) || string.IsNullOrEmpty(model.ShipmentMode))
        //        {
        //            return false;
        //        }
        //        var entity = await _repository.GetByIdAsync(model.Tc);
        //        if (entity == null)
        //        {
        //            await _repository.RollbackTransactionAsync();
        //            return false;
        //        }

        //        entity.OFRNo = "";
        //        entity.JobNo = "";
        //        entity.ExpenseTypeID = model.AccountHeadID;
        //        //entity.S = model.ServiceTypeID;

        //        entity.LDate = DateTime.MinValue;
        //        entity.LIP = model.LIP;
        //        entity.LMAC = model.LMAC;
        //        entity.CompanyCode = "";
        //        entity.Ex = model.Remark;
        //        entity.EmployeeID = model.EmployeeID;
        //        entity.LUser = model.UpdatedBy.ToString();

        //        await _Tmpdetails.UpdateAsync(entity);
        //        await _Tmpdetails.CommitTransactionAsync();
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.ToString());
        //        await _Tmpdetails.RollbackTransactionAsync();
        //        return false;
        //    }
        //}

        #endregion

        #region Delete Tmp Details with filtering Current User ID and Specific Row from table
        public async Task<bool> DeleteTmpDetailAsync(decimal tc, string? currentUserId)
        {
            await _Tmpdetails.BeginTransactionAsync();

            try
            {
                if (currentUserId == null) return false;

                var entity = await _Tmpdetails.All()
                    .FirstOrDefaultAsync(x => x.Tc == tc && x.Luser == currentUserId.ToString());

                if (entity == null) return false;

                await _Tmpdetails.DeleteAsync(entity.Tc);
                await _Tmpdetails.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                await _Tmpdetails.RollbackTransactionAsync();
                return false;
            }
        }
        #endregion

        #region When User Click on CLear Button then Current User Related Data Delete from Tmp Details Table
        public async Task<bool> ClearTmp(string? currentUserId)
        {
            await _Tmpdetails.BeginTransactionAsync();

            try
            {
                if (currentUserId == null) return false;

                var entity = await _Tmpdetails.All().Where(x => x.Luser == currentUserId.ToString()).ToListAsync();

                if (entity == null) return false;

                await _Tmpdetails.DeleteRangeAsync(entity);
                await _Tmpdetails.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                await _Tmpdetails.RollbackTransactionAsync();
                return false;
            }
        }
        #endregion

        #region Get All Tmp Details
        public async Task<List<RequistionDetailsTmpVM>> GetAllTmpAsync(string? currentUser)
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
                                where tmp.Luser == currentUser
                                orderby Convert.ToInt32(head.SerialNo)

                                select new RequistionDetailsTmpVM
                                {
                                    Tc = tmp.Tc,
                                    SerialNo = head.SerialNo,
                                    AccountHeadName = head.ExpenseHead,
                                    IsReceivetable = head.IsReceiptable,
                                    ServiceTypeName = servic.ServiceTypeName,
                                    Amount = tmp.EstimatedAmount,
                                    Remark = tmp.ExpenseHeadRemarks
                                }).ToListAsync();

            return result;
        }

        #endregion

        #endregion

        #region Operntional Fund Entry Master & Detail Save
        public async Task<bool> MasterDetailsSaveAsync(RequsitionMasterVM model)
        {
            if (string.IsNullOrEmpty(model.ExpenseTypeID) || model.OFFRDate == DateTime.MinValue)
                return false;

            await _repository.BeginTransactionAsync();
            try
            {
                //Check if Master already exists
                var existingMaster = await _repository.All().FirstOrDefaultAsync(x => x.JobNo == model.JobNo);

                string ofrNo;

                if (existingMaster == null)
                {
                    //If Master doesn't exist → create new Master
                    ofrNo = await GenerateRequisitionNoAsync(model.JobNo);
                    var master = new CfOperationalFundRequisitionEntry
                    {
                        Ofrno = ofrNo,
                        Ofrdate = model.OFFRDate,
                        JobNo = model.JobNo,
                        ExpenseTypeId = model.ExpenseTypeID,
                        Luser = model.Luser,   //.CreatedBy.ToString(),
                        Lip = model.Lip,
                        Ldate = DateTime.Now,
                        Lmac = model.Lmac,
                        PortCharge = 0,
                        AsycodaCharge = 0,
                        DutyByChallanCharge = 0,
                        ShippingCharge = 0,
                        PrifixId = "",
                        CompanyCode = "",
                        EmployeeId = model.UserInfoEmployeeId,//   .CreatedBy.ToString(),
                        CashBank = "",
                        CashBank2 = "",
                        BankAccount = "",
                        BankAccount2 = "",
                    };

                    await _repository.AddAsync(master);
                }
                else
                {
                    // Master exists → don't update anything
                    ofrNo = existingMaster.Ofrno;
                }

                //Update TmpDetails with Amounts from frontend
                if (model.Details != null && model.Details.Any())
                {
                    foreach (var inputDetail in model.Details)
                    {
                        var tmp = await _Tmpdetails.All()
                                    .FirstOrDefaultAsync(d => d.Tc == inputDetail.Tc);

                        if (tmp != null)
                        {
                            tmp.EstimatedAmount = inputDetail.Amount ?? 0;
                            tmp.ExpenseHeadRemarks = inputDetail.Remark ?? "";
                            await _Tmpdetails.UpdateAsync(tmp);
                        }
                    }
                }

                //Get Tmp Details for this JobNo + User
                var tmpDetails = await _Tmpdetails.All().Where(d => d.Luser == model.Luser    /*.CreatedBy.ToString()*/ && d.JobNo == model.JobNo).ToListAsync();
                var currentDetailIds = new List<string>();
                //Move Tmp Details → Actual Details Table
                foreach (var tmp in tmpDetails)
                {
                    //Check if detail already exists
                    var existingDetail = await _details.All().FirstOrDefaultAsync(d => d.Ofrno == ofrNo && d.JobNo == tmp.JobNo && d.OfrDetailsId == tmp.OfrDetailsId);

                    if (existingDetail != null)
                    {
                        //Update existing
                        existingDetail.ExpenseHeadId = tmp.ExpenseHeadId;
                        existingDetail.ServiceTypeId = tmp.ServiceTypeId;
                        existingDetail.EstimatedAmount = tmp.EstimatedAmount ?? 0;
                        existingDetail.ActualAmount = tmp.ActualAmount ?? 0;
                        existingDetail.DifferentAmount = tmp.DifferentAmount ?? 0;
                        existingDetail.BillAdjustAmount = tmp.BillAdjustAmount ?? 0;
                        existingDetail.DifferentAmount2 = tmp.DifferentAmount2 ?? 0;
                        existingDetail.Luser = tmp.Luser;
                        existingDetail.Lip = tmp.Lip;
                        existingDetail.Lmac = tmp.Lmac;
                        existingDetail.Ldate = DateTime.Now;
                        existingDetail.EmployeeId = model.UserInfoEmployeeId;   //.CreatedBy.ToString();
                        existingDetail.DetailsBankAccount = "";
                        existingDetail.AdjustDetailsBankAccount = "";
                        existingDetail.AdjustDetailsCashBank = "";
                        existingDetail.AdjustApprovalAmount = 0;
                        existingDetail.AdjustUser = "";
                        existingDetail.DetailsCashBank = "";
                        existingDetail.ExpenseHeadRemarks = tmp.ExpenseHeadRemarks ?? "";

                        await _details.UpdateAsync(existingDetail);
                        currentDetailIds.Add(existingDetail.OfrDetailsId);
                    }
                    else
                    {
                        //Insert new
                        var newId = await GenerateOfrDetailsId();

                        var newDetail = new CfOperationalFundRequisitionDetail
                        {
                            OfrDetailsId = newId,
                            Ofrno = ofrNo,
                            JobNo = tmp.JobNo,
                            ExpenseHeadId = tmp.ExpenseHeadId,
                            ServiceTypeId = tmp.ServiceTypeId,
                            EstimatedAmount = tmp.EstimatedAmount ?? 0,
                            ActualAmount = tmp.ActualAmount ?? 0,
                            DifferentAmount = tmp.DifferentAmount ?? 0,
                            BillAdjustAmount = tmp.BillAdjustAmount ?? 0,
                            DifferentAmount2 = tmp.DifferentAmount2 ?? 0,
                            Luser = tmp.Luser,
                            Lip = tmp.Lip,
                            Lmac = tmp.Lmac,
                            Ldate = DateTime.Now,
                            CompanyCode = "",
                            EmployeeId = model.UserInfoEmployeeId,  //.CreatedBy.ToString(),
                            DetailsBankAccount = "",
                            AdjustDetailsBankAccount = "",
                            AdjustDetailsCashBank = "",
                            AdjustApprovalAmount = 0,
                            AdjustUser = "",
                            DetailsCashBank = "",
                            ExpenseHeadRemarks = tmp.ExpenseHeadRemarks ?? "",
                        };

                        await _details.AddAsync(newDetail);
                        currentDetailIds.Add(newId);
                    }
                }

                // Find real detail rows for this OFR that no longer exist in tmp
                //var currentDetailIds = tmpDetails.Select(t => t.OfrDetailsId).ToList(); // or your matching key
                var detailsToRemove = await _details.All()
                    .Where(d => d.Ofrno == ofrNo && !currentDetailIds.Contains(d.OfrDetailsId))
                    .ToListAsync();

                if (detailsToRemove.Any())
                    await _details.DeleteRangeAsync(detailsToRemove);

                //Delete Tmp Details
                await _Tmpdetails.DeleteRangeAsync(tmpDetails);
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

        #region Requisition Entry Delete
        public async Task<bool> BulkDeleteAsync(List<string> ofrNos)
        {
            await _repository.BeginTransactionAsync();

            try
            {
                var masters = await _repository.All().Where(m => ofrNos.Contains(m.Ofrno)).ToListAsync();

                if (!masters.Any())
                {
                    await _repository.RollbackTransactionAsync();
                    return false;
                }
                var details = await _details.All().Where(d => ofrNos.Contains(d.Ofrno)).ToListAsync();

                if (details.Any())
                {
                    await _details.DeleteRangeAsync(details);
                }

                await _repository.DeleteRangeAsync(masters);

                await _repository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                await _repository.RollbackTransactionAsync();
                Console.WriteLine($"Bulk delete error: {ex}");
                return false;
            }
        }
        #endregion

        #region When click Edit Button

        #region Getting Master Data
        public async Task<RequisitionHeaderVM> GetRequisitionDetails(string offrNo)
        {
            var data = await (from req in _repository.All()
                              join det in _details.All()
                               on req.Ofrno equals det.Ofrno into detGroup
                              from det in detGroup.DefaultIfEmpty()  // left join with details

                              join doc in _genericRepository.All()
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

                              where req.Ofrno == offrNo
                              select new RequisitionHeaderVM
                              {
                                  // Master fields
                                  JobNo = req.JobNo,
                                  ShipmentModeID = req.ExpenseTypeId,
                                  //CustomerID = req.EmployeeID,
                                  ReqDate = req.Ofrdate,
                                  ReqNo = req.Ofrno,
                                  Amount = det.EstimatedAmount,
                                  Remark = det.ExpenseHeadRemarks,
                                  CustomerName = cust.CustomerName,
                                  CustomerID = cust.CustomerId,
                                  ShipmentMode = ship.ExpenseType,
                                  ServiceTypeID = st != null ? st.ServiceTypeId : null,
                                  AccountHeadID = eh != null ? eh.ExpenseHeadId : null,

                                  // Documentation fields
                                  InvoiceNo = doc != null ? doc.InvoiceNo : null,
                                  LcValue = doc != null ? doc.Lcvalue : null,
                                  InvoiceValue = doc != null ? doc.InvoiceValue : null,
                                  HAWB = doc != null ? doc.Hawb : null,
                                  MaterialDescription = doc != null ? doc.MaterialDescription : null,
                                  Qty = doc != null ? doc.Quntity1 : null,
                                  Weight = doc != null ? doc.Quntity2 : null,
                                  CurrencyID = doc != null ? doc.CurrencyId : null
                              }).FirstOrDefaultAsync();

            return data;
        }
        #endregion

        #region Copied To Tmp Details Table
        public async Task<bool> CopyDetailsToTmp(string offrNo, string? currentUserId)
        {
            var details = await _details.All().Where(d => d.Ofrno == offrNo).ToListAsync();

            if (!details.Any()) return false;

            var existingTmp = await _Tmpdetails.All()
                .Where(x => x.Luser == currentUserId)
                .ToListAsync();

            if (existingTmp.Any())
                await _Tmpdetails.DeleteRangeAsync(existingTmp);

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
                DifferentAmount2 = d.DifferentAmount2,
                BillAdjustAmount = d.BillAdjustAmount,
                Luser = currentUserId,
                Ldate = d.Ldate,
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

        #region Ip & Mac
        public string GetLocalIP()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return string.Empty;
        }

        public string GetMacAddress()
        {
            var nics = NetworkInterface.GetAllNetworkInterfaces();
            var macAddress = string.Empty;
            foreach (var adapter in nics)
            {
                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    macAddress = adapter.GetPhysicalAddress().ToString();
                    break;
                }
            }
            return macAddress;
        }
        #endregion
    }
}

