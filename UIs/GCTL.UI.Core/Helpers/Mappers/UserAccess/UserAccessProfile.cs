using AutoMapper;
using GCTL.Core.ViewModels.UserAccesses;
using GCTL.Data.Models;

namespace GCTL.UI.Core.Helpers.Mappers.UserAccess
{
    public class UserAccessProfile : Profile
    {
        public UserAccessProfile()
        {
            CreateMap<CoreUserInfo, UserAccessSetupViewModel>()
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => src.Status == "1"))
                
                .ForMember(dest => dest.Role,
                        opt => opt.MapFrom(src => src.Type))
                .ReverseMap()

                .ForPath(dest => dest.Ldate, opt => opt.Ignore())
                .ForPath(dest => dest.EntryDate, opt => opt.Ignore())
                .ForPath(dest => dest.Status,
                    opt => opt.MapFrom(src => src.Status ? "1" : "0"))
                
                .ForPath(dest => dest.Type,
                    opt => opt.MapFrom(src => src.Role))
                .AfterMap((src, dest) =>
                 {
                     foreach (var prop in dest.GetType().GetProperties()
                                  .Where(p => p.PropertyType == typeof(string) && p.CanWrite))
                     {
                         if (prop.GetValue(dest) is null)
                             prop.SetValue(dest, string.Empty);
                     }
                 });

            CreateMap<UserViewModel, UserAccessSetupViewModel>().ReverseMap();
        }
    }
}
