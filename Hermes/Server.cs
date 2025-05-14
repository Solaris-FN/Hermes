using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Fleck;
using Hermes.Classes;
using Hermes.Events;
using Hermes.Handlers;
using Hermes.Interfaces;
using Hermes.Utilities;

namespace Hermes;

public class Server
{
    private WebSocketServer _server;
    private readonly Configuration _config;
    private readonly IClientManager _clientManager;
    private readonly SocketMessageHandler _messageHandler;
    
    public Server(Configuration config)
    {
        _config = config;
        Globals._clients = new ConcurrentDictionary<Guid, SocketClientDefinition>();
        
        _clientManager = new ClientManager();
        _messageHandler = new SocketMessageHandler(_clientManager);
    }

    public async Task StartAsync()
    {
        FleckLog.Level = LogLevel.Info;

        var server = new WebSocketServer($"ws://{_config.Host}:{_config.Port}");

        server.RestartAfterListenError = true;
        server.Start( socket =>
        {
            socket.OnOpen = () => HandleClientConnected(socket);
            socket.OnClose = () => HandleClientDisconnected(socket);
            socket.OnMessage = async (message) => await HandleMessageReceived(socket, message);
            socket.OnError = ex => HandleError(socket, ex);
        });

        await Task.Delay(100);

        _server = server;
    }

    public async Task StopAsync()
    {
        if (_server != null)
        {
            foreach (var client in Globals._clients.Values)
            {
                client.Socket.Close();
            }

            Globals._clients.Clear();

            await Task.Delay(100);

            _server.Dispose();
            _server = null;
        }
    }

    private void HandleClientConnected(IWebSocketConnection socket)
    {
        Logger.Info($"Client connected: {socket.ConnectionInfo.Id}");
        
        var starterClient = new SocketClientDefinition
        {
            Socket = socket
        };
        
        try
        {
            _clientManager.AddClient(starterClient, socket);
            EventManager.OnClientConnected(new ClientConnectedEventArgs { Client = starterClient });
        }
        catch (Exception ex)
        {
            Logger.Error($"Error handling client connection: {ex.Message}");
            EventManager.OnErrorOccurred(new Events.ErrorEventArgs
            {
                Client = starterClient,
                Error = ex,
                ErrorSource = "Client connection handler"
            });
        }
    }
    
        
    private void HandleClientDisconnected(IWebSocketConnection socket)
    {
        try
        {
            _clientManager.TryGetClient(socket.ConnectionInfo.Id, out SocketClientDefinition client);
            if (client != null)
            {
                _clientManager.RemoveClient(socket.ConnectionInfo.Id);
                EventManager.OnClientDisconnected(new ClientDisconnectedEventArgs { Client = client });
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error handling client disconnection: {ex.Message}");
            EventManager.OnErrorOccurred(new Events.ErrorEventArgs
            {
                Client = new SocketClientDefinition(),
                Error = ex,
                ErrorSource = "Client disconnection handler"
            });
        }
    }
    
    private async Task HandleMessageReceived(IWebSocketConnection socket, string message)
    {
        Logger.Info($"Received message: {message}");
        
        try
        {
            _clientManager.TryGetClient(socket.ConnectionInfo.Id, out SocketClientDefinition client);
                
            if (client == null) return;
            if (!_messageHandler.TryParseXmppMessage(message, out var xmppMessage)) return;
            
            await _messageHandler.HandleXmppMessageAsync(xmppMessage, client, socket);
            
            EventManager.OnMessageReceived(new MessageReceivedEventArgs
            {
                Client = client,
                Message = message
            });
        }
        catch (Exception ex)
        {
            Logger.Error($"Error handling message: {ex.Message}");
            EventManager.OnErrorOccurred(new Events.ErrorEventArgs
            {
                Client = new SocketClientDefinition(),
                Error = ex,
                ErrorSource = "Message handler"
            });
        }
    }
    
    private void HandleError(IWebSocketConnection socket, Exception ex)
    {
        try
        {
            _clientManager.TryGetClient(socket.ConnectionInfo.Id, out SocketClientDefinition client);
            if (client != null)
            {
                Logger.Error($"Error from client {socket.ConnectionInfo.Id}: {ex.Message}");
                EventManager.OnErrorOccurred(new Events.ErrorEventArgs
                {
                    Client = client,
                    Error = ex,
                    ErrorSource = "Client connection"
                });
                
                if (ex.Message.Contains("An existing connection was forcibly closed by the remote host"))
                { 
                    _clientManager.RemoveClient(socket.ConnectionInfo.Id);
                    EventManager.OnClientDisconnected(new ClientDisconnectedEventArgs { Client = client });
                }
            }
        }
        catch (Exception handlerEx)
        {
            if (!handlerEx.Message.Contains("An existing connection was forcibly closed by the remote host"))
            {
                Logger.Error($"Error in error handler: {handlerEx.Message}");
                EventManager.OnErrorOccurred(new Events.ErrorEventArgs
                {
                    Error = handlerEx,
                    ErrorSource = "Error handler"
                });
            }
        }
    }
}