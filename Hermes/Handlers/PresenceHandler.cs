using System.Xml.Linq;
using Fleck;
using Hermes.Classes;
using Hermes.Utilities;
using Newtonsoft.Json;

namespace Hermes.Handlers;

public class PresenceHandler
{
    public static async Task HandleAsync(IWebSocketConnection socket, SocketClientDefinition client, XmppMessage root)
    {
        var element = root.Element;
        if (element == null) return;

        string? type = element.Attribute("type")?.Value;
        if (type == "unavailable") return;

        var statusElement = element.Elements().FirstOrDefault(e => e.Name.LocalName == "status");
        if (statusElement == null) return;

        var showElement = element.Elements().FirstOrDefault(x => x.Name.LocalName == "show");
        bool isAway = showElement != null;
        string statusValue = statusElement.Value;
        
        var lastPresence = new LastPresenceUpdate
        {
            Status = statusValue,
            Away = isAway
        };
        client.LastPresenceUpdate = lastPresence;
        
        var clientNamespace = XNamespace.Get("jabber:client");
        var presenceAttrs = new[]
        {
            new XAttribute("to", client.Jid),
            new XAttribute("from", client.Jid),
            new XAttribute("xmlns", "jabber:client"),
            new XAttribute("type", "available")
        };

        var presenceResponse = new XElement(clientNamespace + "presence", presenceAttrs);
        presenceResponse.Add(new XElement(clientNamespace + "status", statusValue));
        if (isAway) presenceResponse.Add(new XElement(clientNamespace + "show", "away"));

        await socket.Send(presenceResponse.ToString());

        Console.WriteLine(JsonConvert.SerializeObject(client, Formatting.Indented));
        
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
            
            var friendMessage = new XElement(clientNamespace + "presence",
                new XAttribute("to", friendClient.Jid),
                new XAttribute("from", client.Jid),
                new XAttribute("xmlns", "jabber:client"),
                new XAttribute("type", "available"),
                new XElement(clientNamespace + "status", statusValue));

            if (isAway) friendMessage.Add(new XElement(clientNamespace + "show", "away"));
            await friendClient.Socket.Send(friendMessage.ToString());
        }
    }
}