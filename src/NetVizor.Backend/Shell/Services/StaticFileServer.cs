using System;
using System.IO;
using System.Net;
using System.Threading;
using Common;

namespace Shell.Services;

public class StaticFileServer : IDisposable
{
    private HttpListener? _httpListener;
    private string? _webServerPath;
    private bool _isDisposed;

    public string? WebServerPath => _webServerPath;

    public void Start(int port)
    {
        _httpListener = new HttpListener();
        _webServerPath = $"http://127.0.0.1:{port}/";
        _httpListener.Prefixes.Add(_webServerPath);
        _httpListener.Start();

        ThreadPool.QueueUserWorkItem((_) =>
        {
            while (_httpListener.IsListening && !_isDisposed)
            {
                try
                {
                    var context = _httpListener.GetContext();
                    ProcessRequest(context);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"HTTP Server Error: {ex.Message}");
                }
            }
        });
    }

    private void ProcessRequest(HttpListenerContext context)
    {
        string urlPath = context.Request.Url.AbsolutePath.TrimStart('/');

        if (string.IsNullOrEmpty(urlPath))
        {
            urlPath = "index.html";
        }

        string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", urlPath);

        if (File.Exists(localPath))
        {
            ServeFile(context, localPath);
        }
        else
        {
            HandleFileNotFound(context, urlPath);
        }
    }

    private void ServeFile(HttpListenerContext context, string localPath)
    {
        byte[] buffer = File.ReadAllBytes(localPath);
        context.Response.ContentType = GetContentType(localPath);
        context.Response.ContentLength64 = buffer.Length;
        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
        context.Response.OutputStream.Close();
    }

    private void HandleFileNotFound(HttpListenerContext context, string urlPath)
    {
        if (IsStaticResource(urlPath))
        {
            context.Response.StatusCode = 404;
            context.Response.Close();
        }
        else
        {
            ServeSpaFallback(context);
        }
    }

    private void ServeSpaFallback(HttpListenerContext context)
    {
        string indexPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "index.html");
        if (File.Exists(indexPath))
        {
            byte[] buffer = File.ReadAllBytes(indexPath);
            context.Response.ContentType = "text/html";
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }
        else
        {
            context.Response.StatusCode = 404;
            context.Response.Close();
        }
    }

    private bool IsStaticResource(string path)
    {
        string[] staticExtensions =
        {
            ".js", ".css", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".svg", ".woff", ".woff2", ".ttf", ".eot", ".map"
        };
        string extension = Path.GetExtension(path).ToLower();
        return staticExtensions.Contains(extension);
    }

    private string GetContentType(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLower();
        return extension switch
        {
            ".html" or ".htm" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".mjs" => "application/javascript",
            ".json" => "application/json",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            ".ttf" => "font/ttf",
            ".eot" => "application/vnd.ms-fontobject",
            ".map" => "application/json",
            _ => "application/octet-stream"
        };
    }

    public void Stop()
    {
        _httpListener?.Stop();
        _httpListener?.Close();
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            Stop();
            _httpListener?.Close();
            _isDisposed = true;
        }
    }
}