

using CIT.API.Models;

namespace CIT.API.Data;

public interface IWebApiExecutor
{
    Task InvokeDelete(string relativeUrl);
    Task<T?> InvokeGet<T>(string relativeUrl);
    Task<T?> InvokePost<T>(string relativeUrl, T obj);
    Task<TResponse?> InvokePost<TRequest, TResponse>(string relativeUrl, TRequest obj);
    Task<TResponse?> InvokePostWithBearer<TRequest, TResponse>(string relativeUrl, TRequest obj);

    public Task<SmsApiResponseWrapper?> SendSmsOtpAsync(string mobile, string message);

}
