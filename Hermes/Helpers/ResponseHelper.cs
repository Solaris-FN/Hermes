using System.Xml.Linq;
using Fleck;

namespace Hermes.Helpers;

public class ResponseHelper
{
    public static void SendStreamError(IWebSocketConnection socket, string errorType)
    {
        var streamErrorXml = new XElement(XName.Get("error", "http://etherx.jabber.org/streams"),
            new XElement(XName.Get(errorType, "urn:ietf:params:xml:ns:xmpp-streams")),
            new XAttribute(XNamespace.Xmlns + "stream", "http://etherx.jabber.org/streams")
        );
    
        socket.Send(streamErrorXml.ToString(SaveOptions.DisableFormatting));
    }
}