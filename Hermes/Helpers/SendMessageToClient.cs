using System.Xml.Linq;
using Fleck;
using Hermes.Classes;
using Hermes.Global;
using Hermes.Global.Definitions;

namespace Hermes.Helpers;

public class SendMessageToClient
{
    public static async Task<bool> SendAsync(IWebSocketConnection socket, string body, SocketClientDefinition client, string to, string id)
    {
        try
        {
            string bareJid = to.Split('/')[0];
            var targetPair = HermesGlobal._clients.FirstOrDefault(x => x.Value.Jid.Split('/')[0] == bareJid);
        
            if (targetPair.Equals(default(KeyValuePair<string, SocketClientDefinition>)))
                return false;
            
            var targetClient = targetPair.Value;

            var messageXml = new XElement("message",
                new XAttribute("from", client.Jid),
                new XAttribute("xmlns", "jabber:client"),
                new XAttribute("to", targetClient.Jid),
                new XAttribute("id", id),
                new XAttribute("type", "chat"),
                new XElement("body", body)
            );
        
            await targetClient.Socket.Send(messageXml.ToString(SaveOptions.DisableFormatting));
            return true;
        }
        catch
        {
            return false;
        }
    }
}