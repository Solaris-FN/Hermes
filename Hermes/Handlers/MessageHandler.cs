using System.Xml.Linq;
using Fleck;
using Hermes.Global;
using Hermes.Global.Definitions;
using Hermes.Helpers;
using Hermes.Utilities;

namespace Hermes.Handlers;

public class MessageHandler
{
    public static async Task HandleAsync(IWebSocketConnection socket, SocketClientDefinition client, XmppMessage root)
    {
        if (root?.Element == null)
        {
            Logger.Error("Received null message or element");
            return;
        }

        var bodyElement = root.Element.Elements().FirstOrDefault(x => x.Name.LocalName == "body");
        if (bodyElement == null || string.IsNullOrEmpty(bodyElement.Value))
        {
            Logger.Debug("Message received with empty or missing body");
            return;
        }

        string body = bodyElement.Value;
        string type = root.Element.Attribute("type")?.Value ?? "";
        
        var toAttribute = root.Element.Attribute("to");
        
        if (toAttribute == null)
        {
            Logger.Error("Message missing 'to' attribute");
            return;
        }
        
        string to = toAttribute.Value;
        string id = root.Element.Attribute("id")?.Value ?? Guid.NewGuid().ToString();

        try
        {
            if (type == "chat")
            {
                string bareJid = to.Split('/')[0];
                var targetPair = HermesGlobal._clients.FirstOrDefault(x => x.Value.Jid.Split('/')[0] == bareJid);
                
                if (targetPair.Equals(default(KeyValuePair<string, SocketClientDefinition>)))
                    return;

                var targetClient = targetPair.Value;
                
                var message = new XElement(XNamespace.Get("jabber:client") + "message",
                    new XAttribute("to", targetClient.Jid!),
                    new XAttribute("from", client.Jid!),
                    new XAttribute("type", "chat"),
                    new XElement("body", body))
                    .ToString()
                    .Replace(" xmlns=\"\"", "");
                
                await client.Socket.Send(message);
            }
            else if (!string.IsNullOrEmpty(to))
            {
                await SendMessageToClient.SendAsync(socket, body, client, to, id);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error handling message: {ex.Message}");
        }
    }
}