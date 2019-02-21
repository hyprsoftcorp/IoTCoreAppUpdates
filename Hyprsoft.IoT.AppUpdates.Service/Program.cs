using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Hyprsoft.Logging.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hyprsoft.IoT.AppUpdates.Service
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            var isService = !(Debugger.IsAttached || args.Contains("--console"));

            var hostBuilder = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging(builder =>
                    {
                        if (!isService)
                        {
                            builder.AddConsole();
                            builder.AddDebug();
                        }   // is service?
                        builder.AddSimpleFileLogger(options =>
                        {
                            options.Filename = "app-updates-log.log";
                            options.MaxFileSizeBytes = 524288;
                        });
                    });
                    services.AddHostedService<UpdateService>();
                });

            if (isService)
                await hostBuilder.RunAsServiceAsync();
            else
                await hostBuilder.RunConsoleAsync();
        }
    }
}