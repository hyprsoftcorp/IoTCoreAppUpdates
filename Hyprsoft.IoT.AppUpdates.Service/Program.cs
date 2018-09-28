using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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

            var builder = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging(options =>
                    {
                        options.AddConsole();
                        options.AddDebug();
                        options.AddFile(o =>
                        {
                            o.RootPath = hostContext.HostingEnvironment.ContentRootPath;
                            o.FallbackFileName = "appupdates.log";
                        });
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