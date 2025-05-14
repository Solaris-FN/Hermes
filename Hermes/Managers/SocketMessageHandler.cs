using System.Xml.Linq;
using Fleck;
using Hermes.Classes;
using Hermes.Events;
using Hermes.Interfaces;
using Hermes.Utilities;

namespace Hermes.Handlers;

public class SocketMessageHandler
{
    private readonly IClientManager _clientManager;
    public event EventHandler<XmppMessageEventArgs> XmppMessageReceived;
    
    public SocketMessageHandler(IClientManager clientManager)
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
            Logger.Error($"Failed to parse XML message: {ex.Message}");
            return false;
        }
    }

    public async Task HandleXmppMessageAsync(XmppMessage message, SocketClientDefinition client, IWebSocketConnection socket)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (client == null) throw new ArgumentNullException(nameof(client));
        if (socket == null) throw new ArgumentNullException(nameof(socket));
        
        if (!Globals.MessageHandlers.TryGetValue(message.Type, out var handler))
        {
            Logger.Warning($"No handler found for root element: {message.Type}");
            return;
        }
        
        Logger.Info($"Requested MessageType: {message.Type}");
        
        try
        {
            await (handler switch
            {
                Func<IWebSocketConnection, SocketClientDefinition, XmppMessage, Task> asyncHandler => 
                    asyncHandler(socket, client, message),
                
                Action<IWebSocketConnection, SocketClientDefinition, XmppMessage> syncHandler => 
                    Task.Run(() => syncHandler(socket, client, message)),
                
                _ => Task.FromException(new InvalidOperationException($"Unsupported handler type for '{message.Type}'"))
            });
            
            CheckAndUpdateLoginStatus(client);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error executing handler for root '{message.Type}': {ex.Message}");
        }
    }
    
    private void CheckAndUpdateLoginStatus(SocketClientDefinition client)
    {
        if (client.IsLoggedIn) return;
    
        var (isAuthenticated, hasAccountId, hasDisplayName, hasJid, hasResource) = (
            client.IsAuthenticated,
            !string.IsNullOrEmpty(client.AccountId),
            !string.IsNullOrEmpty(client.DisplayName),
            !string.IsNullOrEmpty(client.Jid),
            !string.IsNullOrEmpty(client.Resource)
        );
    
        if (isAuthenticated && hasAccountId && hasDisplayName && hasJid && hasResource)
        {
            client.IsLoggedIn = true;
            Logger.Info($"User '{client.DisplayName}' ({client.AccountId}) successfully logged in");
        
            EventManager.OnClientLogin(client);
        }
    }
}