using Hermes.Global.Definitions;

namespace Hermes.Events;

public class ClientDisconnectedEventArgs : EventArgs
{
    public SocketClientDefinition Client { get; set; }
    public string Reason { get; set; }
}