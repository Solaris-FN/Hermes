using System.Xml.Linq;
using Fleck;
using Hermes.Global.Definitions;

namespace Hermes.Handlers;

public class InitHandler
{
    public static void Handle(IWebSocketConnection socket, SocketClientDefinition client, XmppMessage root)
    {
        socket.Send(new XElement("initialized").ToString());
    }
}