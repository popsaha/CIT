using AutoMapper;
using CIT.API.Context;
using CIT.API.Models;
using CIT.API.Models.Dto;
using CIT.API.Repository.IRepository;
using Dapper;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CIT.API.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly DapperContext _db;
        private readonly string _secretKey;
        private readonly IMapper _mapper;
        public UserRepository(DapperContext db, IMapper mapper, IConfiguration configuration)
        {
            _db = db;
            _mapper = mapper;
            _secretKey = configuration.GetValue<string>("ApiSettings:Secret");
        }
        public bool IsUniqueUser(string username)
        {
            using (var connection = _db.CreateConnection())
            {
                string query = "SELECT COUNT(1) FROM UserMaster WHERE UserName = @Username";
                var count = connection.ExecuteScalar<int>(query, new { Username = username });
                return count == 0;
            }
        }

        public async Task<LoginResponseDTO> Login(LoginRequestDTO loginRequestDTO)
        {
            using (var connection = _db.CreateConnection())
            {
                string query = "SELECT * FROM UserMaster WHERE LOWER(UserName) = LOWER(@Username) AND PasswordHash = @PasswordHash";

                var user = await connection.QuerySingleOrDefaultAsync<UserMaster>(query, new { Username = loginRequestDTO.UserName, PasswordHash = loginRequestDTO.Password });

                if (user == null)
                {
                    return new LoginResponseDTO()
                    {
                        Token = "",
                        User = null
                    };
                }

                // If user was found, generate JWT Token
                //var roles = await _userManager.GetRolesAsync(user);
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_secretKey);

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim(ClaimTypes.Name, user.UserName),
                        //new Claim(ClaimTypes.Role, user.Role)
                    }),
                    Expires = DateTime.UtcNow.AddDays(7),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                LoginResponseDTO loginResponseDTO = new LoginResponseDTO()
                {
                    Token = tokenHandler.WriteToken(token),
                    User = _mapper.Map<UserDTO>(user)
                };
                return loginResponseDTO;
            }
        }

        //public Task<UserDTO> Register(RegisterationRequestDTO registerationRequestDTO)
        //{
        //    using (var connection = _db.CreateConnection())
        //    {
        //        string query = "SELECT * FROM ApplicationUsers WHERE LOWER(UserName) = LOWER(@Username)";
        //        var user = connection.QuerySingleOrDefault<ApplicationUser>(query, new { Username = loginRequestDTO.UserName });

        //        if (user == null || !(await _userManager.CheckPasswordAsync(user, loginRequestDTO.Password)))
        //        {
        //            return new LoginResponseDTO
        //            {
        //                Token = "",
        //                User = null
        //            };
        //        }

        //        // If user was found, generate JWT Token
        //        var roles = await _userManager.GetRolesAsync(user);
        //        var tokenHandler = new JwtSecurityTokenHandler();
        //        var key = Encoding.ASCII.GetBytes(_secretKey);

        //        var tokenDescriptor = new SecurityTokenDescriptor
        //        {
        //            Subject = new ClaimsIdentity(new Claim[]
        //            {
        //                new Claim(ClaimTypes.Name, user.UserName),
        //                new Claim(ClaimTypes.Role, roles.FirstOrDefault())
        //            }),
        //            Expires = DateTime.UtcNow.AddDays(7),
        //            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        //        };

        //        var token = tokenHandler.CreateToken(tokenDescriptor);
        //        return new LoginResponseDTO
        //        {
        //            Token = tokenHandler.WriteToken(token),
        //            User = _mapper.Map<UserDTO>(user)
        //        };
        //    }
        //}
    }
}
