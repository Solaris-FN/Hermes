using Fleck;
using Hermes.Classes;
using Hermes.Interfaces;

namespace Hermes.Handlers;

public class ClientManager : IClientManager
{
    public void AddClient(SocketClientDefinition client, IWebSocketConnection socket)
    {
        if (socket == null)
            throw new ArgumentNullException(nameof(socket));

        var existing = Globals._clients
            .FirstOrDefault(x => x.Value.AccountId == client.AccountId);

        if (!existing.Equals(default(KeyValuePair<Guid, SocketClientDefinition>)))
        {
            Globals._clients.TryRemove(existing.Key, out _);
        }

        Globals._clients[socket.ConnectionInfo.Id] = client;
    }
    
    public bool RemoveClient(Guid connectionId)
    {
        return Globals._clients.TryRemove(connectionId, out _);
    }
    
    public bool TryGetClient(Guid connectionId, out SocketClientDefinition? client)
    {
        return Globals._clients.TryGetValue(connectionId, out client);
    }
}