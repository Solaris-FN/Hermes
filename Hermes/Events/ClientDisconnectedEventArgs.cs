using Hermes.Classes;

namespace Hermes.Events;

public class ClientDisconnectedEventArgs : EventArgs
{
    public SocketClientDefinition Client { get; set; }
    public string Reason { get; set; }
}