using System.Text;
using System.Security.Cryptography;
namespace CIT.API.Utility
{
    public class OtpHelper
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
                using var sha = SHA256.Create();
                var bytes = Encoding.UTF8.GetBytes(otp + salt);
                var hash = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }

            public async Task CreateOtp()
            {
                string otp = OtpHelper.GenerateOtp();
                string salt = OtpHelper.GenerateSalt();
                string hash = OtpHelper.HashOtp(otp, salt);
            }
    }
}
