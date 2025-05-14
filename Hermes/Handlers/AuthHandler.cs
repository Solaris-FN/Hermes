using System.Text;
using System.Xml.Linq;
using System.Net.Http;
using System.Text.Json;
using Fleck;
using Hermes.Classes;

namespace Hermes.Handlers;

public class AuthHandler
{
    public static async Task HandleAsync(IWebSocketConnection socket, SocketClientDefinition client, XmppMessage root)
    {
        if (root is null || root.Element is null)
        {
            socket.Close();
            return;
        }
        
        byte[] decodedBytes = Convert.FromBase64String(root.Element.Value);
        string decodedContent = Encoding.UTF8.GetString(decodedBytes);
        
        string[] authFields = decodedContent.Split('\0');
        if (authFields.Length < 2)
        {
            socket.Close();
            return;
        }

        string accountId = authFields[1];
        string token = authFields.Length > 2 ? authFields[2] : string.Empty;
        
        var queryParams = new Dictionary<string, string>
        {
            { "accountId", accountId },
            { "token", token }
        };
        
        string endpoint = "/h/v1/auth/verify" + ApiHandler.BuildQueryString(queryParams);
        
        var authResponse = await ApiHandler.GetAsync<AuthResponse>(endpoint);
        
        if (authResponse != null)
        {
            client.AccountId = authResponse.AccountId;
            client.Token = token;
            client.DisplayName = authResponse.Username;
            client.IsAuthenticated = true;
            
            await socket.Send(new XElement(
                XNamespace.Get("urn:ietf:params:xml:ns:xmpp-sasl") + "success"
            ).ToString());
        }
        else
        {
            await socket.Send(CreateFailureResponse("not-authorized", "Invalid credentials"));
        }
    }

    private static string CreateFailureResponse(string condition, string message)
    {
        XNamespace ns = XNamespace.Get("urn:ietf:params:xml:ns:xmpp-sasl");

        XDocument doc = new XDocument(
            new XElement(ns + "failure",
                new XElement(ns + condition),
                new XElement(ns + "text",
                    new XAttribute(XNamespace.Xml + "lang", "eng"),
                    message
                )
            )
        );
        return doc.ToString();
    }
}
