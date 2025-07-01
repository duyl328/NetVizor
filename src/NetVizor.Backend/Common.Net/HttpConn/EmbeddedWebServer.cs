using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Web;
using Common.Utils;
using Fleck;

namespace Common.Net.HttpConn;

// HTTP 方法枚举
public enum HttpMethod
{
    GET,
    POST,
    PUT,
    DELETE,
    PATCH,
    HEAD,
    OPTIONS
}

// 路由处理委托
public delegate Task RequestHandler(HttpContext context);

// 中间件委托
public delegate Task<bool> MiddlewareHandler(HttpContext context);

// HTTP 上下文
public class HttpContext
{
    public HttpListenerRequest Request { get; set; }
    public HttpListenerResponse Response { get; set; }
    public Dictionary<string, object> Items { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, string> RouteParams { get; set; } = new Dictionary<string, string>();
    public NameValueCollection QueryParams { get; set; }
    public string RequestBody { get; set; }
    public T GetRequestBody<T>() => JsonHelper.FromJson<T>(RequestBody);
}

// 路由信息
public class Route
{
    public string Pattern { get; set; }
    public HttpMethod Method { get; set; }
    public RequestHandler Handler { get; set; }
    public List<string> ParamNames { get; set; } = new List<string>();
}

/// <summary>
/// HTTP 服务器主类
/// </summary>
public class HttpServer
{
    private readonly HttpListener _listener;
    private readonly List<Route> _routes = new List<Route>();
    private readonly List<MiddlewareHandler> _middlewares = new List<MiddlewareHandler>();
    private readonly string _prefix;
    private bool _isRunning;
    private readonly SemaphoreSlim _concurrencySemaphore;

    public HttpServer(string prefix = "http://localhost:8080/", int maxConcurrentRequests = 100)
    {
        _prefix = prefix.EndsWith("/") ? prefix : prefix + "/";
        _listener = new HttpListener();
        _listener.Prefixes.Add(_prefix);
        _concurrencySemaphore = new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);
    }

    /// <summary>
    /// 添加中间件
    /// </summary>
    /// <param name="middleware"></param>
    public void UseMiddleware(MiddlewareHandler middleware)
    {
        _middlewares.Add(middleware);
    }

    /// <summary>
    /// 添加路由
    /// </summary>
    public void AddRoute(HttpMethod method, string pattern, RequestHandler handler)
    {
        var route = new Route
        {
            Method = method,
            Pattern = pattern,
            Handler = handler
        };

        // 解析路由参数
        var parts = pattern.Split('/');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].StartsWith("{") && parts[i].EndsWith("}"))
            {
                route.ParamNames.Add(parts[i].Trim('{', '}'));
            }
        }

        _routes.Add(route);
    }

    /// <summary>
    /// 便捷路由方法
    /// </summary>
    public void Get(string pattern, RequestHandler handler) => AddRoute(HttpMethod.GET, pattern, handler);
    public void Post(string pattern, RequestHandler handler) => AddRoute(HttpMethod.POST, pattern, handler);
    public void Put(string pattern, RequestHandler handler) => AddRoute(HttpMethod.PUT, pattern, handler);
    public void Delete(string pattern, RequestHandler handler) => AddRoute(HttpMethod.DELETE, pattern, handler);

    /// <summary>
    /// 启动服务器
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _listener.Start();
        _isRunning = true;
        Console.WriteLine($"HTTP Server started at {_prefix}");

        while (_isRunning && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var listenerContext = await _listener.GetContextAsync().ConfigureAwait(false);
                _ = Task.Run(async () => await HandleRequestAsync(listenerContext), cancellationToken);
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 995) // Listener stopped
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accepting request: {ex.Message}");
            }
        }
    }

    // 停止服务器
    public void Stop()
    {
        _isRunning = false;
        _listener.Stop();
        _listener.Close();
        Console.WriteLine("HTTP Server stopped");
    }

    // 处理请求
    private async Task HandleRequestAsync(HttpListenerContext listenerContext)
    {
        await _concurrencySemaphore.WaitAsync();
        try
        {
            var context = await CreateHttpContext(listenerContext);

            // 执行中间件
            foreach (var middleware in _middlewares)
            {
                if (!await middleware(context))
                {
                    return; // 中间件返回 false，停止处理
                }
            }

            // 匹配路由并执行处理器
            var route = MatchRoute(context);
            if (route != null)
            {
                await route.Handler(context);
            }
            else
            {
                await SendNotFound(context);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Request handling error: {ex}");
            await SendInternalServerError(listenerContext.Response, ex);
        }
        finally
        {
            try
            {
                listenerContext.Response.Close();
            }
            catch
            {
            }

            _concurrencySemaphore.Release();
        }
    }

    // 创建 HTTP 上下文
    private async Task<HttpContext> CreateHttpContext(HttpListenerContext listenerContext)
    {
        var context = new HttpContext
        {
            Request = listenerContext.Request,
            Response = listenerContext.Response,
            QueryParams = listenerContext.Request.QueryString
        };

        // 读取请求体
        if (listenerContext.Request.HasEntityBody)
        {
            using (var reader = new StreamReader(listenerContext.Request.InputStream,
                       listenerContext.Request.ContentEncoding))
            {
                context.RequestBody = await reader.ReadToEndAsync();
            }
        }

        return context;
    }

    // 匹配路由
    private Route MatchRoute(HttpContext context)
    {
        var method = Enum.Parse<HttpMethod>(context.Request.HttpMethod);
        var path = context.Request.Url.AbsolutePath;

        foreach (var route in _routes.Where(r => r.Method == method))
        {
            if (TryMatchRoute(route, path, out var routeParams))
            {
                context.RouteParams = routeParams;
                return route;
            }
        }

        return null;
    }

    // 尝试匹配路由模式
    private bool TryMatchRoute(Route route, string path, out Dictionary<string, string> routeParams)
    {
        routeParams = new Dictionary<string, string>();

        var routeParts = route.Pattern.Split('/');
        var pathParts = path.Split('/');

        if (routeParts.Length != pathParts.Length)
            return false;

        for (int i = 0; i < routeParts.Length; i++)
        {
            if (routeParts[i].StartsWith("{") && routeParts[i].EndsWith("}"))
            {
                var paramName = routeParts[i].Trim('{', '}');
                routeParams[paramName] = HttpUtility.UrlDecode(pathParts[i]);
            }
            else if (routeParts[i] != pathParts[i])
            {
                return false;
            }
        }

        return true;
    }

    // 发送 404 响应
    private async Task SendNotFound(HttpContext context)
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteJsonAsync(new { error = "Not Found" });
    }

    // 发送 500 响应
    private async Task SendInternalServerError(HttpListenerResponse response, Exception ex)
    {
        response.StatusCode = 500;
        await response.WriteJsonAsync(new { error = "Internal Server Error", message = ex.Message });
    }
}

// 响应扩展方法
public static class HttpListenerResponseExtensions
{
    public static async Task WriteJsonAsync<T>(this HttpListenerResponse response, T data)
    {
        response.ContentType = "application/json; charset=utf-8";
        var json = JsonHelper.ToJson(data);
        
        var buffer = Encoding.UTF8.GetBytes(json);
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
    }

    public static async Task WriteTextAsync(this HttpListenerResponse response, string text)
    {
        response.ContentType = "text/plain; charset=utf-8";
        var buffer = Encoding.UTF8.GetBytes(text);
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
    }

    public static async Task WriteHtmlAsync(this HttpListenerResponse response, string html)
    {
        response.ContentType = "text/html; charset=utf-8";
        var buffer = Encoding.UTF8.GetBytes(html);
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
    }
}

// 常用中间件
public static class Middlewares
{
    // CORS 中间件
    public static async Task<bool> Cors(HttpContext context)
    {
        
        // 允许所有来源（生产环境应该指定具体域名）
        string? origin = context.Request.Headers["Origin"];
        context.Response.Headers.Add("Access-Control-Allow-Origin", !string.IsNullOrEmpty(origin) ? origin : "*");

        // 允许的HTTP方法
        context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS, PATCH");

        // 关键：允许的请求头（包括你的uuid头）
        context.Response.Headers.Add("Access-Control-Allow-Headers", 
            "Content-Type, Authorization, X-Requested-With, uuid, Accept, Origin");

        // 允许携带凭据（如果需要）
        context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");

        // 预检请求缓存时间（秒）
        context.Response.Headers.Add("Access-Control-Max-Age", "86400");

        // 允许前端访问的响应头
        context.Response.Headers.Add("Access-Control-Expose-Headers", "Content-Length, Content-Type");

        if (context.Request.HttpMethod == "OPTIONS")
        {
            context.Response.StatusCode = 204;
            return false; // 停止后续处理
        }

        return true;
    }

    // 请求日志中间件
    public static async Task<bool> RequestLogging(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        Console.WriteLine($"[{startTime:yyyy-MM-dd HH:mm:ss}] {context.Request.HttpMethod} {context.Request.Url}");

        // 继续处理
        return await Task.FromResult(true);
    }

    // 认证中间件示例
    public static async Task<bool> Authentication(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"];
        if (string.IsNullOrEmpty(authHeader))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteJsonAsync(new { error = "Unauthorized" });
            return false;
        }

        // TODO: 验证 token
        // if (!ValidateToken(authHeader)) { ... }

        return true;
    }
}