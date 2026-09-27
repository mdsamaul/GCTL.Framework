using GCTL.Core.Data;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.ChartOfAccount;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace GCTL.Service.ChartOfAccounts.SubSidiaryLedger
{
    public class SubSidiaryLedgerService : AppService<AccSubsidiaryLedger>, ISubSidiaryLedger
    {
        private readonly IRepository<AccGeneralLedger> _subcontrolLedger;
        private readonly IRepository<AccSubControlLedger> _controlLedger;
        private readonly IRepository<AccControlLedger> _groupLedger;
        private readonly IRepository<AccSubsidiaryLedger> _repository;

        public SubSidiaryLedgerService(IRepository<AccGeneralLedger> subcontrolLedger, IRepository<AccSubControlLedger> controlLedger, IRepository<AccControlLedger> groupLedger, IRepository<AccSubsidiaryLedger> genericRepository) : base(genericRepository)
        {
            _repository = genericRepository;
            _controlLedger = controlLedger;
            _groupLedger = groupLedger;
            _subcontrolLedger = subcontrolLedger;
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

        public async Task<List<SubsidiaryLedgerVM>> GetAllAsync(string groupLedgerCode = "", string controlLedgerCode = "", string subcontrolLedgerCode = "", string subSididaryLedger = "")
        {
            IQueryable<AccSubsidiaryLedger> subslQuery;

            if (string.IsNullOrEmpty(groupLedgerCode) &&
                string.IsNullOrEmpty(controlLedgerCode) &&
                string.IsNullOrEmpty(subcontrolLedgerCode) &&
                string.IsNullOrEmpty(subSididaryLedger))
            {
                subslQuery = _repository.All();
            }
            else
            {
                var query = from subsl in _repository.All()
                            join subcrl in _subcontrolLedger.All() on subsl.GeneralLedgerCodeNo equals subcrl.GeneralLedgerCodeNo
                            join cl in _controlLedger.All() on subcrl.SubControlLedgerCodeNo equals cl.SubControlLedgerCodeNo
                            join gr in _groupLedger.All() on cl.ControlLedgerCodeNo equals gr.ControlLedgerCodeNo
                            select new { subsl, subcrl, cl, gr };

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
                    query = query.Where(x => x.subsl.GeneralLedgerCodeNo == subcontrolLedgerCode);
                }

                if (!string.IsNullOrEmpty(subSididaryLedger))
                {
                    query = query.Where(x => x.subsl.SusidiaryLedgerCodeNo == subSididaryLedger);
                }

                subslQuery = query.Select(x => x.subsl);
            }

            var entity = await subslQuery.ToListAsync();

            var result = entity.Select(model => new SubsidiaryLedgerVM
            {
                autoId = model.AutoId,
                SusidiaryLedgerCodeNo = model.SusidiaryLedgerCodeNo,
                SubsidiaryLedgerName = model.SubsidiaryLedgerName,
                ShortName = model.ShortName
            }).ToList();

            return result;
        }

        public async Task<SubsidiaryLedgerVM> GetByIdAsync(int id)
        {
            var data = await (from subsidi in _repository.All()
                              join subcrl in _subcontrolLedger.All()
                                  on subsidi.GeneralLedgerCodeNo equals subcrl.GeneralLedgerCodeNo
                              join crl in _controlLedger.All()
                                  on subcrl.SubControlLedgerCodeNo equals crl.SubControlLedgerCodeNo
                              join grl in _groupLedger.All()
                                  on crl.ControlLedgerCodeNo equals grl.ControlLedgerCodeNo
                              where subsidi.AutoId == id
                              select new SubsidiaryLedgerVM
                              {
                                  autoId = subsidi.AutoId,
                                  GeneralLedgerCodeNo = subsidi.GeneralLedgerCodeNo,
                                  SusidiaryLedgerCodeNo = subsidi.SusidiaryLedgerCodeNo,
                                  SubsidiaryLedgerName = subsidi.SubsidiaryLedgerName,
                                  ShortName = subsidi.ShortName,
                                  LDate = subsidi.Ldate,
                                  ModifyDate = subsidi.ModifyDate,

                                  //Group Ledger info
                                  GLCode = grl.ControlLedgerCodeNo,
                                  GLShortName = grl.ShortName,

                                  //Control Ledger Info
                                  CRLCode = crl.SubControlLedgerCodeNo,
                                  CRLShortName = crl.ShortName,

                                  //Sub-Control Ledger Info
                                  SubCRLShortName = subcrl.ShortName

                              }).FirstOrDefaultAsync();

            return data;
        }


        //public async Task<string> GetLastSubSidiaryLedgerCodeAsync(string subcontrolLedgercode)
        //{
        //    if (string.IsNullOrWhiteSpace(subcontrolLedgercode))
        //        throw new ArgumentException("Sub-Control Ledger Code is required.", nameof(subcontrolLedgercode));

        //    // Get Code 
        //    var lastCode = await _repository.All()
        //        .Where(x => x.GeneralLedgerCodeNo == subcontrolLedgercode)
        //        .OrderByDescending(x => x.SusidiaryLedgerCodeNo)
        //        .Select(x => x.SusidiaryLedgerCodeNo)
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
        //            throw new Exception($"Maximum Sub-Sidiary LedgerCode (99999) reached for Group {subcontrolLedgercode}.");


        //        nextCode = subcontrolLedgercode + newSeq.ToString("D5");
        //    }
        //    else
        //    {
        //        // First entry 
        //        nextCode = subcontrolLedgercode + "00001";
        //    }

        //    return nextCode;
        //}

        public async Task<string> GetLastSubSidiaryLedgerCodeAsync(string subcontrolLedgercode)
        {
            if (string.IsNullOrWhiteSpace(subcontrolLedgercode))
                throw new ArgumentException("Sub-Control Ledger Code is required.", nameof(subcontrolLedgercode));

            // Use MaxAsync for performance
            var lastCode = await _repository.All()
                .Where(x => x.GeneralLedgerCodeNo == subcontrolLedgercode)
                .MaxAsync(x => x.SusidiaryLedgerCodeNo);

            int newSeq = 1;

            if (!string.IsNullOrEmpty(lastCode))
            {
                if (!int.TryParse(lastCode.Substring(lastCode.Length - 5), out int lastSeq))
                    throw new Exception($"Invalid SubControlLedgerCode format: {lastCode}");

                newSeq = lastSeq + 1;

                if (newSeq > 99999)
                    throw new Exception($"Maximum Sub-Sidiary LedgerCode (99999) reached for Group {subcontrolLedgercode}.");
            }

            return subcontrolLedgercode + newSeq.ToString("D5");
        }


        public async Task<PaginationService<AccSubsidiaryLedger, SubsidiaryLedgerVM>.PaginationResult<SubsidiaryLedgerVM>> GetPaginatedAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string sortColumn = "SusidiaryLedgerCodeNo", string sortOrder = "desc", string groupLedgerCode = "", string controlLedgerCode = "", string subcontrolLedgerCode = "")
        {
            //var query = _repository.All();
            var query = from subsl in _repository.All() // Sub-Sidiary Ledger
                        join subcrl in _subcontrolLedger.All() // Sub-Control Ledger
                           on subsl.GeneralLedgerCodeNo equals subcrl.GeneralLedgerCodeNo
                        join cl in _controlLedger.All() // Control Ledger
                            on subcrl.SubControlLedgerCodeNo equals cl.SubControlLedgerCodeNo
                        join gr in _groupLedger.All() // Group Ledger
                            on cl.ControlLedgerCodeNo equals gr.ControlLedgerCodeNo
                        select new { subsl, subcrl, cl, gr };

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
            // Filter by controlLedgerCode 
            if (!string.IsNullOrEmpty(subcontrolLedgerCode))
            {
                query = query.Where(x => x.subsl.GeneralLedgerCodeNo == subcontrolLedgerCode);
            }

            if (pageSize == -1)
            {
                pageSize = await query.CountAsync();
                pageNumber = 1;
            }

            var paginatedResult = await PaginationService<AccSubsidiaryLedger, SubsidiaryLedgerVM>.GetPaginatedData(
                query.Select(x => x.subsl), pageNumber, pageSize, searchTerm, sortColumn, sortOrder,
                term => sc =>
                    EF.Functions.Like(sc.SusidiaryLedgerCodeNo ?? "", $"%{term}%") ||
                    EF.Functions.Like(sc.SubsidiaryLedgerName ?? "", $"{term}%") ||
                    EF.Functions.Like(sc.ShortName ?? "", $"%{term}%"),
                pt => new SubsidiaryLedgerVM
                {
                    autoId = pt.AutoId,
                    GeneralLedgerCodeNo = pt.GeneralLedgerCodeNo,
                    SusidiaryLedgerCodeNo = pt.SusidiaryLedgerCodeNo,
                    SubsidiaryLedgerName = pt.SubsidiaryLedgerName,
                    ShortName = pt.ShortName,
                    LIP = pt.Lip,
                    LMAC = pt.Lmac,
                    LDate = pt.Ldate,
                    ModifyDate = pt.ModifyDate,
                });

            return paginatedResult;
        }

        public async Task<bool> IsDuplicateAsync(SubsidiaryLedgerVM model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var alldata = await _repository.All().ToListAsync();

            var isDuplicate = alldata.Any(pm =>
            string.Equals(pm.SubsidiaryLedgerName, model.SubsidiaryLedgerName, StringComparison.OrdinalIgnoreCase) &&
             pm.AutoId != model.autoId

            );

            return isDuplicate;
        }

        public async Task<bool> IsExistAsync(int id)
        {
            return await _repository.All().AnyAsync(x => x.AutoId == id);
        }

        public async Task<bool> SaveAsync(SubsidiaryLedgerVM model)
        {
            try
            {
                var entity = new AccSubsidiaryLedger
                {
                    GeneralLedgerCodeNo = model.GeneralLedgerCodeNo,
                    SusidiaryLedgerCodeNo = model.SusidiaryLedgerCodeNo,
                    SubsidiaryLedgerName = model.SubsidiaryLedgerName,
                    ShortName = model.ShortName ?? "",
                    IsSameNameSl = "",
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

        public async Task<bool> UpdateAsync(SubsidiaryLedgerVM model)
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

                entity.GeneralLedgerCodeNo = model.GeneralLedgerCodeNo;
                entity.SusidiaryLedgerCodeNo = model.SusidiaryLedgerCodeNo;
                entity.SubsidiaryLedgerName = model.SubsidiaryLedgerName;
                entity.ShortName = model.ShortName ?? "";
                entity.IsSameNameSl = "";
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


        #region Sub-Control Ledger Dropdown

        //Sub-Control Ledger 
        public async Task<List<CommonChoiceVM>> SubControlLedgerDropdown(string controlcode)
        {
            var SubcontrolLedger = await _subcontrolLedger.All()
                .Where(c => c.SubControlLedgerCodeNo == controlcode)
                .Select(e => new CommonChoiceVM
                {
                    Id = e.GeneralLedgerCodeNo,
                    Name = e.GeneralLedgerName,
                }).ToListAsync();
            
            return SubcontrolLedger;
        }

        //Sub-Control Ledger Info
        public async Task<AccGeneralLedger> SubControlLedgerInfo(string subcontrlcode)
        {
          
         var subcontrolLedger = await _subcontrolLedger.All()
        .Where(e => e.GeneralLedgerCodeNo == subcontrlcode)
        .Select(e => new AccGeneralLedger
        {

            GeneralLedgerCodeNo = e.GeneralLedgerCodeNo,
            GeneralLedgerName = e.GeneralLedgerName,
            ShortName = e.ShortName
        })
        .FirstOrDefaultAsync();

            return subcontrolLedger;
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
