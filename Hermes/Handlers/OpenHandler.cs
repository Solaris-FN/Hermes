using System.Xml.Linq;
using Fleck;
using Hermes.Classes;

namespace Hermes.Handlers;

public class OpenHandler
{
    private static readonly XNamespace XmppFraming = "urn:ietf:params:xml:ns:xmpp-framing";
    private static readonly XNamespace Streams = "http://etherx.jabber.org/streams";
    private static readonly XNamespace Sasl = "urn:ietf:params:xml:ns:xmpp-sasl";
    private static readonly XNamespace RosterVer = "urn:xmpp:features:rosterver";
    private static readonly XNamespace Tls = "urn:ietf:params:xml:ns:xmpp-tls";
    private static readonly XNamespace Bind = "urn:ietf:params:xml:ns:xmpp-bind";
    private static readonly XNamespace Compress = "http://jabber.org/features/compress";
    private static readonly XNamespace Session = "urn:ietf:params:xml:ns:xmpp-session";
    private static readonly XNamespace IqAuth = "http://jabber.org/features/iq-auth";
    private static readonly XNamespace Xml = "xml";

    public static void Handle(IWebSocketConnection socket, SocketClientDefinition client, XmppMessage root)
    {
        var openMessage = new XElement(
            XmppFraming + "open",
            new XAttribute("from", Globals.Domain),
            new XAttribute("id", socket.ConnectionInfo.Id),
            new XAttribute("version", "1.0"),
            new XAttribute(Xml + "lang", "en")
        );
        socket.Send(openMessage.ToString());

        var features = new XElement(Streams + "features",
            new XElement(RosterVer + "ver"),
            new XElement(Tls + "starttls"),
            new XElement(Compress + "compression",
                new XElement("method", "zlib")
            )
        );

        if (client.IsAuthenticated)
        {
            features.Add(
                new XElement(Bind + "bind"),
                new XElement(Session + "session")
            );
        }
        else
        {
            features.Add(
                new XElement(Sasl + "mechanisms",
                    new XElement("method", "PLAIN")
                ),
                new XElement(IqAuth + "auth")
            );
        }

        socket.Send(features.ToString());
    }
}