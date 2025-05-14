using System.Collections.Concurrent;
using Hermes.Classes;

namespace Hermes;

public static class Globals
{
    public static ConcurrentDictionary<Guid, SocketClientDefinition> _clients;

    public static readonly ConcurrentDictionary<string, Delegate> MessageHandlers =
        new ConcurrentDictionary<string, Delegate>
        {
            
        };
}