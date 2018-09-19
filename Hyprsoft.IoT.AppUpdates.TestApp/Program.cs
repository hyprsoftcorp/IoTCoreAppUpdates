using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Hyprsoft.IoT.AppUpdates.TestApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using (var cts = new CancellationTokenSource())
            {
                Console.CancelKeyPress += (s, e) =>
                {
                    cts.Cancel();
                    e.Cancel = true;
                };
                Console.WriteLine($"{FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location)}");
                if (args.Length > 0)
                    Console.WriteLine($"Command line: {String.Join(", ", args).Trim()}");

                try
                {
                    await Task.Delay(-1, cts.Token);
                }
                catch (TaskCanceledException)
                {
                }
                Console.WriteLine("Exiting.");
            }   // using cancellation token source.
        }
    }
}
