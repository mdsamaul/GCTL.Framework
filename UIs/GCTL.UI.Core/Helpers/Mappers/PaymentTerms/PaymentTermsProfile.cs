using AutoMapper;
using GCTL.Core.ViewModels.PaymentTerms;
using GCTL.Data.Models;

namespace GCTL.UI.Core.Helpers.Mappers.PaymentTerms
{
    public class PaymentTermsProfile : Profile
    {
        public PaymentTermsProfile()
        {
            CreateMap<SalesDefPaymentTerm, PaymentTermsSetupViewModel>()
                .ForMember(dest => dest.AutoId, opt => opt.MapFrom(src => src.Tc))
                .ForMember(dest => dest.Tc, opt => opt.MapFrom(src => src.Tc))
                .ForMember(dest => dest.PaymentTermsId, opt => opt.MapFrom(src => src.PaymentTermsId))
                .ForMember(dest => dest.PaymentTermsName, opt => opt.MapFrom(src => src.PaymentTermsName))
                .ForMember(dest => dest.Percentise, opt => opt.MapFrom(src => src.Percentise))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
                .ForMember(dest => dest.CreditDays, opt => opt.MapFrom(src => src.CreditDays))
                .ForMember(dest => dest.Ldate, opt => opt.MapFrom(src => src.Ldate))
                .ForMember(dest => dest.Luser, opt => opt.MapFrom(src => src.Luser))
                .ForMember(dest => dest.Lip, opt => opt.MapFrom(src => src.Lip))
                .ForMember(dest => dest.Lmac, opt => opt.MapFrom(src => src.Lmac))
                .ForMember(dest => dest.ModifyDate, opt => opt.MapFrom(src => src.ModifyDate));

            CreateMap<PaymentTermsSetupViewModel, SalesDefPaymentTerm>()
                .ForMember(dest => dest.Tc, opt => opt.MapFrom(src => src.Tc))
                .ForMember(dest => dest.PaymentTermsId, opt => opt.MapFrom(src => src.PaymentTermsId))
                .ForMember(dest => dest.PaymentTermsName, opt => opt.MapFrom(src => src.PaymentTermsName))
                .ForMember(dest => dest.Percentise, opt => opt.MapFrom(src => src.Percentise))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
                .ForMember(dest => dest.CreditDays, opt => opt.MapFrom(src => src.CreditDays))
                .ForMember(dest => dest.Ldate, opt => opt.MapFrom(src => src.Ldate))
                .ForMember(dest => dest.Luser, opt => opt.MapFrom(src => src.Luser))
                .ForMember(dest => dest.Lip, opt => opt.MapFrom(src => src.Lip))
                .ForMember(dest => dest.Lmac, opt => opt.MapFrom(src => src.Lmac))
                .ForMember(dest => dest.ModifyDate, opt => opt.MapFrom(src => src.ModifyDate));
        }
    }
}
