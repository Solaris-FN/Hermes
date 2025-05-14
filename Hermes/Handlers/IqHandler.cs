using System.Text.Json;
using System.Xml.Linq;
using Fleck;
using Hermes.Classes;
using Hermes.Utilities;
using Newtonsoft.Json;

namespace Hermes.Handlers;

public class IqHandler
{
    public static async Task HandleAsync(IWebSocketConnection socket, SocketClientDefinition client, XmppMessage root)
    {
        if (client is null)
            return;

        var attributeId = root.Element.Attribute("id")?.Value;


        switch (attributeId)
        {
            case "_xmpp_auth1":
                var query = root.Element.Elements().FirstOrDefault(x => x.Name.LocalName == "query");

                if (query == null)
                {
                    ResponseHelper.SendStreamError(socket, "not-well-formed");
                    return;
                }
                
                Console.WriteLine(JsonConvert.SerializeObject(client, Formatting.Indented));
                
                if (string.IsNullOrEmpty(client.Resource) 
                    && string.IsNullOrEmpty(client.Jid)
                    && string.IsNullOrEmpty(client.Token) 
                    && string.IsNullOrEmpty(client.AccountId) 
                    && !client.IsAuthenticated)
                {
                    var resourceValue = query.Elements()
                        .FirstOrDefault(x => x.Name.LocalName == "resource")?.Value;
                    var usernameValue = query.Elements()
                        .FirstOrDefault(x => x.Name.LocalName == "username")?.Value;
                    var passwordValue = query.Elements()
                        .FirstOrDefault(x => x.Name.LocalName == "password")?.Value;
    
                    if (string.IsNullOrEmpty(resourceValue) || string.IsNullOrEmpty(usernameValue) || string.IsNullOrEmpty(passwordValue))
                        return;
                    
                     var queryParams = new Dictionary<string, string>
                     {
                         { "accountId", usernameValue },
                         { "token", passwordValue },
                     };
                    
                     string verifyEndpoint = "/h/v1/auth/verify" + ApiHandler.BuildQueryString(queryParams);
                    
                     var authResponse = await ApiHandler.GetAsync<AuthResponse>(verifyEndpoint);
                     if (authResponse == null)
                     {
                         ResponseHelper.SendStreamError(socket, "invalid-credentials");
                         return;
                     }

                    client.Resource = resourceValue;
                    client.DisplayName = authResponse.AccountId;
                    
                    client.Jid = $"{client.AccountId}@{Globals.Domain}/{client.Resource}";
                    client.Token = passwordValue;
                    client.IsAuthenticated = true;
                    
                    socket.Send(new XElement(XNamespace.Get("jabber:client") + "iq",
                        new XAttribute("type", "result"),
                        new XAttribute("xmlns", "jabber:client"),
                        new XAttribute("id", "_xmpp_auth1"),
                        "Authentication successful."
                    ).ToString());
                }

                if (string.IsNullOrEmpty(client.Resource) || 
                    string.IsNullOrEmpty(client.AccountId) || 
                    string.IsNullOrEmpty(client.Token) ||
                    string.IsNullOrEmpty(client.Jid))
                {
                    ResponseHelper.SendStreamError(socket, "not-well-formed");
                    return;
                }
                break;

            case "_xmpp_bind1":
                var resource = root.Element.Elements().Where(x => x.Name.LocalName == "bind").First().Elements().First()
                    .Value.ToString();
                if (client.Resource is null && client.AccountId != null)
                {
                    client.Resource = resource;
                    client.Jid = $"{client.AccountId}@{Globals.Domain}/{client.Resource}";

                    socket.Send(
                        new XElement(XNamespace.Get("jabber:client") + "iq",
                            new XAttribute("to", client.Jid),
                            new XAttribute("id", "_xmpp_bind1"),
                            new XAttribute("type", "result"),
                            new XElement(XNamespace.Get("urn:ietf:params:xml:ns:xmpp-bind") + "bind",
                                new XElement(XNamespace.Get("urn:ietf:params:xml:ns:xmpp-bind") + "jid", client.Jid)
                            )
                        ).ToString()
                    );
                }
                else
                {
                    socket.Send("<close xmlns='urn:ietf:params:xml:ns:xmpp-framing'/>");
                    socket.Close();
                }

                break;

            case "_xmpp_session1":
                XNamespace clientNamespace = XNamespace.Get("jabber:client");

                await socket.Send(new XElement(clientNamespace + "iq",
                    new XAttribute("to", client.Jid),
                    new XAttribute("from", Globals.Domain),
                    new XAttribute("id", attributeId),
                    new XAttribute("xmlns", "jabber:client"),
                    new XAttribute("type", "result")
                ).ToString());

                var endpoint = $"/h/v1/friends?accountId={Uri.EscapeDataString(client.AccountId)}";
                var friends = await ApiHandler.GetAsync<List<FriendResponse>>(endpoint);

                if (friends == null || !friends.Any())
                {
                    Logger.Error($"No friends found for user '{client.AccountId}'.");
                    return;
                }

                foreach (var friend in friends.Where(f => f.Status == "ACCEPTED"))
                {
                    var friendClientPair = Globals._clients.FirstOrDefault(x => x.Value.AccountId == friend.Id);
                    if (friendClientPair.Equals(default(KeyValuePair<Guid, SocketClientDefinition>)))
                    {
                        Logger.Error($"Friend with AccountId '{friend.Id}' not found.");
                        continue;
                    }

                    var friendClient = friendClientPair.Value;
                    var lastPresence = friendClient.LastPresenceUpdate ?? new LastPresenceUpdate();

                    try
                    {
                        var presenceXml = new XElement(clientNamespace + "presence",
                            new XAttribute("to", client.Jid),
                            new XAttribute("from", friendClient.Jid),
                            new XAttribute("xmlns", "jabber:client"),
                            new XAttribute("type", "available"));

                        if (lastPresence.Away)
                        {
                            presenceXml.Add(new XElement(clientNamespace + "show", "away"));
                        }

                        presenceXml.Add(new XElement(clientNamespace + "status", lastPresence.Status));

                        await socket.Send(presenceXml.ToString());
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error sending presence for {friend.Id}: {ex.Message}");
                    }
                }

                break;

            default:
                Logger.Warning($"Missing attributeId: {attributeId}");
                socket.Send(
                    new XElement(XNamespace.Get("jabber:client") + "iq",
                        new XAttribute("to", client.Jid),
                        new XAttribute("from", Globals.Domain),
                        new XAttribute("id", attributeId),
                        new XAttribute("type", "result")
                    ).ToString()
                );
                break;
        }
    }
}