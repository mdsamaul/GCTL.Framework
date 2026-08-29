using GCTL.Core.Data;
using GCTL.Data.Models;
using GCTL.Service.Pagination;
using GCTL.UI.Core.ViewModels.ShipmentMode;
using GCTL.Core.DataTables;
using Microsoft.EntityFrameworkCore;

namespace GCTL.Service.ShipmentMode
{
    public class ShipmentStatusService : AppService<CfShipmentStatus>, IShipmentStatusService
    {
        private readonly IRepository<CfShipmentStatus> _repository;
        private readonly IRepository<TransportModeType> tMTrepository;

        public ShipmentStatusService(IRepository<CfShipmentStatus> genericRepository, IRepository<TransportModeType> tMTrepository) : base(genericRepository)
        {
            _repository = genericRepository;
            this.tMTrepository = tMTrepository;
        }

        public async Task<bool> BulkDeleteAsync(List<int> ids)
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
                return false;
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

                await _repository.DeleteAsync(entity.AutoId);
                await _repository.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                await _repository.RollbackTransactionAsync();
                Console.WriteLine($"Delete error: {ex}");
                return false;
            }
        }

        public async Task<ShipmentStatusVM> GetByIdAsync(int id)
        {
            var data = await _repository.All().Where(pt => pt.AutoId == id)
                        .Select(pt => new ShipmentStatusVM
                        {
                            AutoId = pt.AutoId,
                            ShipmentStatusId = pt.ShipmentStatusId,
                            ShipmentStatus = pt.ShipmentStatus ?? "",
                            ShortName = pt.ShortName ?? "",
                            TransportModeTypeId = pt.TransportModeTypeId,
                            Luser = pt.Luser,
                            Lip = pt.Lip,
                            Lmac = pt.Lmac ?? "",
                            Ldate = pt.Ldate,
                            ModifyDate = pt.ModifyDate
                        }).FirstOrDefaultAsync();
            return data;
        }

        public async Task<string> GetLastIdAsync()
        {
            var last = await _repository.All()
                        .OrderByDescending(c => c.ShipmentStatusId)
                        .FirstOrDefaultAsync();

            return last?.ShipmentStatusId;
        }

        public async Task<PaginationResponse<ShipmentStatusVM>> GetPaginatedAsync(PaginationRequest request)
        {
            var query = from st in _repository.All().AsNoTracking()
                        join tmt in tMTrepository.All().AsNoTracking()
                            on st.TransportModeTypeId equals tmt.TransportModeId into tmtGroup
                        from tmt in tmtGroup.DefaultIfEmpty()
                        select new ShipmentStatusVM
                        {
                            AutoId = st.AutoId,
                            ShipmentStatusId = st.ShipmentStatusId,
                            ShipmentStatus = st.ShipmentStatus ?? "",
                            ShortName = st.ShortName ?? "",
                            TransportModeTypeId = st.TransportModeTypeId ?? "",
                            transportModeType = tmt != null ? tmt.TransportMode : "",
                            Ldate = st.Ldate,
                            ModifyDate = st.ModifyDate
                        };

            if (request.PageSize == -1)
            {
                request.PageSize = await query.CountAsync();
                request.PageNumber = 1;
            }

            if (string.IsNullOrEmpty(request.SortColumn) || request.SortColumn.Equals("default", StringComparison.OrdinalIgnoreCase))
            {
                query = query.OrderByDescending(x => x.ShipmentStatusId);
                request.SortColumn = "";
            }

            return await PaginationService<ShipmentStatusVM>.GetPaginatedData(query, request);
        }

        public async Task<bool> IsDuplicateAsync(ShipmentStatusVM model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            return await _repository.All()
                .AnyAsync(pm =>
                    pm.ShipmentStatus.Trim().ToLower() == model.ShipmentStatus.Trim().ToLower()
                    && pm.AutoId != model.AutoId
                );
        }

        public async Task<bool> IsExistAsync(int id)
        {
            return await _repository.All().AnyAsync(x => x.AutoId == id);
        }

        public async Task<bool> SaveAsync(ShipmentStatusVM model)
        {
            try
            {
                var entity = new CfShipmentStatus
                {
                    ShipmentStatusId = model.ShipmentStatusId,
                    ShipmentStatus = model.ShipmentStatus,
                    ShortName = model.ShortName ?? "",
                    TransportModeTypeId = model.TransportModeTypeId ?? "",
                    Luser = "",
                    Ldate = DateTime.Now,
                    Lip = model.Lip ?? "",
                    Lmac = model.Lmac ?? ""
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

        public async Task<bool> UpdateAsync(ShipmentStatusVM model)
        {
            await _repository.BeginTransactionAsync();
            try
            {
                var entity = await _repository.GetByIdAsync(model.AutoId);
                if (entity == null)
                {
                    await _repository.RollbackTransactionAsync();
                    return false;
                }

                entity.ShipmentStatusId = model.ShipmentStatusId;
                entity.ShipmentStatus = model.ShipmentStatus;
                entity.ShortName = model.ShortName ?? "";
                entity.TransportModeTypeId = model.TransportModeTypeId ?? "";
                entity.Luser = "";
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

        public async Task<List<ShipmentStatusVM>> GetShipmentStatusDetailsAsync(string transportModeTypeId)
        {
            var data = await _repository.All()
                .Where(pt => pt.TransportModeTypeId == transportModeTypeId)
                .Select(pt => new ShipmentStatusVM
                {
                    AutoId = pt.AutoId,
                    ShipmentStatusId = pt.ShipmentStatusId,
                    ShipmentStatus = pt.ShipmentStatus ?? "",
                    ShortName = pt.ShortName ?? "",
                    TransportModeTypeId = pt.TransportModeTypeId ?? "",
                    transportModeType = tMTrepository.All()
                        .Where(t => t.TransportModeId == pt.TransportModeTypeId)
                        .Select(t => t.TransportMode)
                        .FirstOrDefault(),
                    Ldate = pt.Ldate,
                    ModifyDate = pt.ModifyDate
                })
                .ToListAsync();

            return data;
        }
    }
}