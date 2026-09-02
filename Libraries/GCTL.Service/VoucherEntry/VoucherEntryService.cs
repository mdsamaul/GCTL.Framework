using GCTL.Core.Data;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.VoucherEntry;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Net;   
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace GCTL.Service.VoucherEntry
{
    public class VoucherEntryService : AppService<AccVoucherEntry>, IVoucherEntry
    {

        #region Service

        private readonly IRepository<AccVoucherEntry> _repository;
        private readonly IRepository<AccVoucherType> _voucherType;
        private readonly IRepository<AccVoucherEntryDetailsTemp> _tmpDetails;
        private readonly IRepository<AccVoucherEntryDetail> _voucherDetails;
        private readonly IRepository<AccSubSubsidiaryLedger> _generalLedger;
        private readonly IRepository<CoreBranch> _branch;

        public VoucherEntryService(
            IRepository<AccVoucherEntry> genericRepository,
            IRepository<AccVoucherType> voucherType,
            IRepository<AccVoucherEntryDetailsTemp> tmpDetails,
            IRepository<AccVoucherEntryDetail> voucherDetails,
            IRepository<AccSubSubsidiaryLedger> generalLedger,
            IRepository<CoreBranch> branch
            ) 
            
            : base(genericRepository)
        {
            _repository = genericRepository;
            _voucherType = voucherType;
            _tmpDetails = tmpDetails;
            _voucherDetails = voucherDetails;
            _generalLedger = generalLedger;
            _branch = branch;
        }

        #endregion

        #region Generate Voucher Entry Code No
        private async Task<string> GetLastVoucherEntryCodeNoAsync()
        {
            var lastEntry = await _repository.All()
                .OrderByDescending(v => v.AutoId)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (lastEntry != null && !string.IsNullOrEmpty(lastEntry.VoucherEntryCodeNo))
            {
                // Example: VE_000123 → extract 000123
                var lastCode = lastEntry.VoucherEntryCodeNo;
                var numberPart = lastCode.Replace("VE_", "");
                if (int.TryParse(numberPart, out int lastNum))
                {
                    nextNumber = lastNum + 1;
                }
            }

            // Format → VE_ + 6 digit number
            return $"VE_{nextNumber:D6}";
        }
        #endregion

        #region Checking Debit & Credit Amount Is Equal or Not
        public async Task<bool> IsDebitCreditEqualAsync(string userId)
        {
            try
            {
                var tmpdetails = await _tmpDetails.All()
                                .Where(d => d.Luser == userId)
                                .ToListAsync();

                decimal totalDebit = tmpdetails.Sum(d => d.DebitAmount ?? 0);
                decimal totalCredit = tmpdetails.Sum(d => d.CreditAmount ?? 0);

                if (totalDebit != totalCredit) return false;

                return true;
            }
            catch (Exception)
            {

                throw;
            }
        }

        #endregion

        #region Voucher Entry Save and Update

        public async Task<bool> SaveAsync(VoucherEntryVM model)
        {
            await _repository.BeginTransactionAsync();
            try
            {
                if(string.IsNullOrEmpty(model.CompanyCode) || string.IsNullOrEmpty(model.BranchCode) || string.IsNullOrEmpty(model.VoucherType_Code) || string.IsNullOrEmpty(model.VoucherNo) || string.IsNullOrEmpty(model.Narration))
                {
                    return false;
                }
                //Debit and Credit validation
                bool Isequal = await IsDebitCreditEqualAsync(model.Luser.ToString());
                if (!Isequal) return false;

                string voucherNo = await GetLastVoucherNoAsync(model.VoucherType_Code);
                string voucherEntryCodeNo = await GetLastVoucherEntryCodeNoAsync();

                var entity = new AccVoucherEntry
                {
                    CompanyCode = model.CompanyCode,
                    BranchCode = model.BranchCode,
                    VoucherTypeCode = model.VoucherType_Code,
                    VoucherNo = voucherNo,
                    VoucherEntryCodeNo = voucherEntryCodeNo,
                    VoucherDate = model.VoucherDate,
                    Narration = model.Narration,
                    Luser = model.Luser.ToString(),
                    Ldate = DateTime.Now,
                    Lip = GetLocalIP(),
                    Lmac = GetMacAddress(),
                    CostCenterCodeNo = "",
                    FinancialYear = "",
                    PeriodCodeNo = "",
                    UserInfoEmployeeId = "",
                    MainCompanyCode = "",
                    DivisionCode = "",
                    InvoiceNo = "",
                    IsApproved = "",
                    ApprovedBy = "",
                };

                await _repository.AddAsync(entity);

                var tmpDetails = await _tmpDetails.All().Where(d => d.Luser == model.Luser.ToString()).ToListAsync();

                foreach (var tmp in tmpDetails)
                {
                    // Generate Details Code if missing
                      var  newcode = await GetLastVoucherDetailsCodeAsync();

                    var detail = new AccVoucherEntryDetail
                    {
                        VoucherEntryCodeNo = voucherEntryCodeNo,
                        VoucherEntryDetailsCodeNo = newcode,
                        AccCode = tmp.AccCode,
                        TrType = tmp.TrType,
                        Description = tmp.Description,
                        DebitAmount = tmp.DebitAmount,
                        CreditAmount = tmp.CreditAmount,
                        ChequeNo = tmp.ChequeNo,
                        ChequeDate = tmp.ChequeDate,
                        Luser = tmp.Luser,
                        VoucherEntryAutoId = entity.AutoId,
                        BankName = "",
                        Branch = "",
                        Ldate = DateTime.Now,
                        Lip = GetLocalIP(),
                        Lmac = GetMacAddress()
                    };

                    await _voucherDetails.AddAsync(detail);

                    //tmp.VoucherEntryCodeNo = voucherEntryCodeNo;
                    //await _tmpDetails.UpdateAsync(tmp);
                }
                await _tmpDetails.DeleteRangeAsync(tmpDetails);

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

        public async Task<bool> UpdateAsync(VoucherEntryVM model)
        {
            await _repository.BeginTransactionAsync();
            try
            {
                if (string.IsNullOrEmpty(model.CompanyCode) || string.IsNullOrEmpty(model.BranchCode) || string.IsNullOrEmpty(model.VoucherType_Code) || string.IsNullOrEmpty(model.VoucherNo) || string.IsNullOrEmpty(model.Narration))
                {
                    return false;
                }

                //Debit & Credit Validation 
                bool Isequal = await IsDebitCreditEqualAsync(model.Luser.ToString());
                if (!Isequal) return false;

                var entity = await _repository.GetByIdAsync(model.autoId);
                if (entity == null)
                {
                    await _repository.RollbackTransactionAsync();
                    return false;
                }

                entity.CompanyCode = model.CompanyCode;
                entity.BranchCode = model.BranchCode;
                entity.VoucherTypeCode = model.VoucherType_Code;
                entity.VoucherDate = model.VoucherDate;
                entity.VoucherNo = model.VoucherNo;
                entity.Narration = model.Narration;
                entity.ModifyDate = DateTime.Now;
                entity.Luser = model.Luser.ToString();

                var tmpDetails = await _tmpDetails.All().Where(d => d.Luser == model.Luser.ToString()).ToListAsync();

                foreach(var data in tmpDetails)
                {
                    var detail =await _voucherDetails.All().Where(d => d.VoucherEntryDetailsCodeNo == data.VoucherEntryDetailsCodeNo).FirstOrDefaultAsync();

                    detail.TrType = data.TrType;
                    detail.AccCode = data.AccCode;
                    detail.Description = data.Description;
                    detail.DebitAmount = data.DebitAmount;
                    detail.CreditAmount = data.CreditAmount;
                    detail.ChequeNo = data.ChequeNo;
                    detail.ChequeDate = data.ChequeDate;
                    detail.Luser = model.Luser.ToString();
                    detail.ModifyDate = DateTime.Now;

                    await _voucherDetails.UpdateAsync(detail);
                }

                await _tmpDetails.DeleteRangeAsync(tmpDetails);

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

        #endregion

        #region  Voucher Delete Single Or More
        public async Task<bool> BulkDeleteAsync(List<decimal> ids)
        {
            await _repository.BeginTransactionAsync();

            try
            {
                //Master data
                var masters = await _repository.All().Where(c => ids.Contains(c.AutoId)).ToListAsync();

                if (!masters.Any())
                {
                    await _repository.RollbackTransactionAsync();
                    return false;
                }

                //Related Details data
                var details = await _voucherDetails.All().Where(d => d.VoucherEntryAutoId.HasValue && ids.Contains(d.VoucherEntryAutoId.Value)).ToListAsync();

                //Delete details first
                if (details.Any())
                {
                    await _voucherDetails.DeleteRangeAsync(details);
                }

                //Delete masters
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

        #region Delete tmp data With User ID
        public async Task<bool> DeleteAsync(int? ids)
        {
            try
            {
                var tmpdetails = await _tmpDetails.All().Where(d => d.Luser == ids.ToString()).ToListAsync();
                if (tmpdetails != null)
                {
                    await _tmpDetails.DeleteRangeAsync(tmpdetails);
                }
                return true;

            }
            catch (Exception ex)
            {
                return false;
            }

        }

        #endregion

        #region Generate Voucher Entry Detail Code

        public async Task<string> GetLastVoucherDetailsCodeAsync()
        {
            // Tmp table
            var lastCodeString = await _voucherDetails.All()
                .OrderByDescending(t => t.VoucherEntryDetailsCodeNo)
                .Select(t => t.VoucherEntryDetailsCodeNo)
                .FirstOrDefaultAsync();

            long nextCode = 1;

            if (!string.IsNullOrEmpty(lastCodeString))
            {
                var numberPart = lastCodeString.Substring(4);
                if (long.TryParse(numberPart, out long lastNum))
                {
                    nextCode = lastNum + 1;
                }
            }

            return $"VED_{nextCode:D6}";
        }




        #endregion

        #region Generate VoucherNo Code
        public async Task<string> GetLastVoucherNoAsync(string voucherTypeCode)
        {
            var currentYear = DateTime.Now.ToString("yy");

            // Get last voucher for this type and current year
            var lastVoucher = await _repository.All()
                .Where(v => v.VoucherTypeCode == voucherTypeCode && v.VoucherNo.Contains(currentYear))
                .OrderByDescending(v => v.AutoId)
                .FirstOrDefaultAsync();

            int runningNumber = 1;
            if (lastVoucher != null)
            {
                // Extract last 6-digit number
                var lastNumberString = lastVoucher.VoucherNo.Substring(lastVoucher.VoucherNo.Length - 6);
                if (int.TryParse(lastNumberString, out int lastNumber))
                {
                    runningNumber = lastNumber + 1;
                }
            }

            // Get Voucher Type prefix
            var voucherType = await _voucherType.All()
                .FirstOrDefaultAsync(v => v.VoucherTypeCode == voucherTypeCode);

            if (voucherType == null)
                throw new Exception("Voucher Type not found");

            string voucherNo = $"{voucherType.Prefix}_{currentYear}_{runningNumber:D6}";
            return voucherNo;
        }

        #endregion

        #region Get All Voucher Entry
        public async Task<PaginationService<VoucherEntryVM, VoucherEntryVM>.PaginationResult<VoucherEntryVM>> GetPaginatedAsync(int pageNumber = 1, int pageSize = 5, string searchTerm = "", string sortColumn = "VoucherDate", string sortOrder = "desc", DateTime? startDate = null, DateTime? endDate = null)
        {
            //var query =
                        //from main in _repository.All()
                        //join branch in _branch.All()
                        //    on main.BranchCode equals branch.BranchCode
                        //select new VoucherEntryVM
                        //{
                        //    autoId = main.AutoId,
                        //    VoucherNo = main.VoucherNo,
                        //    VoucherDate = main.VoucherDate,
                        //    Narration = main.Narration,
                        //    InvoiceNo = main.InvoiceNo,
                        //    BranchName = branch.BranchName,

                        //    Amount = _voucherDetails.All()
                        //        .Where(d => d.VoucherEntryCodeNo == main.VoucherEntryCodeNo)
                        //        .Select(d => d.DebitAmount > 0 ? d.DebitAmount : d.CreditAmount).FirstOrDefault()
                        //};

            var query = from main in _repository.All().AsNoTracking()
                        join branch in _branch.All().AsNoTracking()
                           on main.BranchCode equals branch.BranchCode
                        select new VoucherEntryVM
                        {
                            autoId = main.AutoId,
                            VoucherEntryCodeNo = main.VoucherEntryCodeNo,
                            VoucherNo = main.VoucherNo,
                            VoucherDate = main.VoucherDate,
                            Narration = main.Narration,
                            InvoiceNo = main.InvoiceNo,
                            BranchName = branch.BranchName
                        };

            if (startDate.HasValue && endDate.HasValue)
            {
                query = query.Where(x => x.VoucherDate >= startDate.Value && x.VoucherDate <= endDate.Value);
            }
            else if (startDate.HasValue)
            {
                query = query.Where(x => x.VoucherDate >= startDate.Value);
            }
            else if (endDate.HasValue)
            {
                query = query.Where(x => x.VoucherDate <= endDate.Value);
            }


            if (pageSize == -1)
            {
                pageSize = await query.CountAsync();
                pageNumber = 1;
            }

            // PaginationService
            var paginatedResult = await PaginationService<VoucherEntryVM, VoucherEntryVM>.GetPaginatedData(query, pageNumber, pageSize, searchTerm, sortColumn, sortOrder, term => sc =>
                        EF.Functions.Like(sc.VoucherNo ?? "", $"%{term}%") ||
                        EF.Functions.Like(sc.BranchName ?? "", $"%{term}%") ||
                        EF.Functions.Like(sc.Narration ?? "", $"%{term}%"),
                    ob => ob
             );

            // Populate Amount for the current page only to avoid SQL timeouts on the Count/Sort query
            if (paginatedResult.Data != null && paginatedResult.Data.Any())
            {
                var voucherCodes = paginatedResult.Data.Select(x => x.VoucherEntryCodeNo).Where(c => c != null).ToList();
                
                var amounts = await _voucherDetails.All().AsNoTracking()
                    .Where(d => voucherCodes.Contains(d.VoucherEntryCodeNo))
                    .Select(d => new { d.VoucherEntryCodeNo, d.DebitAmount, d.CreditAmount, d.AutoId })
                    .ToListAsync();

                var groupedAmounts = amounts
                    .GroupBy(d => d.VoucherEntryCodeNo)
                    .ToDictionary(g => g.Key, g => g.OrderBy(d => d.AutoId).Select(d => d.DebitAmount > 0 ? d.DebitAmount : d.CreditAmount).FirstOrDefault());

                foreach (var item in paginatedResult.Data)
                {
                    if (item.VoucherEntryCodeNo != null && groupedAmounts.TryGetValue(item.VoucherEntryCodeNo, out var amt))
                    {
                        item.Amount = amt;
                    }
                }
            }

            return paginatedResult;
        }

        #endregion

        #region Get Master Voucher Information with specific Master ID

        public async Task<VoucherEntryVM> GetByMasterIdAsync(decimal autoId)
        {
            var data = await (from main in _repository.All()
                              where main.AutoId == autoId
                              select new VoucherEntryVM
                              {
                                  autoId = main.AutoId,
                                  CompanyCode = main.CompanyCode,
                                  BranchCode = main.BranchCode,
                                  VoucherNo = main.VoucherNo,
                                  VoucherDate = main.VoucherDate,
                                  Narration = main.Narration,
                                  VoucherType_Code = main.VoucherTypeCode,
                                  Ldate = main.Ldate,
                                  ModifyDate = main.ModifyDate
                              }).FirstOrDefaultAsync();

            return data;
        }
        #endregion

        #region Get Details Voucher Information with specific VoucherEntryAutoId
        public async Task<List<VoucherDetailsVM>> GetDetailsByVoucherIdAsync(decimal? voucherEntryAutoId)
        {
            var data = await (from details in _voucherDetails.All()
                              join gnrlL in _generalLedger.All()
                                on details.AccCode equals gnrlL.SubSusidiaryLedgerCodeNo into gnLjoin
                              from gnrl in gnLjoin.DefaultIfEmpty()
                              where details.VoucherEntryAutoId == voucherEntryAutoId
                              select new VoucherDetailsVM
                              {
                                  autoId = details.AutoId,
                                  AccCode = details.AccCode,
                                  Description = details.Description,
                                  DebitAmount = details.DebitAmount,
                                  CreditAmount = details.CreditAmount,
                                  AccountHeadName = gnrl.SubSubsidiaryLedgerName,
                                  TrType = details.TrType,
                                  VoucherEntryDetailsCodeNo = details.VoucherEntryDetailsCodeNo,
                              }).ToListAsync();

            return data;
        }
        #endregion

        #region When Edit Button Clicked then Details table data Copied to Tmp Table

        public async Task<bool> DetailsCopiedToTmp(decimal id, int? currenUserID)
        {
            var orginaDetailsData = await GetDetailsByVoucherIdAsync(id);

            //Details table Data Copied to Tmp table
            foreach (var tmpdata in orginaDetailsData)
            {
                var tmp = new AccVoucherEntryDetailsTemp
                {
                    VoucherEntryDetailsCodeNo = tmpdata.VoucherEntryDetailsCodeNo,
                    VoucherEntryCodeNo = tmpdata.VoucherEntryNo ?? "",
                    TrType = tmpdata.TrType,
                    AccCode = tmpdata.AccCode,
                    Description = tmpdata.Description,
                    DebitAmount = tmpdata.DebitAmount,
                    CreditAmount = tmpdata.CreditAmount,
                    ChequeNo = tmpdata.ChequeNo ?? "",
                    ChequeDate = tmpdata.ChequeDate,
                    Luser = currenUserID.ToString()
                };
                await _tmpDetails.AddAsync(tmp);
            }

            return true;
        }

        #endregion

        #region Get Exist ID
        public async Task<bool> IsExistAsync(int id)
        {
            return await _repository.All().AnyAsync(x => x.AutoId == id);
        }

        #endregion

        #region Voucher Type Dropdown
        public async Task<List<CommonChoiceVM>> GetVoucherTypeDropdownInfo()
        {
            var dropdowndata = await _voucherType.All().Select(e => new CommonChoiceVM
            {
                Id = e.VoucherTypeCode,
                Name = e.Description,
            }).ToListAsync();

            return dropdowndata;
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
