using AutoMapper;
using GCTL.Core.ViewModels.PaymentType;
using GCTL.Data.Models;

namespace GCTL.UI.Core.Helpers.Mappers.PaymentType
{
    public class PaymentTypeProfile : Profile
    {
        public PaymentTypeProfile()
        {
            CreateMap<SalesDefPaymentType, PaymentTypeSetupViewModel>()
                .ForMember(dest => dest.AutoId, opt => opt.MapFrom(src => src.Tc))
                .ForMember(dest => dest.PaymentTypeId, opt => opt.MapFrom(src => src.PaymentTypeId))
                .ForMember(dest => dest.PaymentType, opt => opt.MapFrom(src => src.PaymentType))
                .ForMember(dest => dest.ShortName, opt => opt.MapFrom(src => src.ShortName))
                .ForMember(dest => dest.Ldate, opt => opt.MapFrom(src => src.Ldate))
                .ForMember(dest => dest.Luser, opt => opt.MapFrom(src => src.Luser))
                .ForMember(dest => dest.Lip, opt => opt.MapFrom(src => src.Lip))
                .ForMember(dest => dest.Lmac, opt => opt.MapFrom(src => src.Lmac))
                .ForMember(dest => dest.ModifyDate, opt => opt.MapFrom(src => src.ModifyDate));

            CreateMap<PaymentTypeSetupViewModel, SalesDefPaymentType>()
                .ForMember(dest => dest.Tc, opt => opt.MapFrom(src => src.Tc))
                .ForMember(dest => dest.PaymentTypeId, opt => opt.MapFrom(src => src.PaymentTypeId))
                .ForMember(dest => dest.PaymentType, opt => opt.MapFrom(src => src.PaymentType))
                .ForMember(dest => dest.ShortName, opt => opt.MapFrom(src => src.ShortName))
                .ForMember(dest => dest.Ldate, opt => opt.MapFrom(src => src.Ldate))
                .ForMember(dest => dest.Luser, opt => opt.MapFrom(src => src.Luser))
                .ForMember(dest => dest.Lip, opt => opt.MapFrom(src => src.Lip))
                .ForMember(dest => dest.Lmac, opt => opt.MapFrom(src => src.Lmac))
                .ForMember(dest => dest.ModifyDate, opt => opt.MapFrom(src => src.ModifyDate));
        }
    }
}
