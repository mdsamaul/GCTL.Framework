using GCTL.Core.ViewModels.TransportModeType;

namespace GCTL.UI.Core.Controllers
{
    public interface ITransportModeTypeService
    {
        Task<List<TransportModeTypeSetupViewModel>?> GetAllAsync();
    }
}