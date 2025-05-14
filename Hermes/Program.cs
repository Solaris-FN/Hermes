using System;
using System.Threading;
using System.Threading.Tasks;
using Hermes.Api;
using Hermes.Utilities;

namespace Hermes
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = Configuration.Load();
            var server = new Server(config);

            var apiServer = new ApiServer(new ServerConfig
            {
                ServerName = config.ServerName,
                Environment = config.Environment,
                HttpPort = config.HttpPort
            });

            var cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cancellationTokenSource.Cancel();
            };

            try
            {
                var apiTask = apiServer.StartAsync();
                var socketTask = server.StartAsync();

                await Task.Delay(Timeout.Infinite, cancellationTokenSource.Token);
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                Logger.Error($"Unexpected error: {ex.Message}");
            }
            finally
            {
                await server.StopAsync();
                Logger.Info("Socket server stopped");

                await apiServer.StopAsync();
                Logger.Info("HTTP server stopped");
            }
        }
    }
}