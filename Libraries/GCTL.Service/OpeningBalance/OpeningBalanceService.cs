using GCTL.Core.Data;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.OpeningBalance;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace GCTL.Service.OpeningBalance
{
    public class OpeningBalanceService : AppService<AccCompanyOpeningBalance>, IOpeningBalance
    {
        private readonly IRepository<AccCompanyOpeningBalance> _repository;
        private readonly IRepository<CoreCompany> _company;
        private readonly IRepository<CoreBranch> _branch;
        private readonly IRepository<AccSubSubsidiaryLedger> _generalLedger; 
        private readonly IRepository<AccSubsidiaryLedger> _subsidiary;
        private readonly IRepository<AccGeneralLedger> _subcontrol;
        private readonly IRepository<SalesCustomer> _salesCustomer;
        public OpeningBalanceService(
            IRepository<AccCompanyOpeningBalance> genericRepository, 
            IRepository<CoreCompany> company, 
            IRepository<CoreBranch> branch,
            IRepository<AccSubSubsidiaryLedger> generalLedger,
            IRepository<SalesCustomer> salesCustomer,
            IRepository<AccSubsidiaryLedger> subsidiary,
            IRepository<AccGeneralLedger> subcontrol
            )
    : base(genericRepository)
        {
            _repository = genericRepository;
            _company = company;
            _branch = branch;
            _generalLedger = generalLedger;
            _salesCustomer = salesCustomer;
            _subsidiary = subsidiary;
            _subcontrol = subcontrol;
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
                Console.WriteLine($"Bulk delete error: {ex}");
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
                Console.WriteLine($"Bulk delete error: {ex}");
                return (false);
            }
        }

        public async Task<List<OpeningBalanceVM>> GetAllAsync()
        {
            var entity = await _repository.All().ToListAsync();

            var result = entity.Select(model => new OpeningBalanceVM
            {
                autoId = model.AutoId,
                ComOpeningBalanceCode = model.ComOpeningBalanceCode,
                CompanyCode = model.CompanyCode,
                BranchCode = model.BranchCode,
                SubSusidiaryLedgerCodeNo = model.SubSusidiaryLedgerCodeNo,
                OpeningBalance = model.OpeningBalance,
                TrType = model.TrType,
                OpeningDate = model.OpeningDate
            }).ToList();

            return result;
        }

        public async Task<OpeningBalanceVM> GetByIdAsync(int id)
        {
            var data = await (from openbalance in _repository.All().AsNoTracking()
                              join company in _company.All().AsNoTracking()
                                  on openbalance.CompanyCode equals company.CompanyCode into compJoin
                              from company in compJoin.DefaultIfEmpty()
                              join branch in _branch.All().AsNoTracking()
                                  on openbalance.BranchCode equals branch.BranchCode into branchJoin
                              from branch in branchJoin.DefaultIfEmpty()
                              join accounthead in _generalLedger.All().AsNoTracking()
                                  on openbalance.SubSusidiaryLedgerCodeNo equals accounthead.SubSusidiaryLedgerCodeNo into ahJoin
                              from accounthead in ahJoin.DefaultIfEmpty()
                              where openbalance.AutoId == id
                              select new OpeningBalanceVM
                              {
                                  autoId = openbalance.AutoId,
                                  ComOpeningBalanceCode = openbalance.ComOpeningBalanceCode,                                
                                  OpeningBalance = openbalance.OpeningBalance,
                                  TrType = openbalance.TrType,
                                  OpeningDate = openbalance.OpeningDate,
                                  LDate = openbalance.Ldate,
                                  ModifyDate = openbalance.ModifyDate,
                                  CompanyCode = company != null ? company.CompanyCode : openbalance.CompanyCode,
                                  BranchCode = branch != null ? branch.BranchCode : openbalance.BranchCode,
                                  CompanyName = company != null ? company.CompanyName : null,
                                  BranchName = branch != null ? branch.BranchName : null,
                                  SubSusidiaryLedgerCodeNo = openbalance.SubSusidiaryLedgerCodeNo,
                                  AccountHead = accounthead != null ? accounthead.SubSubsidiaryLedgerName : null
                              }).FirstOrDefaultAsync();

            if (data != null && !string.IsNullOrEmpty(data.AccountHead) && data.AccountHead.StartsWith("CUS", StringComparison.OrdinalIgnoreCase))
            {
                var customer = await _salesCustomer.All().AsNoTracking()
                    .Where(c => c.CustomerId == data.AccountHead)
                    .Select(c => new { c.CustomerId, c.CustomerName })
                    .FirstOrDefaultAsync();
                if (customer != null && !string.IsNullOrEmpty(customer.CustomerName))
                {
                    data.AccountHead = $"{customer.CustomerName} ({data.AccountHead})";
                }
            }

            return data;
        }


        public async Task<string> GetLastVoucherTypeCodeAsync()
        {
            var allCodes = await _repository.All()
                                 .Select(c => c.ComOpeningBalanceCode)
                                 .ToListAsync();

            int maxCode = 0;

            foreach (var code in allCodes)
            {
                if (int.TryParse(code, out int numericCode))
                {
                    if (numericCode > maxCode)
                        maxCode = numericCode;
                }
            }

            int nextCode = maxCode + 1;

            return nextCode.ToString();
        }

        //public async Task<string> GetLastOpeiningBalanceCodeAsync()
        //{
           
        //    var last = await _repository.All()
        //        .OrderByDescending(c => c.ComOpeningBalanceCode)
        //        .FirstOrDefaultAsync();

        //    int nextCode = 1;

        //    if (last != null && int.TryParse(last.ComOpeningBalanceCode, out int lastCode))
        //    {
        //        nextCode = lastCode + 1;
        //    }

        //    string formattedCode = nextCode.ToString("D8");

        //    return formattedCode;
        //}
        public async Task<string> GetLastOpeiningBalanceCodeAsync()
        {
            var lastCodeString = await _repository.All()
                .MaxAsync(c => c.ComOpeningBalanceCode);

            long nextCode = 1;

            if (!string.IsNullOrEmpty(lastCodeString) && long.TryParse(lastCodeString, out long lastCode))
            {
                nextCode = lastCode + 1;
            }

            return nextCode.ToString("D8");
        }


        //public async Task<PaginationService<Acc_Company_Opening_Balance, OpeningBalanceVM>.PaginationResult<OpeningBalanceVM>> GetPaginatedAsync(int pageNumber = 1, int pageSize = 5, string searchTerm = "", string sortColumn = "ComOpeningBalanceCode", string sortOrder = "desc")
        //{
        //    var query = from ob in _repository.All()
        //                join c in _company.All() on ob.CompanyCode equals c.CompanyCode into comp
        //                from company in comp.DefaultIfEmpty() // left join
        //                join b in _branch.All() on new { ob.CompanyCode, ob.BranchCode } equals new { b.CompanyCode, b.BranchCode } into br
        //                from branch in br.DefaultIfEmpty() // left join
        //                join gl in _generalLedger.All() on ob.SubSusidiaryLedgerCodeNo equals gl.SubSusidiaryLedgerCodeNo into gls
        //                from ledger in gls.DefaultIfEmpty() // left join
        //                select new
        //                {
        //                    ob,
        //                    CompanyName = company != null ? company.CompanyName : "",
        //                    BranchName = branch != null ? branch.BranchName : "",
        //                    AccountHead = ledger != null ? ledger.SubSubsidiaryLedgerName : ""
        //                };

        //    if (pageSize == -1)
        //    {
        //        pageSize = await query.CountAsync();
        //        pageNumber = 1;
        //    }

        //    var paginatedResult = await PaginationService<Acc_Company_Opening_Balance, OpeningBalanceVM>.GetPaginatedData(query.Select(x => x.ob), pageNumber, pageSize, searchTerm, sortColumn, sortOrder,
        //        term => sc =>
        //            EF.Functions.Like(sc.CompanyCode ?? "", $"%{term}%") ||
        //            EF.Functions.Like(sc.BranchCode ?? "", $"{term}%") ||
        //            EF.Functions.Like(sc.SubSusidiaryLedgerCodeNo ?? "", $"%{term}%"),
        //        pt => new OpeningBalanceVM
        //        {
        //            autoId = pt.autoId,
        //            ComOpeningBalanceCode = pt.ComOpeningBalanceCode,
        //            CompanyCode = pt.CompanyCode,
        //            BranchCode = pt.BranchCode,
        //            SubSusidiaryLedgerCodeNo = pt.SubSusidiaryLedgerCodeNo,
        //            OpeningBalance = pt.OpeningBalance,
        //            TrType = pt.TrType,
        //            LIP = pt.LIP,
        //            LMAC = pt.LMAC,
        //            LDate = pt.LDate,
        //            ModifyDate = pt.ModifyDate,

        //            CompanyName = query.Where(x => x.ob.CompanyCode == pt.CompanyCode).Select(x => x.CompanyName).FirstOrDefault(),
        //            BranchName = query.Where(x => x.ob.BranchCode == pt.BranchCode).Select(x => x.BranchName).FirstOrDefault(),
        //            AccountHead = query.Where(x => x.ob.SubSusidiaryLedgerCodeNo == pt.SubSusidiaryLedgerCodeNo).Select(x => x.AccountHead).FirstOrDefault()
        //        });

        //    return paginatedResult;
        //}


        public async Task<PaginationService<OpeningBalanceVM, OpeningBalanceVM>.PaginationResult<OpeningBalanceVM>> GetPaginatedAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = "", string sortColumn = "ComOpeningBalanceCode", string sortOrder = "desc")
        {
            var baseQuery = from ob in _repository.All().AsNoTracking()
                            join c in _company.All().AsNoTracking() on ob.CompanyCode equals c.CompanyCode into compJoin
                            from comp in compJoin.DefaultIfEmpty()
                            join b in _branch.All().AsNoTracking() on ob.BranchCode equals b.BranchCode into branchJoin
                            from br in branchJoin.DefaultIfEmpty()
                            join gl in _generalLedger.All().AsNoTracking() on ob.SubSusidiaryLedgerCodeNo equals gl.SubSusidiaryLedgerCodeNo into ledgerJoin
                            from gl in ledgerJoin.DefaultIfEmpty()
                            select new
                            {
                                ob.AutoId,
                                ob.ComOpeningBalanceCode,
                                ob.CompanyCode,
                                ob.BranchCode,
                                ob.SubSusidiaryLedgerCodeNo,
                                ob.OpeningBalance,
                                ob.TrType,
                                ob.OpeningDate,
                                ob.Lip,
                                ob.Lmac,
                                ob.Ldate,
                                ob.ModifyDate,
                                CompanyName = comp != null ? comp.CompanyName : null,
                                BranchName = br != null ? br.BranchName : null,
                                LedgerName = gl != null ? gl.SubSubsidiaryLedgerName : null
                            };

            int totalItems;
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                baseQuery = baseQuery.Where(x =>
                    (x.ComOpeningBalanceCode != null && EF.Functions.Like(x.ComOpeningBalanceCode, $"%{term}%")) ||
                    (x.CompanyName != null && EF.Functions.Like(x.CompanyName, $"%{term}%")) ||
                    (x.BranchName != null && EF.Functions.Like(x.BranchName, $"%{term}%")) ||
                    (x.TrType != null && EF.Functions.Like(x.TrType, $"%{term}%")) ||
                    (x.LedgerName != null && EF.Functions.Like(x.LedgerName, $"%{term}%"))
                );
                totalItems = await baseQuery.CountAsync();
            }
            else
            {
                // Instant count on single table in 1-2 ms without any joins
                totalItems = await _repository.All().AsNoTracking().CountAsync();
            }

            bool isAsc = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase);
            var sortedQuery = (sortColumn?.ToLower()) switch
            {
                "comopeningbalancecode" => isAsc ? baseQuery.OrderBy(x => x.ComOpeningBalanceCode) : baseQuery.OrderByDescending(x => x.ComOpeningBalanceCode),
                "openingdate" => isAsc ? baseQuery.OrderBy(x => x.OpeningDate) : baseQuery.OrderByDescending(x => x.OpeningDate),
                "openingdatestr" => isAsc ? baseQuery.OrderBy(x => x.OpeningDate) : baseQuery.OrderByDescending(x => x.OpeningDate),
                "accounthead" => isAsc ? baseQuery.OrderBy(x => x.LedgerName) : baseQuery.OrderByDescending(x => x.LedgerName),
                "openingbalance" => isAsc ? baseQuery.OrderBy(x => x.OpeningBalance) : baseQuery.OrderByDescending(x => x.OpeningBalance),
                "trtype" => isAsc ? baseQuery.OrderBy(x => x.TrType) : baseQuery.OrderByDescending(x => x.TrType),
                "companyname" => isAsc ? baseQuery.OrderBy(x => x.CompanyName) : baseQuery.OrderByDescending(x => x.CompanyName),
                "branchname" => isAsc ? baseQuery.OrderBy(x => x.BranchName) : baseQuery.OrderByDescending(x => x.BranchName),
                "autoid" => isAsc ? baseQuery.OrderBy(x => x.AutoId) : baseQuery.OrderByDescending(x => x.AutoId),
                _ => isAsc ? baseQuery.OrderBy(x => x.AutoId) : baseQuery.OrderByDescending(x => x.AutoId)
            };

            if (pageSize == -1)
            {
                pageSize = totalItems > 0 ? totalItems : 10;
                pageNumber = 1;
            }
            if (pageSize <= 0) pageSize = 10;
            if (pageNumber < 1) pageNumber = 1;

            var pagedItems = await sortedQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Resolve customer names in memory for just the 10 rows on this page (if any are CUS ledgers)
            var cusCodes = pagedItems
                .Where(x => !string.IsNullOrEmpty(x.LedgerName) && x.LedgerName.StartsWith("CUS", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.LedgerName!)
                .Distinct()
                .ToList();

            Dictionary<string, string> customerMap = new();
            if (cusCodes.Count > 0)
            {
                var customers = await _salesCustomer.All().AsNoTracking()
                    .Where(c => cusCodes.Contains(c.CustomerId))
                    .Select(c => new { c.CustomerId, c.CustomerName })
                    .ToListAsync();

                foreach (var c in customers)
                {
                    if (!string.IsNullOrEmpty(c.CustomerId) && !customerMap.ContainsKey(c.CustomerId))
                    {
                        customerMap[c.CustomerId] = c.CustomerName ?? c.CustomerId;
                    }
                }
            }

            var data = pagedItems.Select(x =>
            {
                string trType = x.TrType == "Dr" || x.TrType == "Debit" ? "Debit" : (x.TrType == "Cr" || x.TrType == "Credit" ? "Credit" : (x.TrType ?? ""));
                string ledgerName = x.LedgerName ?? "";
                string accountHead = ledgerName;
                if (ledgerName.StartsWith("CUS", StringComparison.OrdinalIgnoreCase) && customerMap.TryGetValue(ledgerName, out var cName))
                {
                    accountHead = $"{cName} ({ledgerName})";
                }

                return new OpeningBalanceVM
                {
                    autoId = x.AutoId,
                    ComOpeningBalanceCode = x.ComOpeningBalanceCode,
                    CompanyCode = x.CompanyCode,
                    BranchCode = x.BranchCode,
                    SubSusidiaryLedgerCodeNo = x.SubSusidiaryLedgerCodeNo,
                    OpeningBalance = x.OpeningBalance,
                    TrType = trType,
                    OpeningDate = x.OpeningDate,
                    LIP = x.Lip,
                    LMAC = x.Lmac,
                    LDate = x.Ldate,
                    ModifyDate = x.ModifyDate,
                    CompanyName = x.CompanyName,
                    BranchName = x.BranchName,
                    AccountHead = accountHead
                };
            }).ToList();

            int totalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalItems / pageSize) : 1;
            int startItem = totalItems == 0 ? 0 : (pageNumber - 1) * pageSize + 1;
            int endItem = Math.Min(pageNumber * pageSize, totalItems);

            return new PaginationService<OpeningBalanceVM, OpeningBalanceVM>.PaginationResult<OpeningBalanceVM>
            {
                Data = data,
                TotalCount = totalItems,
                PaginationInfo = new PaginationService<OpeningBalanceVM, OpeningBalanceVM>.PaginationInfo
                {
                    StartItem = startItem,
                    EndItem = endItem,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    CurrentPage = pageNumber,
                    PageNumbers = Enumerable.Range(1, Math.Max(1, totalPages)).ToList()
                }
            };
        }


        public async Task<bool> IsExistAsync(int id)
        {
            return await _repository.All().AnyAsync(x => x.AutoId == id);
        }

        public async Task<bool> SaveAsync(OpeningBalanceVM model)
        {
            try
            {
                string newBalanceCode = await GetLastOpeiningBalanceCodeAsync();

                var entity = new AccCompanyOpeningBalance
                {
                    ComOpeningBalanceCode = newBalanceCode,
                    CompanyCode = model.CompanyCode,
                    BranchCode = model.BranchCode,
                    SubSusidiaryLedgerCodeNo = model.SubSusidiaryLedgerCodeNo,
                    OpeningBalance = model.OpeningBalance,
                    TrType = model.TrType,
                    OpeningDate = model.OpeningDate,
                    Luser = model.LUser,
                    Ldate = DateTime.Now,
                    Lip = GetLocalIP() ?? "",
                    Lmac = GetMacAddress() ?? "",
                    MainCompanyCode = "",
                    UserInfoEmployeeId = ""
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

        public async Task<bool> UpdateAsync(OpeningBalanceVM model)
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
                //string newVoucherCode = await GetLastVoucherTypeCodeAsync();

                //entity.VoucherType_Code = newVoucherCode;
                entity.CompanyCode = model.CompanyCode;
                entity.BranchCode = model.BranchCode;
                entity.SubSusidiaryLedgerCodeNo = model.SubSusidiaryLedgerCodeNo;
                entity.OpeningBalance = model.OpeningBalance;
                entity.TrType = model.TrType;
                entity.OpeningDate = model.OpeningDate;
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


        #region Company & Branch & General Ledger Dropdown

        public async Task<List<CommonChoiceVM>> GetCompanyDropdownInfo()
        {
            var dropdowndata = await _company.All().Select(e => new CommonChoiceVM
            {
                Id = e.CompanyCode,
                Name = e.CompanyName,
            }).ToListAsync();

            return dropdowndata;
        }

        public async Task<List<CommonChoiceVM>> GetBranchDropdownInfo(string companycode)
        {
            var dropdowndata = await _branch.All()
                .Where(b => b.CompanyCode == companycode)
                .Select(e => new CommonChoiceVM
            {
                Id = e.BranchCode,
                Name = e.BranchName,
            }).ToListAsync();

            return dropdowndata;
        }

        //public async Task<List<CommonChoiceVM>> GetGenetalLedegerDropdownInfo()
        //{
        //    var dropdowndata = await _generalLedger.All().Select(e => new CommonChoiceVM
        //    {
        //        Id = e.SubSusidiaryLedgerCodeNo,
        //        Name = e.SubSubsidiaryLedgerName,
        //    }).ToListAsync();

        //    return dropdowndata;
        //}
        public async Task<List<CommonChoiceVM>> GetGenetalLedegerDropdownInfo()
        {
            try
            {
                var ledgers = await (from gl in _generalLedger.All().AsNoTracking()
                                     join ssl in _subsidiary.All().AsNoTracking()
                                         on gl.SubsidiaryLedgerCodeNo equals ssl.SusidiaryLedgerCodeNo into subsidiar
                                     from ssl in subsidiar.DefaultIfEmpty()
                                     join scl in _subcontrol.All().AsNoTracking()
                                         on ssl.GeneralLedgerCodeNo equals scl.GeneralLedgerCodeNo into subcontrolGroup
                                     from scl in subcontrolGroup.DefaultIfEmpty()
                                     select new
                                     {
                                         Id = gl.SubSusidiaryLedgerCodeNo,
                                         LedgerName = gl.SubSubsidiaryLedgerName,
                                         GeneralLedgerName = scl != null ? scl.GeneralLedgerName : null
                                     }).ToListAsync();

                var cusIds = ledgers
                    .Where(x => !string.IsNullOrEmpty(x.LedgerName) && x.LedgerName.StartsWith("CUS", StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.LedgerName)
                    .Distinct()
                    .ToList();

                Dictionary<string, string> customerMap = new();
                if (cusIds.Count > 0)
                {
                    var customers = await _salesCustomer.All().AsNoTracking()
                        .Where(c => cusIds.Contains(c.CustomerId))
                        .Select(c => new { c.CustomerId, c.CustomerName })
                        .ToListAsync();

                    foreach (var c in customers)
                    {
                        if (!string.IsNullOrEmpty(c.CustomerId) && !customerMap.ContainsKey(c.CustomerId))
                        {
                            customerMap[c.CustomerId] = c.CustomerName ?? c.CustomerId;
                        }
                    }
                }

                return ledgers.Select(x =>
                {
                    string ledgerName = x.LedgerName ?? "";
                    string generalName = !string.IsNullOrEmpty(x.GeneralLedgerName) ? $" ({x.GeneralLedgerName})" : "";
                    string name;
                    if (ledgerName.StartsWith("CUS", StringComparison.OrdinalIgnoreCase) && customerMap.TryGetValue(ledgerName, out var cName))
                    {
                        name = $"{cName} ({ledgerName}){generalName}";
                    }
                    else
                    {
                        name = $"{ledgerName}{generalName}".Trim();
                    }
                    return new CommonChoiceVM { Id = x.Id, Name = name };
                }).OrderBy(x => x.Name).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetGenetalLedegerDropdownInfo: {ex}");
                return await _generalLedger.All().AsNoTracking().Select(e => new CommonChoiceVM
                {
                    Id = e.SubSusidiaryLedgerCodeNo,
                    Name = e.SubSubsidiaryLedgerName
                }).OrderBy(x => x.Name).ToListAsync();
            }
        }

        public async Task<(List<CommonChoiceVM> Items, int TotalCount)> GetGeneralLedgerDropdownPagedAsync(string term = "", int page = 1, int pageSize = 10)
        {
            try
            {
                if (pageSize <= 0) pageSize = 10;
                if (page < 1) page = 1;

                var baseQuery = from gl in _generalLedger.All().AsNoTracking()
                                join ssl in _subsidiary.All().AsNoTracking()
                                    on gl.SubsidiaryLedgerCodeNo equals ssl.SusidiaryLedgerCodeNo into subsidiar
                                from ssl in subsidiar.DefaultIfEmpty()
                                join scl in _subcontrol.All().AsNoTracking()
                                    on ssl.GeneralLedgerCodeNo equals scl.GeneralLedgerCodeNo into subcontrolGroup
                                from scl in subcontrolGroup.DefaultIfEmpty()
                                select new
                                {
                                    Id = gl.SubSusidiaryLedgerCodeNo,
                                    LedgerName = gl.SubSubsidiaryLedgerName,
                                    GeneralLedgerName = scl != null ? scl.GeneralLedgerName : null
                                };

                if (!string.IsNullOrWhiteSpace(term))
                {
                    var t = term.Trim();
                    baseQuery = baseQuery.Where(x =>
                        (x.LedgerName != null && EF.Functions.Like(x.LedgerName, $"%{t}%")) ||
                        (x.Id != null && EF.Functions.Like(x.Id, $"%{t}%")) ||
                        (x.GeneralLedgerName != null && EF.Functions.Like(x.GeneralLedgerName, $"%{t}%"))
                    );
                }

                int totalCount = await baseQuery.CountAsync();

                var pagedRows = await baseQuery
                    .OrderBy(x => x.LedgerName)
                    .ThenBy(x => x.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var cusIds = pagedRows
                    .Where(x => !string.IsNullOrEmpty(x.LedgerName) && x.LedgerName.StartsWith("CUS", StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.LedgerName)
                    .Distinct()
                    .ToList();

                Dictionary<string, string> customerMap = new();
                if (cusIds.Count > 0)
                {
                    var customers = await _salesCustomer.All().AsNoTracking()
                        .Where(c => cusIds.Contains(c.CustomerId))
                        .Select(c => new { c.CustomerId, c.CustomerName })
                        .ToListAsync();

                    foreach (var c in customers)
                    {
                        if (!string.IsNullOrEmpty(c.CustomerId) && !customerMap.ContainsKey(c.CustomerId))
                        {
                            customerMap[c.CustomerId] = c.CustomerName ?? c.CustomerId;
                        }
                    }
                }

                var items = pagedRows.Select(x =>
                {
                    string ledgerName = x.LedgerName ?? "";
                    string generalName = !string.IsNullOrEmpty(x.GeneralLedgerName) ? $" ({x.GeneralLedgerName})" : "";
                    string name;
                    if (ledgerName.StartsWith("CUS", StringComparison.OrdinalIgnoreCase) && customerMap.TryGetValue(ledgerName, out var cName))
                    {
                        name = $"{cName} ({ledgerName}){generalName}";
                    }
                    else
                    {
                        name = $"{ledgerName}{generalName}".Trim();
                    }
                    return new CommonChoiceVM { Id = x.Id, Name = name };
                }).ToList();

                return (items, totalCount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetGeneralLedgerDropdownPagedAsync: {ex}");
                return (new List<CommonChoiceVM>(), 0);
            }
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
