using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Hyprsoft.IoT.AppUpdates.Web;
using Hyprsoft.Logging.Core;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Hyprsoft.IoT.AppUpdates.TestWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
            .ConfigureLogging(builder =>
            {
                builder.AddSimpleFileLogger(options =>
                {
                    options.Filename = "app-updates-log.log";
                    options.MaxFileSizeBytes = 524288;
                });
                builder.AddFilter<SimpleFileLoggerProvider>("Microsoft", LogLevel.None);
            })
            .UseStartup<Startup>()
            .UseAppUpdates();
    }
}
