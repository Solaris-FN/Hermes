using Hermes.Events;
using Hermes.Global.Definitions;

namespace Hermes.Handlers;

public static class EventManager
{
    public static event EventHandler<ClientConnectedEventArgs> ClientConnected;
    public static event EventHandler<ClientDisconnectedEventArgs> ClientDisconnected;
    public static event EventHandler<MessageReceivedEventArgs> MessageReceived;
    public static event EventHandler<Events.ErrorEventArgs> ErrorOccurred;
    public static event Action<SocketClientDefinition> ClientLogin;

    public static void OnClientConnected(ClientConnectedEventArgs e)
    {
        ClientConnected?.Invoke(null, e);
    }

    public static void OnClientDisconnected(ClientDisconnectedEventArgs e)
    {
        ClientDisconnected?.Invoke(null, e);
    }

    public static void OnMessageReceived(MessageReceivedEventArgs e)
    {
        MessageReceived?.Invoke(null, e);
    }

    public static void OnErrorOccurred(Events.ErrorEventArgs e)
    {
        ErrorOccurred?.Invoke(null, e);
    }

    public static void OnClientLogin(SocketClientDefinition e)
    {
        ClientLogin?.Invoke(e);
    }
}