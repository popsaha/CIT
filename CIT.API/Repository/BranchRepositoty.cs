using AutoMapper;
using CIT.API.Context;
using CIT.API.Models;
using CIT.API.Models.Dto.Branch;
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
        public async Task<int> AddBranch(BranchCreateDTO Branchdto)
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

                parameters.Add("CustomerID", Branchdto.CustomerID);
                parameters.Add("BranchCode", Branchdto.BranchCode);
                parameters.Add("ReferenceNo1", Branchdto.ReferenceNo1);
                parameters.Add("ReferenceNo2", Branchdto.ReferenceNo2);

                parameters.Add("Email", Branchdto.Email);
                parameters.Add("Country", Branchdto.Country);
                parameters.Add("City", Branchdto.City);
                parameters.Add("PostalCode", Branchdto.PostalCode);

                parameters.Add("Latitude", Branchdto.Latitude);
                parameters.Add("Longitude", Branchdto.Longitude);
                parameters.Add("CreatedBy", 1);
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
                parameters.Add("Flag", "D");
                parameters.Add("DeletedBy", deletedBy);
                parameters.Add("BranchID", branchId);
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
                branchMaster = await connection.QuerySingleOrDefaultAsync<BranchMaster>("spBranch", parameters, commandType: CommandType.StoredProcedure);
            }
            return branchMaster;
        }

        public async Task<int> UpdateBranch(BranchMaster branchDTO)
        {
            int Res = 0;
            try
            {
                using (var connection = _db.CreateConnection())
                {
                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("Flag", "U");
                    parameters.Add("BranchId", branchDTO.BranchID);
                    parameters.Add("BranchName", branchDTO.BranchName);
                    parameters.Add("Address", branchDTO.Address);
                    parameters.Add("ContactNumber", branchDTO.ContactNumber);
                    parameters.Add("ModifiedBy", branchDTO.CreatedBy);

                    parameters.Add("CustomerID", branchDTO.CustomerID);
                    parameters.Add("BranchCode", branchDTO.BranchCode);
                    parameters.Add("ReferenceNo1", branchDTO.ReferenceNo1);
                    parameters.Add("ReferenceNo2", branchDTO.ReferenceNo2);

                    parameters.Add("Email", branchDTO.Email);
                    parameters.Add("Country", branchDTO.Country);
                    parameters.Add("City", branchDTO.City);
                    parameters.Add("PostalCode", branchDTO.PostalCode);

                    parameters.Add("Latitude", branchDTO.Latitude);
                    parameters.Add("Longitude", branchDTO.Longitude);
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
