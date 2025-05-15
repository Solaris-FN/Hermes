using Fleck;
using Hermes.Global.Definitions;

namespace Hermes.Interfaces;

public interface IClientManager
{
    void AddClient(SocketClientDefinition client, IWebSocketConnection socket); 
    bool TryGetClient(Guid connectionId, out SocketClientDefinition client);
    bool RemoveClient(Guid connectionId);
}