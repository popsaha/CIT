namespace CIT.API.Data;

using System.Text.Json;

public class WebApiException: Exception
{
    public ErrorResponse? ErrorResponse { get; }

    public WebApiException(string errorJson)
    {
        ErrorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorJson);
    }
}
