using System.Collections.Concurrent;
using Hermes.Classes;

namespace Hermes;

public class Globals
{
    public static ConcurrentDictionary<Guid, SocketClientDefinition> _clients;
}