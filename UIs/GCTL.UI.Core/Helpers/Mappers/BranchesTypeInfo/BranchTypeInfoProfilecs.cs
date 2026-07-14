using AutoMapper;
using GCTL.Core.ViewModels.BranchesTypeInfo;
using GCTL.Data.Models;

namespace GCTL.UI.Core.Helpers.Mappers.BranchesTypeInfo
{
    public class BranchProfile : Profile
    {
        public BranchProfile()
        {
            CreateMap<CoreBranch, BranchTypeSetupViewModel>().ReverseMap();
        }
    }
}
