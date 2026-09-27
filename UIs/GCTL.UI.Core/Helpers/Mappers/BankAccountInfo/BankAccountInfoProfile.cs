using AutoMapper;
using GCTL.Core.ViewModels.CoreBankAccountInformations;
using GCTL.Data.Models;

namespace GCTL.UI.Core.Helpers.Mappers.BankAccountInfo
{
    public class BankAccountInfoProfile : Profile
    {
        public BankAccountInfoProfile()
        {
            CreateMap<CoreBankAccountInformation, CoreBankAccountInformationSetupViewModel>()
                .ForMember(dest => dest.AutoId, opt => opt.MapFrom(src => src.AutoId))
                .ForMember(dest => dest.Tc, opt => opt.MapFrom(src => (int)src.AutoId))
                .ReverseMap()
                .ForMember(dest => dest.AutoId, opt => opt.MapFrom(src => src.AutoId > 0 ? src.AutoId : (decimal)src.Tc));
        }
    }
}
