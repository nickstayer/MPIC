using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MPIC
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .UseWindowsService(options =>
                {
                    options.ServiceName = "MPIC - Email to CRM Integration Monitor";
                })
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.Sources.Clear();
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
                    config.AddUserSecrets<Program>();
                    config.AddEnvironmentVariables("MPIC_");
                })
                .ConfigureServices((context, services) =>
                {
                    services.Configure<RootSettings>(context.Configuration.GetSection("MpicSettings"));
                    services.AddSingleton<NotificationManager>();
                    services.AddHostedService<MpicWorker>();
                })
                .Build();

            await host.RunAsync();
        }
    }
}