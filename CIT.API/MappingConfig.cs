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

            CreateMap<OrderMaster, OrderDTO>()
                .ForMember(dest => dest.OrderTypeId, opt => opt.MapFrom(src => src.OrderTypeId))
                .ForMember(dest => dest.Repeats, opt => opt.MapFrom(src => src.Repeats))
                .ForMember(dest => dest.taskmodellist, opt => opt.MapFrom(src => src.taskmodellist))
                .ReverseMap();
            CreateMap<BranchMaster, BranchDTO>()
               .ForMember(dest => dest.BranchID, opt => opt.MapFrom(src => src.BranchID))
               .ForMember(dest => dest.BranchName, opt => opt.MapFrom(src => src.BranchName))
               .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
               .ReverseMap();
            CreateMap<OrderTypeMaster, OrderTypeDTO>()
               .ForMember(dest => dest.OrderTypeID, opt => opt.MapFrom(src => src.OrderTypeID))
               .ForMember(dest => dest.TypeName, opt => opt.MapFrom(src => src.TypeName))
               .ForMember(dest => dest.DataSource, opt => opt.MapFrom(src => src.DataSource))
               .ReverseMap();
            CreateMap<RegionMaster, RegionDTO>()
               .ForMember(dest => dest.RegionID, opt => opt.MapFrom(src => src.RegionID))
               .ForMember(dest => dest.RegionName, opt => opt.MapFrom(src => src.RegionName))
               .ForMember(dest => dest.DataSource, opt => opt.MapFrom(src => src.DataSource))
               .ReverseMap();
        }
    }
}
