using AutoMapper;
using GCTL.Core.ViewModels.PaymentModes;
using GCTL.Data.Models;

namespace GCTL.UI.Core.Helpers.Mappers.PaymentModes
{
    public class PaymentModeProfile : Profile
    {
        public PaymentModeProfile()
        {
            CreateMap<SalesDefPaymentMode, PaymentModeSetupViewModel>()
                .ForMember(dest => dest.AutoId, opt => opt.MapFrom(src => src.AutoId))
                .ForMember(dest => dest.Tc, opt => opt.MapFrom(src => src.AutoId))
                .ForMember(dest => dest.PaymentModeId, opt => opt.MapFrom(src => src.PaymentModeId))
                .ForMember(dest => dest.PaymentModeName, opt => opt.MapFrom(src => src.PaymentModeName))
                .ForMember(dest => dest.PaymentModeShortName, opt => opt.MapFrom(src => src.PaymentModeShortName))
                .ForMember(dest => dest.Ldate, opt => opt.MapFrom(src => src.Ldate))
                .ForMember(dest => dest.Luser, opt => opt.MapFrom(src => src.Luser))
                .ForMember(dest => dest.Lip, opt => opt.MapFrom(src => src.Lip))
                .ForMember(dest => dest.Lmac, opt => opt.MapFrom(src => src.Lmac))
                .ForMember(dest => dest.ModifyDate, opt => opt.MapFrom(src => src.ModifyDate));

            CreateMap<PaymentModeSetupViewModel, SalesDefPaymentMode>()
                .ForMember(dest => dest.AutoId, opt => opt.MapFrom(src => src.AutoId ?? src.Tc))
                .ForMember(dest => dest.PaymentModeId, opt => opt.MapFrom(src => src.PaymentModeId))
                .ForMember(dest => dest.PaymentModeName, opt => opt.MapFrom(src => src.PaymentModeName))
                .ForMember(dest => dest.PaymentModeShortName, opt => opt.MapFrom(src => src.PaymentModeShortName))
                .ForMember(dest => dest.Ldate, opt => opt.MapFrom(src => src.Ldate))
                .ForMember(dest => dest.Luser, opt => opt.MapFrom(src => src.Luser))
                .ForMember(dest => dest.Lip, opt => opt.MapFrom(src => src.Lip))
                .ForMember(dest => dest.Lmac, opt => opt.MapFrom(src => src.Lmac))
                .ForMember(dest => dest.ModifyDate, opt => opt.MapFrom(src => src.ModifyDate));
        }
    }
}
