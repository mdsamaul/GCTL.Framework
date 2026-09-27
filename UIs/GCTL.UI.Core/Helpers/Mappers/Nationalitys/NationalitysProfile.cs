using GCTL.Data.Models;
using AutoMapper;
using GCTL.Core.ViewModels.Nationalitys;

namespace GCTL.UI.Core.Helpers.Mappers.Nationalitys
{
    public class NationalitysProfile : Profile
    {
        public NationalitysProfile()
        {
            CreateMap<HrmDefNationality, NationalitysSetupViewModel>().ReverseMap();
        }
    }
}
