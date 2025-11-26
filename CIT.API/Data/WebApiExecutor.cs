using CIT.API.Data;
using CIT.API.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CIT.API.Data;

public class WebApiExecutor : IWebApiExecutor
{
    private const string apiName = "AxSapApi";
    private const string authApiName = "AuthorityApi";
    private const string priceApiName = "PriceApi";
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IConfiguration configuration;
    private readonly ILogger<WebApiExecutor> _logger;
    private string? _cachedBearerToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public WebApiExecutor(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<WebApiExecutor> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.configuration = configuration;
        _logger = logger;
    }

    public async Task<T?> InvokeGet<T>(string relativeUrl)
    {
        var httpClient = httpClientFactory.CreateClient(apiName);
        AddBasicAuthHeader(httpClient); ;
        var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
        var response = await httpClient.SendAsync(request);
        await HandlePotentialError(response);

        return await response.Content.ReadFromJsonAsync<T>();
    }

    public async Task<TResponse?> InvokePost<TRequest, TResponse>(string relativeUrl, TRequest obj)
    {
        var httpClient = httpClientFactory.CreateClient(apiName);
        AddBasicAuthHeader(httpClient);

        var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            PropertyNamingPolicy = null // send PascalCase
        });

        Console.WriteLine($"Request JSON: {json}");
        _logger?.LogInformation("API Request to {Url}: {Json}", relativeUrl, json);

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(relativeUrl, content);
        var responseText = await response.Content.ReadAsStringAsync();


        _logger?.LogInformation("API Response from {Url} - Status: {StatusCode}, Content Length: {Length}",
       relativeUrl, response.StatusCode, responseText?.Length ?? 0);

        // Log first 500 characters of response for debugging
        var logResponse = responseText?.Length > 500 ? responseText.Substring(0, 500) + "..." : responseText;
        _logger?.LogInformation("API Response Content: {Response}", logResponse);

        await HandlePotentialError(response);

        try
        {
            var deserializedResponse = JsonSerializer.Deserialize<TResponse>(responseText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger?.LogInformation("Successfully deserialized response of type {Type}", typeof(TResponse).Name);
            return deserializedResponse;
        }
        catch (JsonException jsonEx)
        {
            _logger?.LogError(jsonEx, "Failed to deserialize response. Response text: {ResponseText}", responseText);
            throw new JsonException($"Failed to deserialize API response: {jsonEx.Message}", jsonEx);
        }
    }

    //POST with Bearer token
    public async Task<TResponse?> InvokePostWithBearer<TRequest, TResponse>(string relativeUrl, TRequest obj)
    {
        var httpClient = httpClientFactory.CreateClient(priceApiName);
        await AddBearerAuthHeader(httpClient);

        var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase // send camelCase
        });

        var fullUrl = new Uri(httpClient.BaseAddress, relativeUrl).ToString();

        Console.WriteLine($"Request JSON: {json}");
        _logger?.LogInformation("API Request with Bearer - URL: {FullUrl}, Body: {Json}", fullUrl, json);
        _logger?.LogDebug("API Request with Bearer - URL: {FullUrl}, Body: {Json}", fullUrl, json);

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(relativeUrl, content);
        var responseText = await response.Content.ReadAsStringAsync();


        _logger?.LogInformation("API Response from {Url} - Status: {StatusCode}, Content Length: {Length}",
       relativeUrl, response.StatusCode, responseText?.Length ?? 0);

        // Log first 500 characters of response for debugging
        var logResponse = responseText?.Length > 500 ? responseText.Substring(0, 500) + "..." : responseText;
        _logger?.LogInformation("API Response Content: {Response}", logResponse);

        await HandlePotentialError(response);

        try
        {
            var deserializedResponse = JsonSerializer.Deserialize<TResponse>(responseText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger?.LogInformation("Successfully deserialized response of type {Type}", typeof(TResponse).Name);
            return deserializedResponse;
        }
        catch (JsonException jsonEx)
        {
            _logger?.LogError(jsonEx, "Failed to deserialize response. Response text: {ResponseText}", responseText);
            throw new JsonException($"Failed to deserialize API response: {jsonEx.Message}", jsonEx);
        }
    }

    public async Task<T?> InvokePost<T>(string relativeUrl, T obj)
    {
        var httpClient = httpClientFactory.CreateClient(apiName);
        AddBasicAuthHeader(httpClient);

        var response = await httpClient.PostAsJsonAsync(relativeUrl, obj);

        await HandlePotentialError(response);

        return await response.Content.ReadFromJsonAsync<T>();
    }

    public async Task<T?> InvokePostWithBearer<T>(string relativeUrl, T obj)
    {
        var httpClient = httpClientFactory.CreateClient(priceApiName);
        await AddBearerAuthHeader(httpClient);

        var response = await httpClient.PostAsJsonAsync(relativeUrl, obj);

        await HandlePotentialError(response);

        return await response.Content.ReadFromJsonAsync<T>();
    }

    public async Task InvokePut<T>(string relativeUrl, T obj)
    {
        var httpClient = httpClientFactory.CreateClient(apiName);
        AddBasicAuthHeader(httpClient);
        var response = await httpClient.PutAsJsonAsync(relativeUrl, obj);
        await HandlePotentialError(response);
    }

    public async Task InvokeDelete(string relativeUrl)
    {
        var httpClient = httpClientFactory.CreateClient(apiName);
        AddBasicAuthHeader(httpClient);
        var response = await httpClient.DeleteAsync(relativeUrl);
        await HandlePotentialError(response);
    }

    private async Task HandlePotentialError(HttpResponseMessage httpResponse)
    {
        if (!httpResponse.IsSuccessStatusCode)
        {
            var errorJson = await httpResponse.Content.ReadAsStringAsync();
            var message = $"HTTP {(int)httpResponse.StatusCode} - {httpResponse.ReasonPhrase}. Response: {errorJson}";
            _logger?.LogError("API call failed: {Message}", message);
            throw new WebApiException(message);
        }
    }


    private void AddBasicAuthHeader(HttpClient httpClient)
    {
        var username = configuration.GetValue<string>("SapAPI:Username");
        var password = configuration.GetValue<string>("SapAPI:Password");

        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        }
    }

    private async Task<string> GetBearerTokenAsync()
    {
        // Check if we have a valid cached token
        if (!string.IsNullOrEmpty(_cachedBearerToken) && DateTime.UtcNow < _tokenExpiry)
        {
            _logger?.LogInformation("Using cached Bearer token");
            return _cachedBearerToken;
        }

        _logger?.LogInformation("Fetching new Bearer token from authentication API");

        var httpClient = httpClientFactory.CreateClient(authApiName);

        var authRequest = new
        {
            username = configuration.GetValue<string>("AuthAPI:Username") ?? "MOH.USER",
            password = configuration.GetValue<string>("AuthAPI:Password") ?? "VeRrzeUOzXM8"
        };

        var json = JsonSerializer.Serialize(authRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await httpClient.PostAsync("api/authenticate", content);

            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogError("Authentication failed with status: {StatusCode}", response.StatusCode);
                throw new WebApiException("Failed to authenticate and retrieve Bearer token");
            }

            var token = await response.Content.ReadAsStringAsync();

            // Remove quotes if the response is a quoted string
            token = token.Trim('"');

            _cachedBearerToken = token;

            // Set expiry time (tokens typically expire in 10 hours based on JWT)
            // Refresh 5 minutes before actual expiry for safety
            _tokenExpiry = DateTime.UtcNow.AddHours(9).AddMinutes(55);

            _logger?.LogInformation("Successfully obtained Bearer token, expires at {Expiry}", _tokenExpiry);

            return token;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during authentication");
            throw;
        }
    }
    // Add Bearer token header to HttpClient
    private async Task AddBearerAuthHeader(HttpClient httpClient)
    {
        var token = await GetBearerTokenAsync();
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }


    //public async Task<SmsApiResponseWrapper?> SendSmsOtpAsync(string mobile, string message)
    //{
    //    var httpClient = httpClientFactory.CreateClient("SmsApi");

    //    var apiKey = configuration["SmsAPI:ApiKey"];
    //    var partnerId = configuration["SmsAPI:PartnerID"];
    //    var shortcode = configuration["SmsAPI:ShortCode"];

    //    string encodedMessage = Uri.EscapeDataString(message);

    //    string query =
    //        $"?apikey={apiKey}&partnerID={partnerId}&message={encodedMessage}&shortcode={shortcode}&mobile={mobile}";

    //    var response = await httpClient.GetAsync(query);
    //    var json = await response.Content.ReadAsStringAsync();

    //    if (!response.IsSuccessStatusCode)
    //        throw new Exception($"SMS API failed: {json}");

    //    var smsResponse = JsonSerializer.Deserialize<SmsApiResponseWrapper>(
    //        json,
    //        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    //    );

    //    return smsResponse;
    //}

    public async Task<SmsApiResponseWrapper?> SendSmsOtpAsync(string mobile, string message)
    {
        var httpClient = httpClientFactory.CreateClient("SmsApi");

        var apiKey = configuration["SmsAPI:ApiKey"];
        var partnerId = configuration["SmsAPI:PartnerID"];
        var shortcode = configuration["SmsAPI:ShortCode"];

        string encodedMessage = Uri.EscapeDataString(message);

        string query =
            $"?apikey={apiKey}&partnerID={partnerId}&message={encodedMessage}&shortcode={shortcode}&mobile={mobile}";

        var fullUrl = httpClient.BaseAddress + query;

        _logger?.LogInformation("?? Sending SMS OTP request: {Url}", fullUrl);

        var response = await httpClient.GetAsync(query);
        var json = await response.Content.ReadAsStringAsync();

        _logger?.LogInformation("?? SMS API Status: {StatusCode}", response.StatusCode);

        // Log trimmed raw response
        var trimmedResponse = json.Length > 500 ? json.Substring(0, 500) + "..." : json;
        _logger?.LogInformation("?? SMS API Response JSON (raw): {Response}", trimmedResponse);

        if (!response.IsSuccessStatusCode)
        {
            _logger?.LogError("? SMS API FAILED. Status: {Status}, Response: {Response}",
                response.StatusCode, trimmedResponse);

            throw new Exception($"SMS API failed: {json}");
        }

        var smsResponse = JsonSerializer.Deserialize<SmsApiResponseWrapper>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        // Log full deserialized object as JSON
        _logger?.LogInformation("?? Parsed SmsResponse JSON:\n{Json}",
            JsonSerializer.Serialize(smsResponse, new JsonSerializerOptions
            {
                WriteIndented = true
            }));

        return smsResponse;
    }


}
