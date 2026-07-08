using GCTL.Core.ViewModels.Common;

namespace GCTL.Service.AdmissionTypes;

/// <summary>
/// SAMPLE MODULE - template for every other service you add.
///
/// This mirrors your original IAdmissionTypeService exactly. It's commented
/// out below the class stub because the real entity (HmsAdmissionType) and
/// access-code entity (CoreAccessCode) don't exist yet in this template -
/// they'll be generated once you run EF Power Tools against your database.
///
/// TO ACTIVATE THIS MODULE once you've scaffolded your DB:
///   1. Confirm your scaffolded entity is named HmsAdmissionType (or rename
///      references below to match whatever EF Power Tools actually generated)
///   2. Uncomment AdmissionTypeService.cs in this same folder
///   3. Register it in Program.cs:
///        builder.Services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
///        builder.Services.AddScoped<IAdmissionTypeService, AdmissionTypeService>();
/// </summary>
public interface IAdmissionTypeService
{
    // List<HmsAdmissionType> GetAdmissionTypes();
    // HmsAdmissionType? GetAdmissionType(string code);
    // bool DeleteAdmissionType(string id);
    // HmsAdmissionType SaveAdmissionType(HmsAdmissionType entity);
    // bool IsAdmissionTypeExistByCode(string code);
    // bool IsAdmissionTypeExist(string name);
    // bool IsAdmissionTypeExist(string name, string typeCode);
    // IEnumerable<CommonSelectModel> AdmissionTypeSelection();
    // bool SavePermission(string accessCode);
    // bool UpdatePermission(string accessCode);
    // bool DeletePermission(string accessCode);
}
