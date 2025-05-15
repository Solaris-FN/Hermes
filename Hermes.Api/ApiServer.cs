using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Hermes.Api.Utilities;
using Hermes.Global;
using Hermes.Global.Definitions;
using Hermes.Global.Requests;
using Hermes.HTTP;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonSerializer = System.Text.Json.JsonSerializer;

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

        _httpServer.RegisterEndpoint("/h/v1/xmpp/forward/{accountId}",
            async (req, res, routeParams) =>
            {
                var accountIdExists = routeParams.TryGetValue("accountId", out var accountId);

                if (!accountIdExists)
                {
                    await WriteErrorResponse(res, HttpStatusCode.BadRequest, "Missing route parameter 'accountId'.");
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

                string requestBody;
                using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    await WriteErrorResponse(res, HttpStatusCode.BadRequest, "Request body is empty.");
                    return;
                }

                var stanza = new XElement(XNamespace.Get("jabber:client") + "message",
                    new XAttribute("from", $"xmpp-admin@{HermesGlobal.Domain}"),
                    new XAttribute("to", client.Jid),
                    new XAttribute("xmlns", "jabber:client"),
                    new XElement("body", requestBody)
                );

                if (client.Socket.IsAvailable)
                {
                    await client.Socket.Send(stanza.ToString(SaveOptions.DisableFormatting));
                }
                else
                {
                    await WriteErrorResponse(res, HttpStatusCode.InternalServerError,
                        "Client socket is not connected.");
                    return;
                }

                await WriteJsonResponse(res, "{ \"status\": \"Forwarded\" }");
            });

        _httpServer.RegisterEndpoint("/h/v1/xmpp/friends/status/{accountId}",
            async (req, res, routeParams) =>
            {
                var accountIdExists = routeParams.TryGetValue("accountId", out var accountId);
                if (!accountIdExists)
                {
                    await WriteErrorResponse(res, HttpStatusCode.BadRequest, "Missing route parameter.");
                    return;
                }

                var endpoint = $"/h/v1/friends?accountId={Uri.EscapeDataString(accountId)}";
                var friends = await ApiHandler.GetAsync<List<FriendResponse>>(endpoint);

                if (friends == null || !friends.Any())
                {
                    await WriteErrorResponse(res, HttpStatusCode.NotFound,
                        $"No friends found for user '{accountId}'.");
                    return;
                }

                var result = new Dictionary<string, LastPresenceUpdate>();

                foreach (var friend in friends.Where(f => f.Status == "ACCEPTED"))
                {
                    var friendClientPair = HermesGlobal._clients.FirstOrDefault(x => x.Value.AccountId == friend.Id);
                    if (friendClientPair.Equals(default(KeyValuePair<Guid, SocketClientDefinition>)))
                    {
                        Logger.Error($"Friend with AccountId '{friend.Id}' not found.");
                        continue;
                    }

                    var friendClient = friendClientPair.Value;
                    if (friendClient == null)
                        continue;

                    result[friend.Id] = friendClient.LastPresenceUpdate;
                }

                var json = JsonSerializer.Serialize(result);
                await WriteJsonResponse(res, json);
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

        _httpServer.RegisterEndpoint("/party/api/v1/Fortnite/parties", async (req, res, _) =>
        {
            if (req.HttpMethod != "POST")
            {
                await WriteErrorResponse(res, HttpStatusCode.MethodNotAllowed, "Method not allowed");
                return;
            }

            string requestBody;
            using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
            {
                requestBody = await reader.ReadToEndAsync();
            }

            var createParty = JsonConvert.DeserializeObject<PartyCreate>(requestBody);
            if (createParty == null)
            {
                await WriteErrorResponse(res, HttpStatusCode.BadRequest, "Invalid request body");
                return;
            }

            string accountId = createParty.join_info.connection.id.Split('@')[0];

            if (HermesGlobal._clients == null)
            {
                await WriteErrorResponse(res, HttpStatusCode.InternalServerError, "Clients list not initialized");
                return;
            }

            var client = HermesGlobal._clients.Values.FirstOrDefault(x => x.AccountId == accountId);
            if (client == null)
            {
                await WriteErrorResponse(res, HttpStatusCode.NotFound, $"Client with ID '{accountId}' not found");
                return;
            }

            string partyId = Guid.NewGuid().ToString().Replace("-", "");
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            var newParty = new PartyDefinition
            {
                id = partyId,
                created_at = timestamp,
                updated_at = timestamp,
                config = createParty.config,
                members = new List<Members>
                {
                    new Members
                    {
                        account_id = accountId,
                        meta = createParty.join_info.meta,
                        connections = new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object>
                            {
                                { "id", createParty.join_info.connection.id },
                                { "connected_at", timestamp },
                                { "updated_at", timestamp },
                                { "yield_leadership", createParty.join_info.connection.yield_leadership },
                                { "meta", createParty.join_info.connection.meta ?? new() }
                            }
                        },
                        revision = 0,
                        updated_at = timestamp,
                        joined_at = timestamp,
                        role = "CAPTAIN"
                    }
                },
                applicants = new List<object>(),
                meta = createParty.meta,
                invites = new List<object>(),
                revision = 0,
                intentions = new List<object>(),
            };

            HermesGlobal.parties.Add(newParty);

            string json = JsonSerializer.Serialize(newParty, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
            await WriteJsonResponse(res, json);
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