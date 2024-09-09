﻿using AutoMapper;
using CIT.API.Context;
using CIT.API.Models;
using CIT.API.Models.Dto;
using CIT.API.Repository.IRepository;
using Dapper;
using System.Data;

namespace CIT.API.Repository
{
    public class BranchRepositoty : IBranchRepositoty
    {
        private readonly DapperContext _db;
        private readonly string _secretKey;
        private readonly IMapper _mapper;
        public BranchRepositoty(DapperContext db, IMapper mapper, IConfiguration configuration)
        {
            _db = db;
            _mapper = mapper;
            _secretKey = configuration.GetValue<string>("ApiSettings:Secret");
        }
        public async Task<int> AddBranch(BranchDTO Branchdto)
        {
            int Res = 0;
            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();

                parameters.Add("Flag", "C");
                parameters.Add("BranchName", Branchdto.BranchName);
                parameters.Add("Address", Branchdto.Address);
                parameters.Add("ContactNumber", Branchdto.ContactNumber);
                parameters.Add("DataSource", Branchdto.DataSource);
                parameters.Add("IsActive", Branchdto.IsActive);
                parameters.Add("CreatedBy", Branchdto.CreatedBy);
                parameters.Add("ModifiedBy", Branchdto.ModifiedBy);
                parameters.Add("DeletedBy", Branchdto.DeletedBy);
                Res = await connection.ExecuteScalarAsync<int>("spBranch", parameters, commandType: CommandType.StoredProcedure);
            };
            return Res;
        }

        public async Task<int> DeleteBranch(int branchId, int deletedBy)
        {
            int Res = 0;
            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("DeletedBy", deletedBy);
                Res = await connection.ExecuteScalarAsync<int>("spBranch", parameters, commandType: CommandType.StoredProcedure);
            };
            return Res;
        }

        public async Task<IEnumerable<BranchMaster>> GetAllBranch()
        {
            IEnumerable<BranchMaster> branchMasterlist;

            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", "A");
                branchMasterlist = await connection.QueryAsync<BranchMaster>("spBranch", parameters, commandType: CommandType.StoredProcedure);
            }
            return branchMasterlist;
        }

        public async Task<BranchMaster> GetBranch(int branchId)
        {
            BranchMaster branchMaster = new BranchMaster();
            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", "R");
                parameters.Add("BranchId", branchId);
                branchMaster = await connection.ExecuteScalarAsync<BranchMaster>("spBranch", parameters, commandType: CommandType.StoredProcedure);
            }
            return branchMaster;
        }

        public async Task<int> UpdateBranch(BranchDTO branchDTO)
        {
            int Res = 0;
            try
            {
                using (var connection = _db.CreateConnection())
                {
                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("Flag", "D");
                    parameters.Add("BranchName", branchDTO.BranchName);
                    parameters.Add("Address", branchDTO.Address);
                    parameters.Add("ContactNumber", branchDTO.ContactNumber);
                    parameters.Add("ModifiedBy", branchDTO.ModifiedBy);
                    Res = await connection.ExecuteScalarAsync<int>("spBranch", parameters, commandType: CommandType.StoredProcedure);
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
