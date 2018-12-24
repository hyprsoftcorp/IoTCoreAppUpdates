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

            var logger = new SimpleLogManager();
#if DEBUG
            logger.AddLogger(new SimpleConsoleLogger());
#endif
            logger.AddLogger(new SimpleFileLogger(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "app-updates-log.log"))
            {
                MaxFileSizeBytes = 524288
            });
            var builder = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging(options =>
                    {
                        options.AddConsole();
                        options.AddDebug();
                    });
                    services.AddSingleton(logger);
                    services.AddHostedService<UpdateService>();
                });

            if (isService)
                await builder.RunAsServiceAsync();
            else
                await builder.RunConsoleAsync();
        }
    }
}