using GCTL.Data.Models;
using AutoMapper;
using GCTL.Core.ViewModels.BankInformations;

namespace GCTL.UI.Core.Helpers.Mappers.BankInformations
{
    public class BankInformationsProfile : Profile
    {
        public BankInformationsProfile()
        {
            CreateMap<SalesDefBankInfo, BankInformationsSetupViewModel>().ReverseMap();
        }
    }
}
