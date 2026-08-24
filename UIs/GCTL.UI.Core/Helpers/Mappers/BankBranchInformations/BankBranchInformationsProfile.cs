using AutoMapper;
using GCTL.Core.ViewModels.SalesDefBankBranchInfos;
using GCTL.Data.Models;

namespace GCTL.UI.Core.Helpers.Mappers.BankBranchInformations
{
    public class BankBranchInformationsProfile : Profile
    {
        public BankBranchInformationsProfile()
        {
            CreateMap<SalesDefBankBranchInfo, SalesDefBankBranchInfoSetupViewModel>()
                .ForMember(dest => dest.AutoId, opt => opt.MapFrom(src => (int)src.AutoId))
                .ForMember(dest => dest.Tc, opt => opt.MapFrom(src => (int)src.AutoId))
                .ReverseMap()
                .ForMember(dest => dest.AutoId, opt => opt.MapFrom(src => src.AutoId > 0 ? (decimal)src.AutoId : (decimal)src.Tc));
        }
    }
}
