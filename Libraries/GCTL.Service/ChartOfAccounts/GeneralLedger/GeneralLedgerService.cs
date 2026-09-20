using GCTL.Core.Data;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.ChartOfAccount;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace GCTL.Service.ChartOfAccounts.GeneralLedger
{
    public class GeneralLedgerService : AppService<AccSubSubsidiaryLedger>, IGeneralLedger
    {
        private readonly IRepository<AccControlLedger> _groupLedger;
        private readonly IRepository<AccSubControlLedger> _controlLedger;
        private readonly IRepository<AccGeneralLedger> _subcontrolLedger;
        private readonly IRepository<AccSubsidiaryLedger> _subsidiaryLedger;
        private readonly IRepository<AccSubSubsidiaryLedger> _repository;
        private readonly IRepository<AccCashFlowType> _cashFlow;

        public GeneralLedgerService(IRepository<AccSubSubsidiaryLedger> genericRepository, IRepository<AccGeneralLedger> subcontrolLedger, IRepository<AccSubControlLedger> controlLedger, IRepository<AccControlLedger> groupLedger, IRepository<AccSubsidiaryLedger> subsidiaryLedger, IRepository<AccCashFlowType> cashFlow) : base(genericRepository)
        {
            _subsidiaryLedger = subsidiaryLedger;
            _controlLedger = controlLedger;
            _groupLedger = groupLedger;
            _subcontrolLedger = subcontrolLedger;
            _repository = genericRepository;
            _cashFlow = cashFlow;
        }

        public async Task<bool> BulkDeleteAsync(List<decimal> ids)
        {
            await _repository.BeginTransactionAsync();

            try
            {
                var entity = await _repository.All().Where(c => ids.Contains(c.AutoId)).ToListAsync();

                if (entity == null || !entity.Any())
                {
                    await _repository.RollbackTransactionAsync();
                    return false;
                }

                await _repository.DeleteRangeAsync(entity);

                await _repository.CommitTransactionAsync();

                return true;
            }
            catch (Exception ex)
            {
                await _repository.RollbackTransactionAsync();
                Console.WriteLine($"Bulk Delete Error: {ex}");
                return (false);
            }
        }

        public async Task<bool> DeleteAsync(decimal id)
        {
            await _repository.BeginTransactionAsync();

            try
            {
                var entity = await _repository.GetByIdAsync(id);

                if (entity == null)
                {
                    await _repository.RollbackTransactionAsync();
                    return false;
                }

                await _repository.DeleteAsync(entity.AutoId);

                await _repository.CommitTransactionAsync();

                return true;
            }
            catch (Exception ex)
            {
                await _repository.RollbackTransactionAsync();
                Console.WriteLine($"Single Delete Error: {ex}");
                return (false);
            }
        }

        public async Task<List<GeneralLedgerVM>> GetAllAsync(string groupLedgerCode = "", string controlLedgerCode = "", string subcontrolLedgerCode = "", string subSididaryLedger = "")
        {
            IQueryable<AccSubSubsidiaryLedger> generalQuery;

            if (string.IsNullOrEmpty(groupLedgerCode) &&
                string.IsNullOrEmpty(controlLedgerCode) &&
                string.IsNullOrEmpty(subcontrolLedgerCode) &&
                string.IsNullOrEmpty(subSididaryLedger))
            {
                generalQuery = _repository.All();
            }
            else
            {
                var query = from general in _repository.All()
                            join subsidi in _subsidiaryLedger.All() on general.SubsidiaryLedgerCodeNo equals subsidi.SusidiaryLedgerCodeNo
                            join subcrl in _subcontrolLedger.All() on subsidi.GeneralLedgerCodeNo equals subcrl.GeneralLedgerCodeNo
                            join cl in _controlLedger.All() on subcrl.SubControlLedgerCodeNo equals cl.SubControlLedgerCodeNo
                            join gr in _groupLedger.All() on cl.ControlLedgerCodeNo equals gr.ControlLedgerCodeNo
                            select new { general, subsidi, subcrl, cl, gr };

                if (!string.IsNullOrEmpty(groupLedgerCode))
                {
                    query = query.Where(x => x.gr.ControlLedgerCodeNo == groupLedgerCode);
                }

                if (!string.IsNullOrEmpty(controlLedgerCode))
                {
                    query = query.Where(x => x.cl.SubControlLedgerCodeNo == controlLedgerCode);
                }

                if (!string.IsNullOrEmpty(subcontrolLedgerCode))
                {
                    query = query.Where(x => x.subcrl.GeneralLedgerCodeNo == subcontrolLedgerCode);
                }

                if (!string.IsNullOrEmpty(subSididaryLedger))
                {
                    query = query.Where(x => x.subsidi.SusidiaryLedgerCodeNo == subSididaryLedger);
                }

                generalQuery = query.Select(x => x.general);
            }

            var entity = await generalQuery.ToListAsync();
            var cashFlows = await _cashFlow.All().ToDictionaryAsync(c => c.CashFlowTypeId, c => c.CashFlowTypeName);

            var result = entity.Select(model => new GeneralLedgerVM
            {
                autoId = model.AutoId,
                SubSusidiaryLedgerCodeNo = model.SubSusidiaryLedgerCodeNo,
                SubSubsidiaryLedgerName = model.SubSubsidiaryLedgerName,
                ShortName = model.ShortName,
                CashFlowTypeID = model.CashFlowTypeId,
                CashFlowTypeName = (!string.IsNullOrEmpty(model.CashFlowTypeId) && cashFlows.ContainsKey(model.CashFlowTypeId)) ? cashFlows[model.CashFlowTypeId] : "",
                IsActive = model.IsActive == "Y" ? "Yes" : (model.IsActive == "N" ? "No" : model.IsActive)
            }).ToList();

            return result;
        }

        public async Task<GeneralLedgerVM> GetByIdAsync(int id)
        {
            var data = await (from general in _repository.All()
                              join subsidi in _subsidiaryLedger.All()
                                  on general.SubsidiaryLedgerCodeNo equals subsidi.SusidiaryLedgerCodeNo
                              join subcrl in _subcontrolLedger.All()
                                  on subsidi.GeneralLedgerCodeNo equals subcrl.GeneralLedgerCodeNo
                              join crl in _controlLedger.All()
                                  on subcrl.SubControlLedgerCodeNo equals crl.SubControlLedgerCodeNo
                              join grl in _groupLedger.All()
                                  on crl.ControlLedgerCodeNo equals grl.ControlLedgerCodeNo
                              where general.AutoId == id
                              select new GeneralLedgerVM
                              {
                                  autoId = general.AutoId,
                                  SubsidiaryLedgerCodeNo = general.SubsidiaryLedgerCodeNo,
                                  SubSusidiaryLedgerCodeNo = general.SubSusidiaryLedgerCodeNo,
                                  SubSubsidiaryLedgerName = general.SubSubsidiaryLedgerName,
                                  ShortName = general.ShortName,
                                  CashFlowTypeID = general.CashFlowTypeId,
                                  IsActive = general.IsActive,
                                  LDate = general.Ldate,
                                  ModifyDate = general.ModifyDate,

                                  //Group Ledger info
                                  Generalgrlcode = grl.ControlLedgerCodeNo,
                                  GeneralgrlshortName = grl.ShortName,

                                  //Control Ledger Info
                                  GeneralCRLCode = crl.SubControlLedgerCodeNo,
                                  GeneralCRLShortName = crl.ShortName,

                                  //Sub-Control Ledger Info
                                  GeneralSCRLCode = subcrl.GeneralLedgerCodeNo,
                                  GeneralSCRLShortname = subcrl.ShortName,

                                  //Sub-Sidiary Ledger Info
                                  GeneralSSLShortName = subsidi.ShortName

                              }).FirstOrDefaultAsync();

            if (data == null)
            {
                var general = await _repository.GetByIdAsync((decimal)id);
                if (general != null)
                {
                    data = new GeneralLedgerVM
                    {
                        autoId = general.AutoId,
                        SubsidiaryLedgerCodeNo = general.SubsidiaryLedgerCodeNo,
                        SubSusidiaryLedgerCodeNo = general.SubSusidiaryLedgerCodeNo,
                        SubSubsidiaryLedgerName = general.SubSubsidiaryLedgerName,
                        ShortName = general.ShortName,
                        CashFlowTypeID = general.CashFlowTypeId,
                        IsActive = general.IsActive,
                        LDate = general.Ldate,
                        ModifyDate = general.ModifyDate
                    };
                }
            }

            return data;
        }

        //public async Task<string> GetLastGeneralLedgerCodeAsync(string subsidiaryLedgercode)
        //{
        //    if (string.IsNullOrWhiteSpace(subsidiaryLedgercode))
        //        throw new ArgumentException("Sub-Sidiary Ledger Code is required.", nameof(subsidiaryLedgercode));

        //    // Get Code 
        //    var lastCode = await _repository.All()
        //        .Where(x => x.SubsidiaryLedgerCodeNo == subsidiaryLedgercode)
        //        .OrderByDescending(x => x.SubSusidiaryLedgerCodeNo)
        //        .Select(x => x.SubSusidiaryLedgerCodeNo)
        //        .FirstOrDefaultAsync();

        //    string nextCode;

        //    if (!string.IsNullOrEmpty(lastCode))
        //    {
        //        // Extract last 3 digits and increment
        //        if (!int.TryParse(lastCode.Substring(lastCode.Length - 5), out int lastSeq))
        //            throw new Exception($"Invalid SubControlLedgerCode format: {lastCode}");

        //        int newSeq = lastSeq + 1;

        //        // Limit to 99999 per code
        //        if (newSeq > 99999)
        //            throw new Exception($"Maximum Sub-Sidiary LedgerCode (99999) reached for Group {subsidiaryLedgercode}.");


        //        nextCode = subsidiaryLedgercode + newSeq.ToString("D5");
        //    }
        //    else
        //    {
        //        // First entry 
        //        nextCode = subsidiaryLedgercode + "00001";
        //    }

        //    return nextCode;
        //}
        public async Task<string> GetLastGeneralLedgerCodeAsync(string subsidiaryLedgercode)
        {
            if (string.IsNullOrWhiteSpace(subsidiaryLedgercode))
                throw new ArgumentException("Sub-Sidiary Ledger Code is required.", nameof(subsidiaryLedgercode));

            // Use MaxAsync for better performance
            var lastCode = await _repository.All()
                .Where(x => x.SubsidiaryLedgerCodeNo == subsidiaryLedgercode)
                .MaxAsync(x => x.SubSusidiaryLedgerCodeNo);

            int newSeq = 1;

            if (!string.IsNullOrEmpty(lastCode))
            {
                if (!int.TryParse(lastCode.Substring(lastCode.Length - 5), out int lastSeq))
                    throw new Exception($"Invalid SubControlLedgerCode format: {lastCode}");

                newSeq = lastSeq + 1;

                if (newSeq > 99999)
                    throw new Exception($"Maximum Sub-Sidiary LedgerCode (99999) reached for Group {subsidiaryLedgercode}.");
            }

            return subsidiaryLedgercode + newSeq.ToString("D5");
        }


        //public async Task<PaginationService<Acc_SubSubsidiaryLedger, GeneralLedgerVM>.PaginationResult<GeneralLedgerVM>> GetPaginatedAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string sortColumn = "SubSusidiaryLedgerCodeNo", string sortOrder = "desc", string groupLedgerCode = "", string controlLedgerCode = "", string subcontrolLedgerCode = "", string subSididaryLedger = "")
        //{
        //    //var query = _repository.All();
        //    var query = from general in _repository.All()     //General Ledger
        //                join subsidi in _subsidiaryLedger.All()      //Sub-Sidiary Ledger
        //                  on general.SubsidiaryLedgerCodeNo equals subsidi.SusidiaryLedgerCodeNo
        //                join subcrl in _subcontrolLedger.All()      //Sub-Control Ledger
        //                   on subsidi.GeneralLedgerCodeNo equals subcrl.GeneralLedgerCodeNo
        //                join cl in _controlLedger.All()         //Control Ledger
        //                    on subcrl.SubControlLedgerCodeNo equals cl.SubControlLedgerCodeNo
        //                join gr in _groupLedger.All()           //Group Ledger
        //                    on cl.ControlLedgerCodeNo equals gr.ControlLedgerCodeNo
        //                select new { general, subsidi, subcrl, cl, gr};

        //    // Filter by groupLedgerCode
        //    if (!string.IsNullOrEmpty(groupLedgerCode))
        //    {
        //        query = query.Where(x => x.gr.ControlLedgerCodeNo == groupLedgerCode);
        //    }

        //    // Filter by controlLedgerCode 
        //    if (!string.IsNullOrEmpty(controlLedgerCode))
        //    {
        //        query = query.Where(x => x.cl.SubControlLedgerCodeNo == controlLedgerCode);
        //    }
        //    // Filter by SubcontrolLedgerCode 
        //    if (!string.IsNullOrEmpty(subcontrolLedgerCode))
        //    {
        //        query = query.Where(x => x.subcrl.GeneralLedgerCodeNo == subcontrolLedgerCode);
        //    }
        //    // Filter by SubsidiaryCode 
        //    if (!string.IsNullOrEmpty(subSididaryLedger))
        //    {
        //        query = query.Where(x => x.subsidi.SusidiaryLedgerCodeNo == subSididaryLedger);
        //    }

        //    if (pageSize == -1)
        //    {
        //        pageSize = await query.CountAsync();
        //        pageNumber = 1;
        //    }

        //    var paginatedResult = await PaginationService<Acc_SubSubsidiaryLedger, GeneralLedgerVM>.GetPaginatedData(
        //        query.Select(x => x.general), pageNumber, pageSize, searchTerm, sortColumn, sortOrder,
        //        term => sc =>
        //            EF.Functions.Like(sc.SubSusidiaryLedgerCodeNo ?? "", $"%{term}%") ||
        //            EF.Functions.Like(sc.SubSubsidiaryLedgerName ?? "", $"{term}%") ||
        //            EF.Functions.Like( _cashFlow.All()  
        //                                        .Where(c => c.CashFlowTypeID == sc.CashFlowTypeID)
        //                                        .Select(c => c.CashFlowTypeName)
        //                                        .FirstOrDefault()
        //                                        ?? "", $"{term}%") ||
        //            EF.Functions.Like(sc.IsActive ?? "", $"%{term}%"),
        //        pt => new GeneralLedgerVM
        //        {
        //            autoId = pt.autoId,
        //            SubsidiaryLedgerCodeNo = pt.SubsidiaryLedgerCodeNo,
        //            SubSusidiaryLedgerCodeNo = pt.SubSusidiaryLedgerCodeNo,
        //            SubSubsidiaryLedgerName = pt.SubSubsidiaryLedgerName,
        //            IsActive = pt.IsActive,
        //            LIP = pt.LIP,
        //            LMAC = pt.LMAC,
        //            LDate = pt.LDate,
        //            ModifyDate = pt.ModifyDate,
        //            CashFlowTypeName = _cashFlow.All()  
        //            .Where(c => c.CashFlowTypeID == pt.CashFlowTypeID)
        //            .Select(c => c.CashFlowTypeName)
        //            .FirstOrDefault()


        //        });

        //    return paginatedResult;
        //}

        public async Task<PaginationService<AccSubSubsidiaryLedger, GeneralLedgerVM>.PaginationResult<GeneralLedgerVM>> GetPaginatedAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string sortColumn = "SubSusidiaryLedgerCodeNo", string sortOrder = "desc", string groupLedgerCode = "", string controlLedgerCode = "", string subcontrolLedgerCode = "", string subSididaryLedger = "")
        {

            var query = from general in _repository.All()
                        join subsidi in _subsidiaryLedger.All()
                            on general.SubsidiaryLedgerCodeNo equals subsidi.SusidiaryLedgerCodeNo
                        join subcrl in _subcontrolLedger.All()
                            on subsidi.GeneralLedgerCodeNo equals subcrl.GeneralLedgerCodeNo
                        join cl in _controlLedger.All()
                            on subcrl.SubControlLedgerCodeNo equals cl.SubControlLedgerCodeNo
                        join gr in _groupLedger.All()
                            on cl.ControlLedgerCodeNo equals gr.ControlLedgerCodeNo
                        select new
                        {
                            general,
                            subsidi,
                            subcrl,
                            cl,
                            gr,
                            SubSubsidiaryLedgerFullName = general.SubSubsidiaryLedgerName + " - " + subcrl.GeneralLedgerName,
                            CashFlowTypeName = _cashFlow.All()
                                        .Where(cf => cf.CashFlowTypeId == general.CashFlowTypeId)
                                        .Select(cf => cf.CashFlowTypeName)
                                        .FirstOrDefault()
                        };

            // Filter by groupLedgerCode
            if (!string.IsNullOrEmpty(groupLedgerCode))
            {
                query = query.Where(x => x.gr.ControlLedgerCodeNo == groupLedgerCode);
            }

            // Filter by controlLedgerCode
            if (!string.IsNullOrEmpty(controlLedgerCode))
            {
                query = query.Where(x => x.cl.SubControlLedgerCodeNo == controlLedgerCode);
            }

            // Filter by subcontrolLedgerCode
            if (!string.IsNullOrEmpty(subcontrolLedgerCode))
            {
                query = query.Where(x => x.subcrl.GeneralLedgerCodeNo == subcontrolLedgerCode);
            }

            // Filter by Subsidiary Ledger Code
            if (!string.IsNullOrEmpty(subSididaryLedger))
            {
                query = query.Where(x => x.subsidi.SusidiaryLedgerCodeNo == subSididaryLedger);
            }


            if (pageSize == -1)
            {
                pageSize = await query.CountAsync();
                pageNumber = 1;
            }

            // Pagination Service 
            var paginatedResult = await PaginationService<AccSubSubsidiaryLedger, GeneralLedgerVM>.GetPaginatedData(
                query.Select(x => x.general),
                pageNumber,
                pageSize,
                searchTerm,
                sortColumn,
                sortOrder,
                term => sc =>
                    EF.Functions.Like(sc.SubSusidiaryLedgerCodeNo ?? "", $"%{term}%") ||
                    EF.Functions.Like(sc.SubSubsidiaryLedgerName ?? "", $"{term}%") ||
                    EF.Functions.Like(  _cashFlow.All().Where(c => c.CashFlowTypeId == sc.CashFlowTypeId).Select(c => c.CashFlowTypeName).FirstOrDefault() ?? "", $"{term}%") ||
                    EF.Functions.Like(sc.IsActive ?? "", $"%{term}%"),
                pt => new GeneralLedgerVM
                {
                    autoId = pt.AutoId,
                    SubsidiaryLedgerCodeNo = pt.SubsidiaryLedgerCodeNo,
                    SubSusidiaryLedgerCodeNo = pt.SubSusidiaryLedgerCodeNo,
                    SubSubsidiaryLedgerName = query.Where(x => x.general.SubSusidiaryLedgerCodeNo == pt.SubSusidiaryLedgerCodeNo).Select(x => x.SubSubsidiaryLedgerFullName).FirstOrDefault(),
                    IsActive = pt.IsActive,
                    LIP = pt.Lip,
                    LMAC = pt.Lmac,
                    LDate = pt.Ldate,
                    ModifyDate = pt.ModifyDate,
                    CashFlowTypeName = query.Where(x => x.general.CashFlowTypeId == pt.CashFlowTypeId).Select(x => x.CashFlowTypeName).FirstOrDefault(),
                });

            return paginatedResult;
        }


        public async Task<bool> IsDuplicateAsync(GeneralLedgerVM model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var alldata = await _repository.All().ToListAsync();

            var isDuplicate = alldata.Any(pm =>
            string.Equals(pm.SubSubsidiaryLedgerName, model.SubSubsidiaryLedgerName, StringComparison.OrdinalIgnoreCase) &&
             pm.AutoId != model.autoId

            );

            return isDuplicate;
        }

        public async Task<bool> IsExistAsync(int id)
        {
            return await _repository.All().AnyAsync(x => x.AutoId == id);
        }

        public async Task<bool> SaveAsync(GeneralLedgerVM model)
        {
            try
            {
                //if (model.IsActive != "Y" && model.IsActive != "N")
                //{
                //    throw new InvalidOperationException("Invalid value for IsActive. Must be 'Y' or 'N'.");
                //}
                var entity = new AccSubSubsidiaryLedger
                {
                    SubsidiaryLedgerCodeNo = model.SubsidiaryLedgerCodeNo,
                    SubSusidiaryLedgerCodeNo = model.SubSusidiaryLedgerCodeNo,
                    SubSubsidiaryLedgerName = model.SubSubsidiaryLedgerName,
                    ShortName = model.ShortName ?? "",
                    OpeningBalance = 0,
                    TrType = "",
                    IsActive = model.IsActive,
                    CashFlowTypeId = model.CashFlowTypeID,
                    CostCenterCodeNo = model.CostCenterCodeNo ?? "",
                    Luser = model.LUser,
                    Ldate = DateTime.Now,
                    Lip = GetLocalIP() ?? "",
                    Lmac = GetMacAddress() ?? ""
                };

                await _repository.AddAsync(entity);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Insertion Failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateAsync(GeneralLedgerVM model)
        {
            await _repository.BeginTransactionAsync();
            try
            {
                var entity = await _repository.GetByIdAsync(model.autoId);
                if (entity == null)
                {
                    await _repository.RollbackTransactionAsync();
                    return false;
                }

                entity.SubsidiaryLedgerCodeNo = model.SubsidiaryLedgerCodeNo;
                entity.SubSusidiaryLedgerCodeNo = model.SubSusidiaryLedgerCodeNo;
                entity.SubSubsidiaryLedgerName = model.SubSubsidiaryLedgerName;
                entity.ShortName = model.ShortName ?? "";
                entity.OpeningBalance = 0;
                entity.TrType = "";
                entity.IsActive = model.IsActive;
                entity.CashFlowTypeId = model.CashFlowTypeID;
                entity.CostCenterCodeNo = model.CostCenterCodeNo;
                entity.Luser = model.LUser;
                entity.ModifyDate = DateTime.Now;
                entity.Lip = GetLocalIP() ?? "";
                entity.Lmac = GetMacAddress() ?? "";

                await _repository.UpdateAsync(entity);
                await _repository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                await _repository.RollbackTransactionAsync();
                return false;
            }
        }

        #region Sub-Sidiary Ledger Dropdown

        //Sub-Sidiary Ledger 
        public async Task<List<CommonChoiceVM>> SubSidiaryLedgerDropdown(string subcontrolcode)
        {
            var SubcontrolLedger = await _subsidiaryLedger.All()
                .Where(subc => subc.GeneralLedgerCodeNo == subcontrolcode)
                .Select(e => new CommonChoiceVM
                {
                    Id = e.SusidiaryLedgerCodeNo,
                    Name = e.SubsidiaryLedgerName,
                }).ToListAsync();

            return SubcontrolLedger;
        }

        //Sub-Sidiary Ledger Info
        public async Task<AccSubsidiaryLedger> SubSidiaryLedgerInfo(string subsidiarycode)
        {

            var subsidiaryLedger = await _subsidiaryLedger.All()
           .Where(e => e.SusidiaryLedgerCodeNo == subsidiarycode)
           .Select(e => new AccSubsidiaryLedger
           {

               SusidiaryLedgerCodeNo = e.SusidiaryLedgerCodeNo,
               SubsidiaryLedgerName = e.SubsidiaryLedgerName,
               ShortName = e.ShortName
           })
           .FirstOrDefaultAsync();

            return subsidiaryLedger;
        }
        #endregion


        #region Cash Flow Dropdown

        public async Task<List<CommonChoiceVM>> CashFlowDropdown()
        {
            var SubcontrolLedger = await _cashFlow.All()
                .Select(e => new CommonChoiceVM
                {
                    Id = e.CashFlowTypeId,
                    Name = e.CashFlowTypeName,
                }).ToListAsync();

            return SubcontrolLedger;
        }

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
