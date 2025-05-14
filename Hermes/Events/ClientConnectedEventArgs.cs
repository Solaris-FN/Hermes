using Hermes.Classes;

namespace Hermes.Events;

public class ClientConnectedEventArgs : EventArgs
{
    public SocketClientDefinition Client { get; set; }
}