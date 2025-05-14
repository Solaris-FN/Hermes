using System.Net;
using System.Text;
using Hermes.Api.Utilities;
using Hermes.HTTP;

namespace Hermes.Api;

public class ApiServer
{
    private readonly ServerConfig _config;
    private readonly HttpServer _httpServer;

    public ApiServer(ServerConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
            
        var httpConfig = new HttpConfiguration(_config.HttpPort);
        _httpServer = new HttpServer(httpConfig);
            
        ConfigureEndpoints();
    }

    private void ConfigureEndpoints()
    {
        _httpServer.RegisterEndpoint("/health", async (req, res) =>
        {
            var response = "{ \"status\": \"OK\" }";
            await WriteJsonResponse(res, response);
        });

        _httpServer.RegisterEndpoint("/status", async (req, res) =>
        {
            var status = new
            {
                ServerName = _config.ServerName,
                StartTime = DateTime.UtcNow,
                Environment = _config.Environment
            };

            var response = System.Text.Json.JsonSerializer.Serialize(status);
            await WriteJsonResponse(res, response);
        });
    }
    
    private async Task WriteJsonResponse(HttpListenerResponse response, string json)
    {
        response.ContentType = "application/json";
        response.StatusCode = (int)HttpStatusCode.OK;
            
        var buffer = Encoding.UTF8.GetBytes(json);
        response.ContentLength64 = buffer.Length;
            
        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        response.Close();
    }

    public Task StartAsync()
    {
        Logger.Info($"Starting API server on port {_config.HttpPort}");
        return _httpServer.StartAsync();
    }

    public Task StopAsync()
    {
        Logger.Info("Stopping API server");
        return _httpServer.StopAsync();
    }
}