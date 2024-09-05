using CIT.API.Models;

namespace CIT.API.StaticMethods
{
    public static class CommonResponse
    {
        public static CommonResponseModel Success(string remarks)
        {
            return new CommonResponseModel
            {
                Status = 200,
                Remarks = remarks
            };
        }

        public static CommonResponseModel Error(string message)
        {
            return new CommonResponseModel
            {
                Status = 500,
                Remarks = message
            };

        }
    }
}
