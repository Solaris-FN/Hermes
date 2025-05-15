using Hermes.Global.Definitions;

namespace Hermes.Events;

public class ClientConnectedEventArgs : EventArgs
{
    public SocketClientDefinition Client { get; set; }
}