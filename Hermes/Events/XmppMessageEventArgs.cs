using Hermes.Classes;
using Hermes.Global.Definitions;

namespace Hermes.Events;

public class XmppMessageEventArgs : EventArgs
{
    public Guid SocketId { get; set; }
    public XmppMessage Message { get; set; }
}