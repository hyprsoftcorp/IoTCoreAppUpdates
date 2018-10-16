using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Hyprsoft.IoT.AppUpdates.Web;

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
                .UseStartup<Startup>()
                .UseAppUpdates();
    }
}
