using AutoMapper;
using CIT.API.Context;
using CIT.API.Models;
using CIT.API.Models.Dto.Login;
using CIT.API.Models.Dto.Registration;
using CIT.API.Repository.IRepository;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
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
                    string query = @"SELECT Id, UserName, Name, Role 
                                    FROM LocalUser 
                                    WHERE LOWER(UserName) = LOWER(@UserName) AND Password = @Password";

                    var user = await connection.QueryFirstOrDefaultAsync<LocalUser>(query, new
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

        private string GenerateJwtToken(LocalUser user)
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
                    new Claim(ClaimTypes.Name, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddDays(7), // Token expiration
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


    }
}
