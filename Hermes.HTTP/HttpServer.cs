using System.Net;
using Hermes.HTTP.Utilities;

namespace Hermes.HTTP;

public class HttpServer
{
    private readonly HttpConfiguration _config;
    private HttpListener _listener;
    private bool _isRunning;
    private readonly Dictionary<string, Func<HttpListenerRequest, HttpListenerResponse, Task>> _endpoints;

    public HttpServer(HttpConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _listener = new HttpListener();
        _endpoints =
            new Dictionary<string, Func<HttpListenerRequest, HttpListenerResponse, Task>>(StringComparer
                .OrdinalIgnoreCase);
    }

    public void RegisterEndpoint(string path, Func<HttpListenerRequest, HttpListenerResponse, Task> handler)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException(nameof(path));

        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        path = path.StartsWith("/") ? path : $"/{path}";
        _endpoints[path] = handler;

        Logger.Info($"Registered endpoint: {path}");
    }

    public async Task StartAsync()
    {
        if (_isRunning)
            return;

        try
        {
            _listener = new HttpListener();

            foreach (var prefix in _config.Prefixes)
            {
                _listener.Prefixes.Add(prefix);
                Logger.Info($"Added prefix: {prefix}");
            }

            _listener.Start();
            _isRunning = true;

            Logger.Info($"HTTP server started on port {_config.Port}");

            await ProcessRequestsAsync();
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to start HTTP server: {ex.Message}");
            throw;
        }
    }

    public Task StopAsync()
    {
        if (!_isRunning)
            return Task.CompletedTask;

        try
        {
            _listener.Stop();
            _listener.Close();
            _isRunning = false;
            Logger.Info("HTTP server stopped");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error stopping HTTP server: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    private async Task ProcessRequestsAsync()
    {
        while (_isRunning)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleRequestAsync(context));
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error processing request: {ex.Message}");
            }
        }
    }
    
    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        string method = context.Request.HttpMethod;
        string path = context.Request.Url.AbsolutePath;

        try
        {
            var request = context.Request;
            var response = context.Response;

            if (_endpoints.TryGetValue(path, out var handler))
            {
                await handler(request, response);
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Close();
            }

            if (!path.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase))
            {
                Logger.Info($"Received request: {method} {path} - {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error handling request: {ex.Message}");
            try
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.Close();
            }
            catch {  }
        }
    }

}