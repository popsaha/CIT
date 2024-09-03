using AutoMapper;
using CIT.API.Models;
using CIT.API.Models.Dto;

namespace CIT.API
{
    public class MappingConfig : Profile
    {
        public MappingConfig()
        {
            CreateMap<UserMaster, UserDTO>()
                .ForMember(dest => dest.ID, opt => opt.MapFrom(src => src.UserID))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName))
                .ReverseMap();
        }
    }
}
