using AutoMapper;
using GCTL.Core.ViewModels.CurrencyType;
using GCTL.Data.Models;

namespace GCTL.UI.Core.Helpers.Mappers.CurrencyType
{
    public class CurrencyTypeProfile : Profile
    {
        public CurrencyTypeProfile()
        {
            CreateMap<CaDefCurrency, CurrencyTypeSetupViewModel>()
                .ForMember(dest => dest.AutoId, opt => opt.MapFrom(src => src.Tc))
                .ForMember(dest => dest.Tc, opt => opt.MapFrom(src => (int)src.Tc))
                .ReverseMap()
                .ForMember(dest => dest.Tc, opt => opt.MapFrom(src => src.AutoId > 0 ? src.AutoId : src.Tc));

            CreateMap<CaDefCurrency, CurrencyTypeVM>().ReverseMap();
        }
    }
}
