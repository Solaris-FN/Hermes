using System;
using System.Threading;
using System.Threading.Tasks;
using Hermes.Utilities;

namespace Hermes
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = Configuration.Load();
            var server = new Server(config);
            
            try
            {
                await server.StartAsync();
                
                var cancellationTokenSource = new CancellationTokenSource();
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    cancellationTokenSource.Cancel();
                };
                
                try
                {
                    await Task.Delay(Timeout.Infinite, cancellationTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error: {ex.Message}");
            }
            finally
            {
                await server.StopAsync();
                Logger.Info("Server stopped");
            }
        }
    }
}