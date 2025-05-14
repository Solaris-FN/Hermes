using Hermes.Classes;
using Hermes.Global.Definitions;

namespace Hermes.Events;

public class ErrorEventArgs : EventArgs
{
    public SocketClientDefinition Client { get; set; }
    public Exception Error { get; set; }
    public string ErrorSource { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}