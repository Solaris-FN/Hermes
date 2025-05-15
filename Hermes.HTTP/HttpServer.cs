using System.Net;
using Hermes.HTTP.Utilities;

namespace Hermes.HTTP;

public class HttpServer
{
    private readonly HttpConfiguration _config;
    private HttpListener _listener;
    private bool _isRunning;
    private readonly Dictionary<string, Func<HttpListenerRequest, HttpListenerResponse, Dictionary<string, string>, Task>> _endpoints;

    public HttpServer(HttpConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _listener = new HttpListener();
        _endpoints = new(StringComparer.OrdinalIgnoreCase);
    }

    public void RegisterEndpoint(string routeTemplate, Func<HttpListenerRequest, HttpListenerResponse, Dictionary<string, string>, Task> handler)
    {
        if (string.IsNullOrEmpty(routeTemplate))
            throw new ArgumentNullException(nameof(routeTemplate));

        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        routeTemplate = routeTemplate.StartsWith("/") ? routeTemplate : $"/{routeTemplate}";
        _endpoints[routeTemplate] = handler;

        Logger.Info($"Registered endpoint: {routeTemplate}");
    }

    public async Task StartAsync()
    {
        if (_isRunning) return;

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
            Logger.Error($"Failed to start HTTP server: {ex.Message} | {ex.StackTrace}");
            throw;
        }
    }

    public Task StopAsync()
    {
        if (!_isRunning) return Task.CompletedTask;

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
            catch (HttpListenerException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (Exception ex)
            {
                Logger.Error($"Error processing request: {ex.Message}");
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        string method = request.HttpMethod;
        string path = request.Url.AbsolutePath;

        try
        {
            foreach (var kvp in _endpoints)
            {
                var routeTemplate = kvp.Key;
                var handler = kvp.Value;

                if (RouteMatcher.TryMatch(routeTemplate, path, out var routeParams))
                {
                    await handler(request, response, routeParams);
                    LogRequest(method, path, response.StatusCode);
                    return;
                }
            }

            response.StatusCode = (int)HttpStatusCode.NotFound;
            response.Close();
            LogRequest(method, path, response.StatusCode);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error handling request: {ex}");
            try
            {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Close();
            }
            catch { }
        }
    }

    private void LogRequest(string method, string path, int statusCode)
    {
        if (!path.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase))
        {
            Logger.Info($"Received request: {method} {path} - {statusCode}");
        }
    }
}
