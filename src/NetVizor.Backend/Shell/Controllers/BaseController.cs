using System;
using System.Text.Json;
using System.Threading.Tasks;
using Common.Net.HttpConn;
using Common.Net.Models;
using Common.Utils;

namespace Shell.Controllers;

public abstract class BaseController
{
    protected async Task WriteJsonResponseAsync<T>(HttpContext context, ResponseModel<T> response, int statusCode = 200)
    {
        context.Response.StatusCode = statusCode;
        await context.Response.WriteJsonAsync(response);
    }

    protected async Task WriteErrorResponseAsync(HttpContext context, string message, int statusCode = 400)
    {
        context.Response.StatusCode = statusCode;
        await context.Response.WriteJsonAsync(new ResponseModel<object>
        {
            Success = false,
            Message = message
        });
    }

    protected T? ParseRequestBody<T>(string requestBody) where T : class
    {
        if (string.IsNullOrEmpty(requestBody))
            return null;

        try
        {
            return JsonHelper.FromJson<T>(requestBody);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    protected string? GetHeaderValue(HttpContext context, string headerName)
    {
        return context.Request.Headers[headerName];
    }

    protected string GetQueryParam(HttpContext context, string paramName, string defaultValue = "")
    {
        return context.Request.QueryString[paramName] ?? defaultValue;
    }
}