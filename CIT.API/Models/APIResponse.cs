using System.Net;

namespace CIT.API.Models
{
    public class APIResponse
    {
        public APIResponse()
        {
            ErrorMessages = new List<string>();
        }
        public HttpStatusCode StatusCode { get; set; }
        public bool IsSuccess { get; set; } = true;
        public List<string> ErrorMessages { get; set; }
        public object Result {  get; set; }
        //public bool otpCheck { get; set; }

    }

    public class APIOtpResponse
    {
        public APIOtpResponse()
        {
            ErrorMessages = new List<string>();
        }
        public HttpStatusCode StatusCode { get; set; }
        public bool IsSuccess { get; set; } = true;
        public bool otpRequired { get; set; }
        public Guid otpTransactionId { get; set; }
        //public string otp { get; set; }
        public List<string> ErrorMessages { get; set; }
        public object Result { get; set; }
    }
    public class APIOtpValidateResponse
    {
        public HttpStatusCode StatusCode { get; set; }
        public bool IsSuccess { get; set; }
        public List<string> ErrorMessages { get; set; } = new();
        public object Result { get; set; }

        public bool otpValidated { get; set; }   // ← NEW property
    }

}
