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
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<NotificationManager>();
                    services.AddHostedService<MpicWorker>();
                })
                .Build();

            await host.RunAsync();
        }
    }
}