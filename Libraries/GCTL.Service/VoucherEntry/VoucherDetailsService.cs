using GCTL.Core.Data;
using GCTL.Core.ViewModels;
using GCTL.Core.ViewModels.VoucherEntry;
using GCTL.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GCTL.Service.VoucherEntry
{
    public class VoucherDetailsService : AppService<AccVoucherEntryDetailsTemp>, IVoucherDetails
    {

        #region Service

        private readonly IRepository<AccVoucherEntryDetailsTemp> _repository;
        private readonly IRepository<AccControlLedger> _groupLedger;
        private readonly IRepository<AccSubControlLedger> _controlledger;
        private readonly IRepository<AccGeneralLedger> _subcontrolLedger;
        private readonly IRepository<AccSubsidiaryLedger> _subsidiledger;
        private readonly IRepository<AccGeneralLedger> _generalLedger;
        public VoucherDetailsService(
            IRepository<AccVoucherEntryDetailsTemp> genericRepository,
            IRepository<AccVoucherEntryDetailsTemp> repository,
            IRepository<AccControlLedger> groupLedger,
            IRepository<AccSubControlLedger> controlledger,
            IRepository<AccGeneralLedger> subcontrolLedger, 
            IRepository<AccSubsidiaryLedger> subsidiledger, 
            IRepository<AccGeneralLedger> generalLedger
            ) 
    : base(genericRepository)
        {
            _groupLedger = groupLedger;
            _controlledger = controlledger;
            _subcontrolLedger = subcontrolLedger;
            _subsidiledger = subsidiledger;
            _repository = repository;
            _generalLedger = generalLedger;
        }

        #endregion

        #region Account Head Dropdown on Event Change Backend
        public async Task<LedgerDetailsVM> GetAllLedgerByIdAsync(string id)
        {
            var raw = await (from subSub in _generalLedger.All() // Acc_SubSubsidiaryLedger
                            join sub in _subsidiledger.All() // Acc_SubsidiaryLedger
                                on subSub.SubsidiaryLedgerCodeNo equals sub.SusidiaryLedgerCodeNo into subJoin
                            from sub in subJoin.DefaultIfEmpty()

                            join general in _subcontrolLedger.All() // Acc_GeneralLedger
                                on sub.GeneralLedgerCodeNo equals general.GeneralLedgerCodeNo into generalJoin
                            from general in generalJoin.DefaultIfEmpty()

                            join subControl in _controlledger.All() // Acc_SubControlLedger
                                on general.SubControlLedgerCodeNo equals subControl.SubControlLedgerCodeNo into subControlJoin
                            from subControl in subControlJoin.DefaultIfEmpty()

                            join control in _groupLedger.All() // Acc_ControlLedger
                                on subControl.ControlLedgerCodeNo equals control.ControlLedgerCodeNo into controlJoin
                            from control in controlJoin.DefaultIfEmpty()

                            where subSub.SubSusidiaryLedgerCodeNo == id

                            select new
                            {
                                GroupCode = control.ControlLedgerCodeNo,
                                GroupName = control.ControlLedgerName,
                                ControlCode = subControl.SubControlLedgerCodeNo,
                                ControlName = subControl.SubControlLedgerName,
                                SubControlCode = general.GeneralLedgerCodeNo,
                                SubControlName = general.GeneralLedgerName,
                                SubSidiaryCode = sub.SusidiaryLedgerCodeNo,
                                SubSidiaryName = sub.SubsidiaryLedgerName,
                                GeneralName = subSub.SubSubsidiaryLedgerName
                            }
                    ).FirstOrDefaultAsync();

            if (raw == null) return null;

            return new LedgerDetailsVM
            {
                GroupLedgerName = !string.IsNullOrEmpty(raw.GroupCode) ? $"{raw.GroupCode}-{raw.GroupName}" : raw.GroupName,
                ControlLedgerName = !string.IsNullOrEmpty(raw.ControlCode) ? $"{raw.ControlCode}-{raw.ControlName}" : raw.ControlName,
                SubControlLedgerName = !string.IsNullOrEmpty(raw.SubControlCode) ? $"{raw.SubControlCode}-{raw.SubControlName}" : raw.SubControlName,
                SubSidiaryLedgerName = !string.IsNullOrEmpty(raw.SubSidiaryCode) ? $"{raw.SubSidiaryCode}-{raw.SubSidiaryName}" : raw.SubSidiaryName,
                GeneralLedgerName = raw.GeneralName
            };
        }

        #endregion

        #region Delete Voucher Details

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

        #endregion

        #region Get All Tmp Voucher Details
        public async Task<List<VoucherEntryDetailsTempVM>> GetAllAsync(string? currentUser)
        {
            var result = await (from tmp in _repository.All().AsNoTracking()
                                join subSub in _generalLedger.All().AsNoTracking()
                                    on tmp.AccCode equals subSub.SubSusidiaryLedgerCodeNo
                                    into subSubJoin
                                from subSub in subSubJoin.DefaultIfEmpty()
                                where tmp.Luser == currentUser.ToString()
                                orderby tmp.AutoId

                                select new VoucherEntryDetailsTempVM
                                {
                                    autoId = tmp.AutoId,
                                    AccCode = tmp.AccCode,
                                    Description = tmp.Description,
                                    DebitAmount = tmp.DebitAmount,
                                    CreditAmount = tmp.CreditAmount,
                                    TrType = tmp.TrType,
                                    ChequeNo = tmp.ChequeNo,
                                    ChequeDate = tmp.ChequeDate.Year > 1900 ? tmp.ChequeDate : (DateTime?)null,
                                    AccountHead = subSub.SubSubsidiaryLedgerName
                                }).ToListAsync();

            return result;
        }

        #endregion

        #region Get Details Voucher Information with specific ID
        public async Task<VoucherEntryDetailsTempVM> GetByIdAsync(decimal id)
        {
            var raw = await (from tmp in _repository.All()
                              join gnrlL in _generalLedger.All()
                                on tmp.AccCode equals gnrlL.SubSusidiaryLedgerCodeNo into gnLjoin
                              from gnrl in gnLjoin.DefaultIfEmpty()

                              join subsidi in _subsidiledger.All()
                                on gnrl.SubsidiaryLedgerCodeNo equals subsidi.SusidiaryLedgerCodeNo into subsijoin
                              from subsidi in subsijoin.DefaultIfEmpty()

                              join subcon in _subcontrolLedger.All() // Acc_GeneralLedger
                                on subsidi.GeneralLedgerCodeNo equals subcon.GeneralLedgerCodeNo into subconJoin
                              from subcon in subconJoin.DefaultIfEmpty()

                              join control in _controlledger.All() // Acc_SubControlLedger
                                on subcon.SubControlLedgerCodeNo equals control.SubControlLedgerCodeNo into ControlJoin
                              from control in ControlJoin.DefaultIfEmpty()

                              join grp in _groupLedger.All() // Acc_ControlLedger
                                on control.ControlLedgerCodeNo equals grp.ControlLedgerCodeNo into controlJoin
                              from grp in controlJoin.DefaultIfEmpty()
                              where tmp.AutoId == id

                              select new
                              {
                                  autoId = tmp.AutoId,
                                  AccCode = tmp.AccCode,
                                  TrType = tmp.TrType,
                                  CreditAmount = tmp.CreditAmount,
                                  DebitAmount = tmp.DebitAmount,
                                  ChequeNo = tmp.ChequeNo,
                                  ChequeDate = tmp.ChequeDate,
                                  Description = tmp.Description,
                                  GroupCode = grp.ControlLedgerCodeNo,
                                  GroupName = grp.ControlLedgerName,
                                  ControlCode = control.SubControlLedgerCodeNo,
                                  ControlName = control.SubControlLedgerName,
                                  SubControlCode = subcon.GeneralLedgerCodeNo,
                                  SubControlName = subcon.GeneralLedgerName,
                                  SubSidiaryCode = subsidi.SusidiaryLedgerCodeNo,
                                  SubSidiaryName = subsidi.SubsidiaryLedgerName
                              }).FirstOrDefaultAsync();

            if (raw == null) return null;

            return new VoucherEntryDetailsTempVM
            {
                autoId = raw.autoId,
                AccCode = raw.AccCode,
                TrType = raw.TrType,
                CreditAmount = raw.CreditAmount,
                DebitAmount = raw.DebitAmount,
                ChequeNo = raw.ChequeNo,
                ChequeDate = raw.ChequeDate.Year > 1900 ? raw.ChequeDate : (DateTime?)null,
                Description = raw.Description,
                GroupLedgerName = !string.IsNullOrEmpty(raw.GroupCode) ? $"{raw.GroupCode}-{raw.GroupName}" : raw.GroupName,
                ControlLedgerName = !string.IsNullOrEmpty(raw.ControlCode) ? $"{raw.ControlCode}-{raw.ControlName}" : raw.ControlName,
                SubControlLedgerName = !string.IsNullOrEmpty(raw.SubControlCode) ? $"{raw.SubControlCode}-{raw.SubControlName}" : raw.SubControlName,
                SubSidiaryLedgerName = !string.IsNullOrEmpty(raw.SubSidiaryCode) ? $"{raw.SubSidiaryCode}-{raw.SubSidiaryName}" : raw.SubSidiaryName
            };
        }

        #endregion

        #region Generate Last Voucher Details Code
        public async Task<string> GetLastVoucherDetailsCodeAsync()
        {
            // Get all codes and extract numeric part safely
            var lastCodeString = await _repository.All().Where(x => x.VoucherEntryDetailsCodeNo.StartsWith("VED_"))
                                .Select(x => x.VoucherEntryDetailsCodeNo).OrderByDescending(x => x).FirstOrDefaultAsync();

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

        #region Get Exist ID
        public async Task<bool> IsExistAsync(int id)
        {
            return await _repository.All().AnyAsync(x => x.AutoId == id);
        }

        #endregion

        #region Tmp Details Voucher Entry Save and Update
        public async Task<bool> SaveAsync(VoucherEntryDetailsTempVM model)
        {
            try
            {
                if(string.IsNullOrEmpty(model.TrType) || string.IsNullOrEmpty(model.AccCode))
                {
                    return false;
                }
                string newCode = await GetLastVoucherDetailsCodeAsync();

                var entity = new AccVoucherEntryDetailsTemp
                {
                    VoucherEntryDetailsCodeNo = newCode,
                    TrType = model.TrType,
                    AccCode = model.AccCode,
                    Description = model.Description,
                    DebitAmount = model.TrType == "Dr" ? model.DebitAmount : 0,
                    CreditAmount = model.TrType == "Cr" ? model.CreditAmount : 0,
                    ChequeNo = model.ChequeNo ?? "",
                    ChequeDate = model.ChequeDate ?? new DateTime(1900, 1, 1),
                    Luser  = model.Luser?.ToString(),
                    VoucherEntryCodeNo = ""
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

        public async Task<bool> UpdateAsync(VoucherEntryDetailsTempVM model)
        {
            await _repository.BeginTransactionAsync();
            try
            {
                if (string.IsNullOrEmpty(model.TrType) || string.IsNullOrEmpty(model.AccCode))
                {
                    return false;
                }
                var entity = await _repository.GetByIdAsync(model.autoId);
                if (entity == null)
                {
                    await _repository.RollbackTransactionAsync();
                    return false;
                }

                entity.TrType = model.TrType;
                entity.AccCode = model.AccCode;
                entity.Description = model.Description;
                entity.DebitAmount = model.TrType == "Dr" ? model.DebitAmount : 0;
                entity.CreditAmount = model.TrType == "Cr" ? model.CreditAmount : 0;
                entity.ChequeNo = model.ChequeNo ?? "";
                entity.ChequeDate = model.ChequeDate ?? new DateTime(1900, 1, 1);
                entity.Luser = model.Luser?.ToString();

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

        #region Account Head Dropdown
        public async Task<List<CommonChoiceVM>> GetAccountHeadDropdownAsync()
        {
            try
            {
                var ledgers = await _generalLedger.All().AsNoTracking()
                    .Where(x => !string.IsNullOrEmpty(x.SubSusidiaryLedgerCodeNo))
                    .Select(x => new
                    {
                        Id = x.SubSusidiaryLedgerCodeNo,
                        Name = x.SubSubsidiaryLedgerName
                    })
                    .ToListAsync();

                return ledgers
                    .OrderBy(x => x.Id)
                    .Select(x => new CommonChoiceVM
                    {
                        Id = x.Id,
                        Name = !string.IsNullOrWhiteSpace(x.Name)
                            ? $"{x.Id}-{x.Name.Trim()}"
                            : x.Id
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAccountHeadDropdownAsync: {ex}");
                return new List<CommonChoiceVM>();
            }
        }
        #endregion

    }
}
