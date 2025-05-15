using System.Net;
using System.Text;
using System.Xml.Linq;
using Hermes.Api.Utilities;
using Hermes.Global;
using Hermes.Global.Definitions;
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
        _httpServer.RegisterEndpoint("/health", async (req, res, _) =>
        {
            var response = "{ \"status\": \"OK\" }";
            await WriteJsonResponse(res, response);
        });

        _httpServer.RegisterEndpoint("/status", async (req, res, _) =>
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
        
        _httpServer.RegisterEndpoint("/h/v1/xmpp/forward/{accountId}/{body}",
            async (req, res, routeParams) =>
            {
                var accountIdExists = routeParams.TryGetValue("accountId", out var accountId);
                var bodyExists = routeParams.TryGetValue("body", out var body);

                if (!accountIdExists || !bodyExists)
                {
                    await WriteErrorResponse(res, HttpStatusCode.BadRequest, "Missing route parameters.");
                    return;
                }
                
                if (HermesGlobal._clients == null)
                {
                    await WriteErrorResponse(res, HttpStatusCode.InternalServerError, "Clients list not initialized.");
                    return;
                }
                
                var clientPair = HermesGlobal._clients.FirstOrDefault(x => x.Value.AccountId == accountId);
                if (clientPair.Equals(default(KeyValuePair<string, SocketClientDefinition>)))
                {
                    await WriteErrorResponse(res, HttpStatusCode.NotFound, $"Client with ID '{accountId}' not found.");
                    return;
                }
                
                var client = clientPair.Value;
                if (client == null)
                {
                    await WriteErrorResponse(res, HttpStatusCode.NotFound,
                        $"Client with ID '{accountId}' not found.");
                    return;
                }
                        
                var stanza = new XElement(XNamespace.Get("jabber:client") + "message",
                    new XAttribute("from", $"xmpp-admin@{HermesGlobal.Domain}"),
                    new XAttribute("to", client.Jid),
                    new XAttribute("xmlns", "jabber:client"),
                    new XElement("body", body)
                );

                await client.Socket.Send(stanza.ToString(SaveOptions.DisableFormatting));
                
                await WriteJsonResponse(res, "{ \"status\": \"Forwarded\" }");
            });

        _httpServer.RegisterEndpoint("/h/v1/xmpp/presence/forward/{senderId}/{receiverId}/{isOffline}",
            async (req, res, routeParams) =>
            {
                var senderIdExists = routeParams.TryGetValue("senderId", out var senderId);
                var receiverIdExists = routeParams.TryGetValue("receiverId", out var receiverId);
                var isOfflineExists = routeParams.TryGetValue("isOffline", out var isOfflineStr);

                if (!senderIdExists || !receiverIdExists || !isOfflineExists)
                {
                    await WriteErrorResponse(res, HttpStatusCode.BadRequest, "Missing route parameters.");
                    return;
                }

                bool isOffline = isOfflineStr.Equals("true", StringComparison.OrdinalIgnoreCase);

                if (HermesGlobal._clients == null)
                {
                    await WriteErrorResponse(res, HttpStatusCode.InternalServerError, "Clients list not initialized.");
                    return;
                }

                var senderPair = HermesGlobal._clients.FirstOrDefault(x => x.Value.AccountId == senderId);
                var receiverPair = HermesGlobal._clients.FirstOrDefault(x => x.Value.AccountId == receiverId);

                if (senderPair.Equals(default(KeyValuePair<string, SocketClientDefinition>)))
                {
                    await WriteErrorResponse(res, HttpStatusCode.NotFound, $"Sender with ID '{senderId}' not found.");
                    return;
                }

                if (receiverPair.Equals(default(KeyValuePair<string, SocketClientDefinition>)))
                {
                    await WriteErrorResponse(res, HttpStatusCode.NotFound,
                        $"Receiver with ID '{receiverId}' not found.");
                    return;
                }

                var sender = senderPair.Value;
                var receiver = receiverPair.Value;

                if (sender == null || receiver == null)
                {
                    await WriteErrorResponse(res, HttpStatusCode.NotFound,
                        $"Receiver or Sender not found.");
                    return;
                }

                if (sender.LastPresenceUpdate == null)
                {
                    await WriteErrorResponse(res, HttpStatusCode.InternalServerError, "Sender presence data missing.");
                    return;
                }

                if (receiver.Socket == null)
                {
                    await WriteErrorResponse(res, HttpStatusCode.InternalServerError, "Receiver socket missing.");
                    return;
                }

                var presenceStanza = new XElement(XNamespace.Get("jabber:client") + "presence",
                    new XAttribute("from", sender.Jid),
                    new XAttribute("xmlns", "jabber:client"),
                    new XAttribute("to", receiver.Jid),
                    new XAttribute("type", isOffline ? "unavailable" : "available")
                );

                if (sender.LastPresenceUpdate.Away)
                    presenceStanza.Add(new XElement("show", "away"));

                presenceStanza.Add(new XElement("status", sender.LastPresenceUpdate.Status));

                receiver.Socket.Send(presenceStanza.ToString(SaveOptions.DisableFormatting));

                await WriteJsonResponse(res, "{ \"status\": \"Forwarded\" }");
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
    
    private async Task WriteErrorResponse(HttpListenerResponse response, HttpStatusCode statusCode, string message)
    {
        response.ContentType = "application/json";
        response.StatusCode = (int)statusCode;

        var errorJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            error = message,
            status = (int)statusCode
        });

        var buffer = Encoding.UTF8.GetBytes(errorJson);
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