using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace CIT.API.Controllers
{
    public class OtpRecordsController : Controller 
    {
            public static string GenerateOtp(int length = 6)
            {
                var random = new Random();
                return random.Next((int)Math.Pow(10, length - 1),
                                   (int)Math.Pow(10, length) - 1).ToString();
            }

            public static string GenerateSalt()
            {
                return Guid.NewGuid().ToString("N");
            }

            public static string HashOtp(string otp, string salt)
            {
                using var sha = System.Security.Cryptography.SHA256.Create();
                var bytes = Encoding.UTF8.GetBytes(otp + salt);
                var hash = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        

    }
}
