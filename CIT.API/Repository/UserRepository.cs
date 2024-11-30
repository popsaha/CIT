using AutoMapper;
using CIT.API.Context;
using CIT.API.Models;
using CIT.API.Models.Dto.UserMasterApi;
using CIT.API.Models.Dto.Login;
using CIT.API.Models.Dto.Registration;
using CIT.API.Models.Dto.UserMasterApi;
using CIT.API.Repository.IRepository;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CIT.API.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly DapperContext _db;
        private readonly ILogger<UserRepository> _logger;
        private string _secretKey;
        private readonly IMapper _mapper;
   
        public UserRepository(DapperContext db, IMapper mapper, IConfiguration configuration, ILogger<UserRepository> logger)
        {
            _db = db;
            _mapper = mapper;
            _secretKey = configuration.GetValue<string>("ApiSettings:Secret");
            _logger = logger;
        }
        public bool IsUniqueUser(string username)
        {
            try
            {
                using (var connection = _db.CreateConnection())
                {
                    string query = "SELECT COUNT(1) FROM UserMaster WHERE UserName = @Username";
                    var count = connection.ExecuteScalar<int>(query, new { Username = username });
                    return count == 0;
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL Error occurred while checking if username {Username} is unique", username);

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while checking if username {Username} is unique", username);

                return false;
            }

        }

        public async Task<LoginResponseDTO> Login(LoginRequestDTO loginRequestDTO)
        {
            try
            {
                using (var connection = _db.CreateConnection())
                {
                    string query = @"
                        SELECT u.UserId, u.UserName, r.RoleName AS Role, RegionID, u.UUID 
                        FROM UserMaster u
                        INNER JOIN UserRoleMapping urm ON u.UserId = urm.UserId
                        INNER JOIN RoleMaster r ON urm.RoleId = r.RoleId
                        INNER JOIN UserRegionMapping ON UserRegionMapping.UserID = u.UserID
                        WHERE LOWER(u.UserName) = LOWER(@UserName) AND u.Password = @Password";
                    var user = await connection.QueryFirstOrDefaultAsync<UserMaster>(query, new
                    {
                        UserName = loginRequestDTO.UserName,
                        Password = loginRequestDTO.Password
                    });
                    if (user == null)
                    {
                        return new LoginResponseDTO
                        {
                            User = null,
                            Token = ""
                        };
                    }

                    // if user was found Generate JWT token 
                    var token = GenerateJwtToken(user);

                    // If login is successful, return a successful response with user details
                    return new LoginResponseDTO
                    {
                        User = user,
                        Token = token
                    };
                }
            }
            catch (SqlException sqlEx)
            {
                // Log SQL exception (using Serilog, if configured)
                _logger.LogError(sqlEx, "SQL Error occurred while logging in user {UserName}", loginRequestDTO.UserName);
                throw;
            }
            catch (Exception ex)
            {
                // Log general exception
                _logger.LogError(ex, "An error occurred while logging in user {UserName}", loginRequestDTO.UserName);
                throw;
            }
        }

        private string GenerateJwtToken(UserMaster user)
        {
            // Define the token handler
            var tokenHandler = new JwtSecurityTokenHandler();

            // Secret key (in a real app, store securely, e.g., in appsettings or environment variables)
            var key = Encoding.ASCII.GetBytes(_secretKey);

            // Token descriptor
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, user.UserID.ToString()),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("regionID", user.RegionID.ToString()),
                    new Claim("uuid", user.UUID.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(1), // Token expiration
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            // Create token
            var token = tokenHandler.CreateToken(tokenDescriptor);

            // Return the JWT token string
            return tokenHandler.WriteToken(token);
        }


        public async Task<LocalUser> Register(RegisterationRequestDTO registerationRequestDTO)
        {
            try
            {
                using (var connection = _db.CreateConnection())
                {
                    string query = @"
                INSERT INTO LocalUser (UserName, Password, Name, Role)
                VALUES (@UserName, @Password, @Name, @Role);
                SELECT CAST(SCOPE_IDENTITY() as int);"; // For SQL Server to get the newly created UserId


                    var parameters = new
                    {
                        UserName = registerationRequestDTO.UserName,
                        Password = registerationRequestDTO.Password,
                        Name = registerationRequestDTO.Name,
                        Role = registerationRequestDTO.Role
                    };

                    // Execute query and get the new user's ID
                    var userId = await connection.ExecuteScalarAsync<int>(query, parameters);

                    return new LocalUser
                    {
                        Id = userId,
                        UserName = registerationRequestDTO.UserName,
                        Name = registerationRequestDTO.Name,
                        Role = registerationRequestDTO.Role,
                        Password = registerationRequestDTO.Password
                    };
                }
            }
            catch (SqlException sqlEx)
            {

                _logger.LogError(sqlEx, "SQL Error occurred while registering user {UserName}", registerationRequestDTO.UserName);
                throw;
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "An error occurred while registering user {UserName}", registerationRequestDTO.UserName);
                throw;
            }
        }

        public async Task<UserCreateDTO> CrewUserCreate(UserCreateDTO crewUserDTO)
        {
            try
            {
                using (var connection = _db.CreateConnection())
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@Flag", "C");
                    parameters.Add("@UserName", crewUserDTO.UserName);
                    parameters.Add("@Password", crewUserDTO.Password); // Store plain password
                    parameters.Add("@RoleName", crewUserDTO.RoleName); // Pass role name to fetch RoleID
                    parameters.Add("@CreatedBy", 1); // Replace with actual CreatedBy user ID

                    // Execute the stored procedure
                    await connection.ExecuteAsync("cit.spUserMaster", parameters, commandType: CommandType.StoredProcedure);

                    return crewUserDTO; // Return the input DTO as confirmation
                }
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL Error occurred while creating CrewUser {UserName}", crewUserDTO.UserName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating CrewUser {UserName}", crewUserDTO.UserName);
                throw;
            }
        }

        public async Task<IEnumerable<UserMasterModel>> GetAllUsers()
        {
            //IEnumerable<UserMaster> userList;

            using (var con = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", "A");
                var userList = await con.QueryAsync<UserMasterModel>("spUserMaster", parameters, commandType: CommandType.StoredProcedure);
                return userList.ToList();
            }
        }

        public async Task<UserMasterModel> UpdateUser(UserMasterModel usermaster)
        {
            int Res = 0;
            try
            {
                using (var connection = _db.CreateConnection())
                {
                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("Flag", "U");
                    parameters.Add("UserName", usermaster.UserName);
                    //parameters.Add("RoleName", usermaster.RoleName);
                    parameters.Add("Password", usermaster.Password);
                    parameters.Add("IsActive", usermaster.IsActive);

                    Res = await connection.ExecuteScalarAsync<int>("spUserMaster", parameters, commandType: CommandType.StoredProcedure);
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }
            return usermaster;
        }

        public async Task<UserMasterModel> GetUserById(int userId)
        {
            UserMasterModel customer = new UserMasterModel();
            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", "R");
                parameters.Add("UserID", userId);
                customer = await connection.QuerySingleOrDefaultAsync<UserMasterModel>("spUserMaster", parameters, commandType: CommandType.StoredProcedure);
            }
            return customer;
        }
        public async Task<int> DeleteUser(int userId)
        {
            int Res = 0;
            using (var connection = _db.CreateConnection())
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("Flag", "D");
                parameters.Add("UserId", userId);
                Res = await connection.ExecuteScalarAsync<int>("spUserMaster", parameters, commandType: CommandType.StoredProcedure);
            };
            return Res;
        }
    }

}
