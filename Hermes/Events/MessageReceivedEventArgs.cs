using Hermes.Classes;
using Hermes.Global.Definitions;

namespace Hermes.Events;

public class MessageReceivedEventArgs : EventArgs
{
    public SocketClientDefinition Client { get; set; }
    public string Message { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}