namespace CIT.API.Models
{
    public class SmsApiResponseWrapper
    {
        public List<SmsApiResponse> responses { get; set; }
    }

    public class SmsApiResponse
    {
        public int response_code { get; set; }
        public string response_description { get; set; }
        public string mobile { get; set; }
        public int messageid { get; set; }
        public int networkid { get; set; }
    }


}
