using Fleck;
using Hermes.Classes;
using Hermes.Global;
using Hermes.Global.Definitions;
using Hermes.Interfaces;

namespace Hermes.Handlers;

public class ClientManager : IClientManager
{
    public void AddClient(SocketClientDefinition client, IWebSocketConnection socket)
    {
        if (socket == null)
            throw new ArgumentNullException(nameof(socket));

        var existing = HermesGlobal._clients
            .FirstOrDefault(x => x.Value.AccountId == client.AccountId);

        if (!existing.Equals(default(KeyValuePair<Guid, SocketClientDefinition>)))
        {
            HermesGlobal._clients.TryRemove(existing.Key, out _);
        }

        HermesGlobal._clients[socket.ConnectionInfo.Id] = client;
    }
    
    public bool RemoveClient(Guid connectionId)
    {
        return HermesGlobal._clients.TryRemove(connectionId, out _);
    }
    
    public bool TryGetClient(Guid connectionId, out SocketClientDefinition? client)
    {
        return HermesGlobal._clients.TryGetValue(connectionId, out client);
    }
}