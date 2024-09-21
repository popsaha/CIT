using AutoMapper;
using CIT.API.Models;
using CIT.API.Models.Dto;
using CIT.API.Models.Dto.Branch;
using CIT.API.Models.Dto.Customer;
using CIT.API.Models.Dto.OrderType;
using CIT.API.Models.Dto.Region;


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

            CreateMap<Customer, CustomerDTO>().ReverseMap();
            CreateMap<Customer, CustomerCreateDTO>().ReverseMap();
            CreateMap<Customer, CustomerUpdateDTO>().ReverseMap();

            CreateMap<OrderMaster, OrderDTO>()
                .ForMember(dest => dest.OrderTypeId, opt => opt.MapFrom(src => src.OrderTypeId))
                .ForMember(dest => dest.Repeats, opt => opt.MapFrom(src => src.Repeats))
                .ForMember(dest => dest.taskmodellist, opt => opt.MapFrom(src => src.taskmodellist))
                .ReverseMap();

            CreateMap<BranchMaster, BranchDTO>().ReverseMap();
            CreateMap<BranchMaster, BranchUpdateDTO>().ReverseMap();
            CreateMap<BranchMaster, BranchCreateDTO>().ReverseMap();

            CreateMap<OrderTypeMaster, OrderTypeDTO>().ReverseMap();
            CreateMap<OrderTypeMaster, OrderTypeUpdateDTO>().ReverseMap();
            CreateMap<OrderTypeMaster, OrderTypeCreateDTO>().ReverseMap();

            CreateMap<RegionMaster, RegionDTO>().ReverseMap();
            CreateMap<RegionMaster, RegionUpdateDTO>().ReverseMap();
            CreateMap<RegionMaster, RegionCreateDTO>().ReverseMap();

            CreateMap<TaskList, TaskListDTO>().ReverseMap();

            CreateMap<TaskGrouping, TaskGroupingRequestDTO>().ReverseMap();
        }
    }
}
