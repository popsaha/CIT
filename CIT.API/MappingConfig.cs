﻿using AutoMapper;
using CIT.API.Models;
using CIT.API.Models.Dto;
using CIT.API.Models.Dto.Branch;
using CIT.API.Models.Dto.UserMasterApi;
using CIT.API.Models.Dto.CrewCommander;
using CIT.API.Models.Dto.CrewTaskDetails;
using CIT.API.Models.Dto.Customer;
using CIT.API.Models.Dto.Order;
using CIT.API.Models.Dto.OrderType;
using CIT.API.Models.Dto.PickupType;
using CIT.API.Models.Dto.Region;
using CIT.API.Models.Dto.Role;
using CIT.API.Models.Dto.Task;
using CIT.API.Models.Dto.TaskGroupList;
using CIT.API.Models.Dto.Vehicle;
using CIT.API.Models.Dto.Police;
using CIT.API.Models.Dto.User;
using CIT.API.Models.Dto.ChaseVehicle;
using CIT.API.Models.Dto.OrderRoute;


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
            CreateMap<UserMaster, UserCreateDTO>().ReverseMap();

            CreateMap<UserMasterModel, UserListDTO>().ReverseMap();
            CreateMap<UserMasterModel, UserUpdateDTO>().ReverseMap();


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

            CreateMap<TaskList, TaskListsDTO>().ReverseMap();

            CreateMap<TaskGrouping, TaskGroupingRequestDTO>().ReverseMap();

            CreateMap<TaskGroupList, TaskGroupListDTO>().ReverseMap();

            CreateMap<VehicleAssignment, VehicleAssignmentRequestDTO>().ReverseMap();

            CreateMap<Vehicle, VehicleDTO>().ReverseMap();
            CreateMap<Vehicle, VehicleCreateDTO>().ReverseMap();
            CreateMap<Vehicle, VehicleUpdateDTO>().ReverseMap();


            CreateMap<UserMaster, CrewCommanderDTO>().ReverseMap();

            //CreateMap<CrewCommander, CrewCommanderDTO>().ReverseMap();

            CreateMap<TaskMaster, TaskDTO>().ReverseMap();
            CreateMap<TaskMaster, TaskCreateDTO>().ReverseMap();

       

            CreateMap<PickupTypes, PickupTypesDTO>().ReverseMap();
            CreateMap<PickupTypes, PickupTypesCreateDTO>().ReverseMap();
            CreateMap<PickupTypes, PickupTypesUpdateDTO>().ReverseMap();

            CreateMap<CrewTaskDetails, CrewTaskDetailsDTO>().ReverseMap();
            CreateMap<CrewTaskDetails, CrewTaskDetailsByTaskIdDTO>().ReverseMap();
            CreateMap<CrewTaskDetails, CrewTaskStatusUpdateDTO>().ReverseMap();
            CreateMap<CrewTaskDetails, CrewTaskParcelDTO>().ReverseMap();
            CreateMap<CrewTaskDetails, CrewTaskFailedStatusDTO>().ReverseMap();
            CreateMap<CrewTaskDetails, CrewTaskUnloadedParcelDTOs>().ReverseMap();

            CreateMap<RoleMaster, RoleListDTO>().ReverseMap();

            CreateMap<PoliceMaster, PoliceDTO>().ReverseMap();
            CreateMap<PoliceMaster, PoliceCreateDTO>().ReverseMap();
            CreateMap<PoliceMaster, PoliceUpdateDTO>().ReverseMap();

            CreateMap<User, LocalUserDTO>().ReverseMap();
            CreateMap<User, LocalUserCreateDTO>().ReverseMap();
            CreateMap<User, LocalUserUpdateDTO>().ReverseMap();


            CreateMap<ChaseVehicle, ChaseVehicleDTO>().ReverseMap();
            CreateMap<ChaseVehicle, ChaseVehicleCreateDTO>().ReverseMap();
            CreateMap<ChaseVehicle, ChaseVehicleUpdateDTO>().ReverseMap();


            CreateMap<OrderRoutesMaster, OrderRouteCreateDTO>().ReverseMap();
            CreateMap<OrderRoutesMaster, OrderRouteDTO>().ReverseMap();
            CreateMap<OrderRoutesMaster, OrderRouteUpdateDTO>().ReverseMap();
            CreateMap<OrderRoutesMaster, RouteUpdateDTO>().ReverseMap();
        }
    }
}
