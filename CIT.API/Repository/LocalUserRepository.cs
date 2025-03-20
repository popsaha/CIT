using AutoMapper;
using CIT.API.Context;
using CIT.API.Models;
using CIT.API.Models.Dto.Customer;
using CIT.API.Models.Dto.User;
using CIT.API.Repository.IRepository;
using Dapper;
using System.Data;

namespace CIT.API.Repository
{
    public class LocalUserRepository : ILocalUserRepository
    {
        private readonly DapperContext _db;
        private readonly ILogger<LocalUserRepository> _logger;
        private readonly string _secretKey;
        private readonly IMapper _mapper;

        public LocalUserRepository(DapperContext db, IMapper mapper, IConfiguration configuration, ILogger<LocalUserRepository> logger)
        {
            _db = db;
            _logger = logger;
            _mapper = mapper;
            _secretKey = configuration.GetValue<string>("ApiSettings:Secret");
        }

        public async Task<int> AddUser(LocalUserCreateDTO userCreateDTO)
        {
            try
            {
                
                using (var connection = _db.CreateConnection())
                {
                    DynamicParameters parameters = new DynamicParameters();

                    parameters.Add("Flag", "C");
                    parameters.Add("UserName", userCreateDTO.UserName);
                    parameters.Add("Password", userCreateDTO.Password);
                    parameters.Add("RegionName", userCreateDTO.RegionName);
                    parameters.Add("RoleName", userCreateDTO.RoleName);
                    parameters.Add("CreatedBy", 3);

                    // Call the stored procedure
                   int Res = await connection.ExecuteScalarAsync<int>(
                        "spLocalUser",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    // Return the result (Res will be the UserID if created)
                    return Res;
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding a User. UserName: {UserName}", userCreateDTO.UserName);
                throw;
            }
        }



        public async Task<int> DeleteUser(int userId, int deletedBy)
        {
            int Res = 0;
            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", "D");
                parameters.Add("DeletedBy", deletedBy);
                parameters.Add("UserID", userId);
                Res = await connection.ExecuteScalarAsync<int>("spLocalUser", parameters, commandType: CommandType.StoredProcedure);
            };
            return Res;
        }

        public async Task<IEnumerable<User>> GetAllUsers()
        {
            using (var con = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", "A");
                var user = await con.QueryAsync<User>("spLocalUser", parameters, commandType: CommandType.StoredProcedure);
                return user.ToList();
            }
        }

        public async Task<User> GetUser(int userId)
        {
            User user = new User();
            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", "R");
                parameters.Add("UserID", userId);
                user = await connection.QuerySingleOrDefaultAsync<User>("spLocalUser", parameters, commandType: CommandType.StoredProcedure);
            }
            return user;
        }

        public async Task<User> UpdateUser(User user)
        {
            int Res = 0;
            try
            {
                using (var connection = _db.CreateConnection())
                {
                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("Flag", "U");
                    parameters.Add("UserId", user.UserId);
                    parameters.Add("UserName", user.UserName);
                    parameters.Add("Password", user.Password);
                    parameters.Add("RoleName", user.RoleName);
                    parameters.Add("RegionName", user.RegionName);
                

                    Res = await connection.ExecuteScalarAsync<int>("spLocalUser", parameters, commandType: CommandType.StoredProcedure);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return user;
        }
    }
}
