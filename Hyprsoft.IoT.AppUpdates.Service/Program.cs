using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using NLog.Targets;

namespace Hyprsoft.IoT.AppUpdates.Service
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            var isService = !(Debugger.IsAttached || args.Contains("--console"));

            var target = new FileTarget("FileLoggingTarget")
            {
                FileName = $"app-updates-log.log",
                Layout = "${level:uppercase=true}\t${logger} @ ${longdate}\n\t${message}",
                ArchiveAboveSize = 1048576, // 1MB
                MaxArchiveFiles = 5
            };
            NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(target, NLog.LogLevel.Trace);

            var builder = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging(options =>
                    {
                        options.AddConsole();
                        options.AddDebug();
                        options.AddNLog();
                    });
                    services.AddHostedService<UpdateService>();
                });

            if (isService)
                await builder.RunAsServiceAsync();
            else
                await builder.RunConsoleAsync();
        }
    }
}