using GCTL.Core.Data;
using GCTL.Core.ViewModels.Common;
using GCTL.Core.ViewModels.DeleteHistories;
using GCTL.Data.Models;
using GCTL.Service.DeleteHistories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace GCTL.Service.BloodGroups
{
    public class BloodGroupService : AppService<HrmDefBloodGroup>, IBloodGroupService
    {
        private readonly IRepository<HrmDefBloodGroup> bloodGroupRepository;
        private readonly IRepository<CoreAccessCode> accessCodeRepository;
        private readonly IDeleteHistoryService deleteHistoryService;

        private const string TableName = "HRM_Def_BloodGroup";
        private const string ColumnName = "BloodGroupCode";

        public BloodGroupService(IRepository<HrmDefBloodGroup> bloodGroupRepository,
            IRepository<CoreAccessCode> accessCodeRepository,
            IDeleteHistoryService deleteHistoryService)
            : base(bloodGroupRepository)
        {
            this.bloodGroupRepository = bloodGroupRepository;
            this.accessCodeRepository = accessCodeRepository;
            this.deleteHistoryService = deleteHistoryService;
        }

        public List<HrmDefBloodGroup> GetBloodGroups()
        {
            return GetAll();
        }

        public HrmDefBloodGroup GetBloodGroup(string id)
        {
            return bloodGroupRepository.GetById(id);
        }

        public HrmDefBloodGroup GetBloodGroup(decimal id)
        {
            return bloodGroupRepository.All().FirstOrDefault(x => x.AutoId == id);
        }

        public HrmDefBloodGroup SaveBloodGroup(HrmDefBloodGroup entity)
        {
            if (entity.AutoId > 0)
                Update(entity);
            else
                Add(entity);

            return entity;


        }

        public async Task<(bool success, bool refSuccess, string message)> DeleteBloodGroup(List<string> ids, DeleteHistoryViewModel model)
        {

            try
            {
                // Normalize to list of IDs (comma-separated support)


                if (ids.Count == 0)
                {
                    return (false, false, "No valid department codes found.");
                }

                // Dependency check
              //  var alternateColumn = new List<string> { "DepartmentID" };
                var dependencyCheck = await deleteHistoryService.CheckDependenciesAsync(
                    bloodGroupRepository.GetTableName(),
                    ColumnName,
                    ids.Cast<string>().ToList()
                );

                if (!dependencyCheck.CanDelete)
                {
                    return (false, true, dependencyCheck.Message);
                }

                await bloodGroupRepository.BeginTransactionAsync();

                // Fetch entities
                var entities = await bloodGroupRepository.All()
                    .Where(x => ids.Contains(x.BloodGroupCode))
                    .ToListAsync();

                if (entities == null || entities.Count == 0)
                {
                    await bloodGroupRepository.RollbackTransactionAsync();
                    return (false, false, "No matching departments found to delete.");
                }

                // Perform delete
                bloodGroupRepository.Delete(entities);
                model.tableName = TableName;
                // Log deleted records
                await deleteHistoryService.LogDeletedRecordsAsync(
                    entities, model
                );

                await bloodGroupRepository.CommitTransactionAsync();
                return (true, false, "Deleted successfully.");
            }
            catch (Exception ex)
            {
                await bloodGroupRepository.RollbackTransactionAsync();
                Console.WriteLine(ex);
                return (false, false, $"Delete failed: {ex.Message}");
            }
        }

        //public bool DeleteBloodGroup(string id)
        //{
        //    var entity = GetBloodGroup(id);
        //    if (entity != null)
        //    {
        //        bloodGroupRepository.Delete(entity);
        //        return true;
        //    }
        //    return false;
        //}

        public bool IsBloodGroupExistByCode(string code)
        {
            return bloodGroupRepository.All().Any(x => x.BloodGroupCode == code);
        }

        public bool IsBloodGroupExist(string name)
        {
            return bloodGroupRepository.All().Any(x => x.BloodGroup == name);
        }

        public bool IsBloodGroupExist(string name, string typeCode)
        {
            return bloodGroupRepository.All().Any(x => x.BloodGroup == name && x.BloodGroupCode != typeCode);
        }

        public bool IsBloodGroupExist(string name, decimal autoId)
        {
            return bloodGroupRepository.All().Any(x => x.BloodGroup == name && x.AutoId != autoId);
        }

        public IEnumerable<CommonSelectModel> BloodGroupSelection()
        {
            return bloodGroupRepository.All()
                .Select(x => new CommonSelectModel
                {
                    Code = x.BloodGroupCode,
                    Name = x.BloodGroup
                });
        }
        public bool SavePermission(string accessCode)
        {
            return accessCodeRepository.All().Any(x => x.AccessCodeId == accessCode && x.Title == "Blood Group" && x.CheckAdd);
        }
        public bool UpdatePermission(string accessCode)
        {
            return accessCodeRepository.All().Any(x => x.AccessCodeId == accessCode && x.Title == "Blood Group" && x.CheckEdit);
        }
        public bool DeletePermission(string accessCode)
        {
            return accessCodeRepository.All().Any(x => x.AccessCodeId == accessCode && x.Title == "Blood Group" && x.CheckDelete);
        }
    }
}
