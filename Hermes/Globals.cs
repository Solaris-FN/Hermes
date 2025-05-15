using System.Collections.Concurrent;
using Fleck;
using Hermes.Global.Definitions;
using Hermes.Handlers;

namespace Hermes;

public static class Globals
{
    public static readonly ConcurrentDictionary<string, Delegate> MessageHandlers =
        new ConcurrentDictionary<string, Delegate>
        {
            ["open"] = new Action<IWebSocketConnection, SocketClientDefinition, XmppMessage>(OpenHandler.Handle),
            ["auth"] = new Func<IWebSocketConnection, SocketClientDefinition, XmppMessage, Task>(AuthHandler.HandleAsync),
            ["iq"] = new Func<IWebSocketConnection, SocketClientDefinition, XmppMessage, Task>(IqHandler.HandleAsync),
            ["presence"] = new Func<IWebSocketConnection, SocketClientDefinition, XmppMessage, Task>(PresenceHandler.HandleAsync),
            ["message"] = new Func<IWebSocketConnection, SocketClientDefinition, XmppMessage, Task>(MessageHandler.HandleAsync),
            
            // Launcher
            ["init"] = new Action<IWebSocketConnection, SocketClientDefinition, XmppMessage>(InitHandler.Handle),
        };
}