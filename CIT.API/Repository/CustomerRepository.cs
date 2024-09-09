﻿using AutoMapper;
using CIT.API.Context;
using CIT.API.Models;
using CIT.API.Models.Dto;
using CIT.API.Repository.IRepository;
using Dapper;
using System.Data;

namespace CIT.API.Repository
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly DapperContext _db;
        private readonly string _secretKey;
        private readonly IMapper _mapper;
        public CustomerRepository(DapperContext db, IMapper mapper, IConfiguration configuration)
        {
            _db = db;
            _mapper = mapper;
            _secretKey = configuration.GetValue<string>("ApiSettings:Secret");
        }
        public async Task<int> AddCustomer(CustomerDTO customerDTO)
        {
            int Res = 0;
            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();

                parameters.Add("Flag", "C");
                parameters.Add("CustomerName", customerDTO.CustomerName);
                parameters.Add("Address", customerDTO.Address);
                parameters.Add("ContactNumber", customerDTO.ContactNumber);
                parameters.Add("Email", customerDTO.Email);
                parameters.Add("DataSource", customerDTO.DataSource);
                parameters.Add("IsActive", customerDTO.IsActive);
                parameters.Add("CreatedBy", customerDTO.CreatedBy);
                parameters.Add("ModifiedBy", customerDTO.ModifiedBy);
                parameters.Add("DeletedBy", customerDTO.DeletedBy);
                Res = await connection.ExecuteScalarAsync<int>("spCustomer", parameters, commandType: CommandType.StoredProcedure);
            };
            return Res;
        }

        public async Task<IEnumerable<Customer>> GetCustomers()
        {
            using (var con = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", "A");
                var customers = await con.QueryAsync<Customer>("spCustomer", parameters, commandType: CommandType.StoredProcedure);
                return customers.ToList();
            }
        }
        public async Task<Customer> GetCustomer(int customerid)
        {
            Customer customer = new Customer();
            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", "R");
                parameters.Add("CustomerID", customerid);
                customer = await connection.ExecuteScalarAsync<Customer>("spCustomer", parameters, commandType: CommandType.StoredProcedure);
            }
            return customer;
        }
        public async Task<int> DeleteCustomer(int customerid, int deletedBy)
        {
            int Res = 0;
            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", "D");
                parameters.Add("DeletedBy", deletedBy);
                parameters.Add("CustomerID", customerid);
                Res = await connection.ExecuteScalarAsync<int>("spCustomer", parameters, commandType: CommandType.StoredProcedure);
            };
            return Res;
        }
        public async Task<int> UpdateCustomer(CustomerDTO customerDTO)
        {
            int Res = 0;
            try
            {
                using (var connection = _db.CreateConnection())
                {
                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("Flag", "U");
                    parameters.Add("CustomerName", customerDTO.CustomerName);
                    parameters.Add("Address", customerDTO.Address);
                    parameters.Add("ContactNumber", customerDTO.ContactNumber);
                    parameters.Add("Email", customerDTO.Email);
                    parameters.Add("ModifiedBy", customerDTO.ModifiedBy);
                    Res = await connection.ExecuteScalarAsync<int>("spCustomer", parameters, commandType: CommandType.StoredProcedure);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return Res;
        }

    }
}
