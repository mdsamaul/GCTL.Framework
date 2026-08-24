using AutoMapper;
using GCTL.Core.ViewModels.UnitType;
using GCTL.Data.Models;

namespace GCTL.UI.Core.Helpers.Mappers.UnitType
{
    public class UnitTypeProfile : Profile
    {
        public UnitTypeProfile()
        {
            CreateMap<RmgProdDefUnitType, UnitTypeSetupViewModel>()
                .ForMember(dest => dest.AutoId, opt => opt.MapFrom(src => src.Tc))
                .ForMember(dest => dest.UnitTypID, opt => opt.MapFrom(src => src.UnitTypId))
                .ForMember(dest => dest.UnitTypeName, opt => opt.MapFrom(src => src.UnitTypeName))
                .ForMember(dest => dest.ShortName, opt => opt.MapFrom(src => src.ShortName))
                .ForMember(dest => dest.DecimalPlaces, opt => opt.MapFrom(src => src.DecimalPlaces))
                .ForMember(dest => dest.Ldate, opt => opt.MapFrom(src => src.Ldate))
                .ForMember(dest => dest.Luser, opt => opt.MapFrom(src => src.Luser))
                .ForMember(dest => dest.Lip, opt => opt.MapFrom(src => src.Lip))
                .ForMember(dest => dest.Lmac, opt => opt.MapFrom(src => src.Lmac))
                .ForMember(dest => dest.ModifyDate, opt => opt.MapFrom(src => src.ModifyDate));

            CreateMap<UnitTypeSetupViewModel, RmgProdDefUnitType>()
                .ForMember(dest => dest.Tc, opt => opt.MapFrom(src => src.Tc))
                .ForMember(dest => dest.UnitTypId, opt => opt.MapFrom(src => src.UnitTypID))
                .ForMember(dest => dest.UnitTypeName, opt => opt.MapFrom(src => src.UnitTypeName))
                .ForMember(dest => dest.ShortName, opt => opt.MapFrom(src => src.ShortName))
                .ForMember(dest => dest.DecimalPlaces, opt => opt.MapFrom(src => src.DecimalPlaces))
                .ForMember(dest => dest.Ldate, opt => opt.MapFrom(src => src.Ldate))
                .ForMember(dest => dest.Luser, opt => opt.MapFrom(src => src.Luser))
                .ForMember(dest => dest.Lip, opt => opt.MapFrom(src => src.Lip))
                .ForMember(dest => dest.Lmac, opt => opt.MapFrom(src => src.Lmac))
                .ForMember(dest => dest.ModifyDate, opt => opt.MapFrom(src => src.ModifyDate));
        }
    }
}
