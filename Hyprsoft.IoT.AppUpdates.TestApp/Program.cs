using System;
using System.Diagnostics;
using System.Reflection;

namespace Hyprsoft.IoT.AppUpdates.TestApp
{
    class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine($"{FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location)}");
            if (args.Length > 0)
                Console.WriteLine($"Command line: {String.Join(", ", args).Trim()}");
            Console.WriteLine();
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
