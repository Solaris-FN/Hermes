using System.Xml.Linq;
using Fleck;
using Hermes.Classes;
using Hermes.Events;
using Hermes.Interfaces;

namespace Hermes.Handlers;

public class MessageHandler
{
    private readonly IClientManager _clientManager;
    public event EventHandler<XmppMessageEventArgs> XmppMessageReceived;
    
    public MessageHandler(IClientManager clientManager)
    {
        _clientManager = clientManager ?? throw new ArgumentNullException(nameof(clientManager));
    }

    public bool TryParseXmppMessage(string message, out XmppMessage xmppMessage)
    {
        xmppMessage = null;
            
        try
        {
            if (!(message.StartsWith("<") && message.EndsWith(">")))
                return false;

            var element = XElement.Parse(message);
                
            xmppMessage = new XmppMessage
            {
                Type = element.Name.LocalName,
                Namespace = element.Name.NamespaceName,
                To = element.Attribute("to")?.Value,
                From = element.Attribute("from")?.Value,
                Id = element.Attribute("id")?.Value,
                Version = element.Attribute("version")?.Value,
                Element = element,
                RawContent = message
            };
                
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse XML message: {ex.Message}");
            return false;
        }
    }

    public async Task HandleXmppMessageAsync(XmppMessage message, SocketClientDefinition client, IWebSocketConnection socket)
    {
        if (!Globals.MessageHandlers.TryGetValue(message.Type, out var handler))
        {
            Console.WriteLine($"No handler found for root element: {message.Type}");
            return;
        }
        
        Console.WriteLine($"Requested MessageType: {message.Type}");
        
        try
        {
            switch (handler)
            {
                case Func<IWebSocketConnection, SocketClientDefinition, XmppMessage, Task> asyncHandler:
                    await asyncHandler(socket, client, message);
                    break;
                case Action<IWebSocketConnection, SocketClientDefinition, XmppMessage> syncHandler:
                    syncHandler(socket, client, message);
                    break;
                default:
                    Console.WriteLine($"Handler for '{message.Type}' has an unsupported type.");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing handler for root '{message.Type}': {ex.Message}");
        }
        
        bool isValidConnection =
            !client.IsLoggedIn &&
            client.IsAuthenticated &&
            !string.IsNullOrEmpty(client.AccountId) &&
            !string.IsNullOrEmpty(client.DisplayName) &&
            !string.IsNullOrEmpty(client.Jid) &&
            !string.IsNullOrEmpty(client.Resource);

        if (isValidConnection)
        {
            client.IsLoggedIn = true;
    
            Console.WriteLine($"New xmpp login detected for '{client.DisplayName}'");
        }
    }
}