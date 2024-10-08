﻿using CIT.API.Context;
using CIT.API.Models;
using CIT.API.Repository;
using CIT.API.Repository.IRepository;

namespace CIT.API
{
    public static class ServiceRegistration
    {
        public static void AddRepositoryServices(this IServiceCollection services)
        {

            services.AddAutoMapper(typeof(MappingConfig));
            services.AddSingleton<DapperContext>();

            services.AddScoped<APIResponse>();

            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IOrderRouteRepository, OrderRouteRepository>();
            services.AddScoped<IBranchRepositoty, BranchRepositoty>();
            services.AddScoped<IOrderTypeRepository, OrderTypeRepository>();
            services.AddScoped<IRegionRepository, RegionRepository>();
            services.AddScoped<ITaskListRepository, TaskListRepository>();
            services.AddScoped<ITaskGroupRepository, TaskGroupRepository>();
            services.AddScoped<ITaskGroupListRepository, TaskGroupListRepository>();
            services.AddScoped<IVehiclesAssignmentRepository, VehiclesAssignmentRepository>();
            services.AddScoped<IVehicleRepository, VehicleRepository>();
            services.AddScoped<ICrewCommanderRepository, CrewCommanderRepository>();
            services.AddScoped<ITaskRepository, TaskRepository>();
            services.AddScoped<IOrderAssignmentRepository, OrderAssignmentRepository>();
        }
    }
}
