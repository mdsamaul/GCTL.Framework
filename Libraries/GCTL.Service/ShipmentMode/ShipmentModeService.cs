using GCTL.Core.Data;
using GCTL.Core.ViewModels;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using GCTL.Core.DataTables;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GCTL.Service.ShipmentMode
{
    public class ShipmentModeService : AppService<CfDefExpenseType>, IShipmentMode
    {
        private readonly IRepository<CfDefExpenseType> _repository;
        private readonly IRepository<TransportModeType> tMTrepository;

        public ShipmentModeService(IRepository<CfDefExpenseType> genericRepository, IRepository<TransportModeType> tMTrepository) : base(genericRepository)
        {
            _repository = genericRepository;
            this.tMTrepository = tMTrepository;
        }
        public async Task<bool> BulkDeleteAsync(List<int> ids)
        {
            await _repository.BeginTransactionAsync();

            try
            {
                var entity = await _repository.All().Where(c => ids.Contains(c.Tc)).ToListAsync();

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

        public async Task<bool> DeleteAsync(int id)
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

                await _repository.DeleteAsync(entity.Tc);

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

        public async Task<ShipmentModeVM> GetByIdAsync(int id)
        {
            var data = await _repository.All().Where(pt => pt.Tc == id)
                        .Select(pt => new ShipmentModeVM
                        {
                            TC = pt.Tc,
                            ExpenseTypeID = pt.ExpenseTypeId,
                            ExpenseType = pt.ExpenseType ?? "",
                            ShortName = pt.ShortName ?? "",
                            transportModeTypeId = pt.TransportModeTypeId ?? "",
                            Luser = pt.Luser,
                            Lip = pt.Lip,
                            Lmac = pt.Lmac ?? "",
                            Ldate = pt.Ldate,
                            ModifyDate = pt.ModifyDate,
                            ExpenseLedgerCodeNo = pt.ExpenseLedgerCodeNo,
                            RevenueLedgerCodeNo = pt.RevenueLedgerCodeNo,
                            AdvanceLedgerCode = pt.AdvanceLedgerCode
                        }).FirstOrDefaultAsync();
            return data;
        }

        public async Task<string> GetLastUnitAsync()
        {

            var last = await _repository.All()
                        .OrderByDescending(c => c.ExpenseTypeId)
                        .FirstOrDefaultAsync();

            return last?.ExpenseTypeId;
        }

        public async Task<PaginationResponse<ShipmentModeVM>> GetPaginatedAsync(PaginationRequest request)
        {
            var query = from exp in _repository.All().AsNoTracking()
                        join tmt in tMTrepository.All().AsNoTracking()
                            on exp.TransportModeTypeId equals tmt.TransportModeId into tmtGroup
                        from tmt in tmtGroup.DefaultIfEmpty()
                        select new ShipmentModeVM
                        {
                            TC = exp.Tc,
                            ExpenseTypeID = exp.ExpenseTypeId,
                            ExpenseType = exp.ExpenseType ?? "",
                            ShortName = exp.ShortName ?? "",
                            transportModeTypeId = exp.TransportModeTypeId ?? "",
                            transportModeType = tmt != null ? tmt.TransportMode : "",
                            Ldate = exp.Ldate,
                            ModifyDate = exp.ModifyDate
                        };

            if (request.PageSize == -1)
            {
                request.PageSize = await query.CountAsync();
                request.PageNumber = 1;
            }

            if (string.IsNullOrEmpty(request.SortColumn) || request.SortColumn.Equals("default", StringComparison.OrdinalIgnoreCase))
            {
                query = query.OrderByDescending(x => x.ExpenseTypeID);
                request.SortColumn = "";
            }

            return await PaginationService<ShipmentModeVM>.GetPaginatedData(query, request);
        }

        public async Task<bool> IsDuplicateAsync(ShipmentModeVM model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            // Compare directly in the DB with trimming and case-insensitivity
            return await _repository.All()
                .AnyAsync(pm =>
                    pm.ExpenseType.Trim().ToLower() == model.ExpenseType.Trim().ToLower() 

                    && pm.Tc != model.TC
                );
        }

        public async Task<bool> IsExistAsync(int id)
        {
            return await _repository.All().AnyAsync(x => x.Tc == id);
        }

        public async Task<bool> SaveAsync(ShipmentModeVM model)
        {
            try
            {
                var entity = new CfDefExpenseType
                {
                    ExpenseTypeId = model.ExpenseTypeID,
                    ExpenseType = model.ExpenseType,
                    ShortName = model.ShortName ?? "",
                    Luser = "", // Set based on your auth system, e.g., User.Identity.Name
                    Ldate = DateTime.Now,
                    Lip = model.Lip ?? "",
                    Lmac = model.Lmac ?? "",
                    ExpenseLedgerCodeNo = model.ExpenseLedgerCodeNo?? "",
                    RevenueLedgerCodeNo = model.RevenueLedgerCodeNo ?? "",
                    AdvanceLedgerCode = model.AdvanceLedgerCode ?? "",
                    TransportModeTypeId = model.transportModeTypeId ?? ""
                };

                await _repository.AddAsync(entity);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Save failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateAsync(ShipmentModeVM model)
        {
            await _repository.BeginTransactionAsync();
            try
            {
                var entity = await _repository.GetByIdAsync(model.ExpenseTypeID);
                if (entity == null)
                {
                    await _repository.RollbackTransactionAsync();
                    return false;
                }

                entity.ExpenseTypeId = model.ExpenseTypeID;
                entity.ExpenseType = model.ExpenseType;
                entity.ShortName = model.ShortName ?? "";
                entity.TransportModeTypeId = model.transportModeTypeId ?? "";
                entity.Luser = ""; // Set based on your auth system, e.g., User.Identity.Name
                entity.Lip = model.Lip ?? "";
                entity.Lmac = model.Lmac ?? "";
                entity.ModifyDate = DateTime.Now;

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

        public async Task<List<ShipmentModeVM>> GetAllAsync()
        {
            var entity = await _repository.All().ToListAsync();

            var result = entity.Select(model => new ShipmentModeVM
            {
                ExpenseTypeID = model.ExpenseTypeId,
                ExpenseType = model.ExpenseType,
                ShortName = model.ShortName,
            }).ToList();

            return result;
        }

        public async Task<List<ShipmentModeVM>> GetAllExpenseTypesAsync()
        {
            var data = await _repository.All().ToListAsync();
            return data.Select(x => new ShipmentModeVM
            {
                ExpenseTypeID = x.ExpenseTypeId,
                ExpenseType = x.ExpenseType
            }).ToList();
        }
        public async Task<List<ShipmentModeVM>> GetShipmentModeDetailsAsync(string transportModeTypeId)
        {
            var data = await _repository.All()
                .Where(pt => pt.TransportModeTypeId == transportModeTypeId)
                .Select(pt => new ShipmentModeVM
                {
                    TC = pt.Tc,
                    ExpenseTypeID = pt.ExpenseTypeId,
                    ExpenseType = pt.ExpenseType ?? "",
                    transportModeType = tMTrepository.All()
                        .Where(t => t.TransportModeId == pt.TransportModeTypeId)
                        .Select(t => t.TransportMode)
                        .FirstOrDefault(),
                    transportModeTypeId = pt.TransportModeTypeId ?? "",
                    ShortName = pt.ShortName ?? "",
                    Ldate = pt.Ldate,
                    ModifyDate = pt.ModifyDate
                })
                .ToListAsync();

            return data;
        }


    }
}
