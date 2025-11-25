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
        public long mobile { get; set; }
        public string messageid { get; set; }
        public int networkid { get; set; }
    }


}
