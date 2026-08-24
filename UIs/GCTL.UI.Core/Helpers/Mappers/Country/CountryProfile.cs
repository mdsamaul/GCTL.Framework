using GCTL.Data.Models;
using AutoMapper;
using GCTL.Core.ViewModels.Country;

namespace GCTL.UI.Core.Helpers.Mappers.Country
{
    public class CountryProfile : Profile
    {
        public CountryProfile()
        {
            CreateMap<CaDefCountry, CountrySetuoViewModel>().ReverseMap();
        }
    }
}
