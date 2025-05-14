using System.Collections.Concurrent;
using Fleck;
using Hermes.Classes;
using Hermes.Handlers;

namespace Hermes;

public static class Globals
{
    public static ConcurrentDictionary<Guid, SocketClientDefinition> _clients;
    public static string Domain = "hermes.solarisfn.org";
    public static string BaseApiUrl = "https://api-v1-horizon-external-api.solarisfn.org"; // change this obviously
    public static string Token = "token123verysecurehahahakys";

    public static readonly ConcurrentDictionary<string, Delegate> MessageHandlers =
        new ConcurrentDictionary<string, Delegate>
        {
            ["open"] = new Action<IWebSocketConnection, SocketClientDefinition, XmppMessage>(OpenHandler.Handle),
            ["auth"] = new Func<IWebSocketConnection, SocketClientDefinition, XmppMessage, Task>(AuthHandler.HandleAsync),
            ["iq"] = new Func<IWebSocketConnection, SocketClientDefinition, XmppMessage, Task>(IqHandler.HandleAsync),
            ["presence"] = new Func<IWebSocketConnection, SocketClientDefinition, XmppMessage, Task>(PresenceHandler.HandleAsync),
            ["message"] = new Func<IWebSocketConnection, SocketClientDefinition, XmppMessage, Task>(MessageHandler.HandleAsync),
        };
}