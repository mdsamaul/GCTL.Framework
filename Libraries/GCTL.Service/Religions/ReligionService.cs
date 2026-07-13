using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Data.Models;
using GCTL.Service.DeleteHistories;
using Microsoft.EntityFrameworkCore;

namespace GCTL.Service.Religions
{
    public class ReligionService : AppService<HrmDefReligion>, IReligionService
    {
        private readonly IRepository<HrmDefReligion> religionRepository;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly IDeleteHistoryService deleteHistoryService;

        private const string TableName = "HRM_Def_Religion";
        private const string ColumnName = "ReligionCode";

        public ReligionService(IRepository<HrmDefReligion> religionRepository,
            IRepository<CoreAccessCode> accessCodeRepository,
            IDeleteHistoryService deleteHistoryService
            )
            : base(religionRepository)
        {
            this.religionRepository = religionRepository;
            this.accessCodeRepository = accessCodeRepository;
            this.deleteHistoryService = deleteHistoryService;
        }

        public List<HrmDefReligion> GetReligions()
        {
            return GetAll();
        }

        public HrmDefReligion GetReligion(string id)
        {
            return religionRepository.GetById(id);
        }

        public HrmDefReligion SaveReligion(HrmDefReligion entity)
        {
            if (IsReligionExistByCode(entity.ReligionCode))
            {
                Update(entity);
            }

            else
            {
                
                Add(entity);
            }
               

            return entity;
        }

        public async Task<(bool success, bool refSuccess, string message)> DeleteReligion(List<string> ids, DeleteHistoryViewModel model)
        {

            try
            {
                // Normalize to list of IDs (comma-separated support)


                if (ids.Count == 0)
                {
                    return (false, false, "No valid department codes found.");
                }

                // Dependency check
               // var alternateColumn = new List<string> { "DepartmentID" };
                var dependencyCheck = await deleteHistoryService.CheckDependenciesAsync(
                    religionRepository.GetTableName(),
                    ColumnName,
                    ids.Cast<string>().ToList()
                );

                if (!dependencyCheck.CanDelete)
                {
                    return (false, true, dependencyCheck.Message);
                }

                await religionRepository.BeginTransactionAsync();

                // Fetch entities
                var entities = await religionRepository.All()
                    .Where(x => ids.Contains(x.ReligionCode))
                    .ToListAsync();

                if (entities == null || entities.Count == 0)
                {
                    await religionRepository.RollbackTransactionAsync();
                    return (false, false, "No matching departments found to delete.");
                }

                // Perform delete
                religionRepository.Delete(entities);
                model.tableName = TableName;
                // Log deleted records
                await deleteHistoryService.LogDeletedRecordsAsync(
                    entities, model
                
                );

                await religionRepository.CommitTransactionAsync();
                return (true, false, "Deleted successfully.");
            }
            catch (Exception ex)
            {
                await religionRepository.RollbackTransactionAsync();
                Console.WriteLine(ex);
                return (false, false, $"Delete failed: {ex.Message}");
            }
        }

        public bool IsReligionExistByCode(string code)
        {
            return religionRepository.All().Any(x => x.ReligionCode == code);
        }

        public bool IsReligionExist(string name)
        {
            return religionRepository.All().Any(x => x.Religion == name);
        }

        public bool IsReligionExist(string name, string typeCode)
        {
            return religionRepository.All().Any(x => x.Religion == name && x.ReligionCode != typeCode);
        }

        public IEnumerable<CommonSelectModel> ReligionSelection()
        {
            return religionRepository.All()
                .Select(x => new CommonSelectModel
                {
                    Code = x.ReligionCode,
                    Name = x.Religion
                });
        }

        public bool SavePermission(string accessCode)
        {
            return accessCodeRepository.All().Any(x => x.AccessCodeId == accessCode && x.Title == "Religion" && x.CheckAdd);
        }
        public bool UpdatePermission(string accessCode)
        {
            return accessCodeRepository.All().Any(x => x.AccessCodeId == accessCode && x.Title == "Religion" && x.CheckEdit);
        }
        public bool DeletePermission(string accessCode)
        {
            return accessCodeRepository.All().Any(x => x.AccessCodeId == accessCode && x.Title == "Religion" && x.CheckDelete);
        }
    }
}
