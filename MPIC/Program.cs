using MegaplanSync.ApiClient;
using MegaplanSync.Core;
using MegaplanSync.Core.Models.Deal;
using MegaplanSync.Core.Interfaces;
using MegaplanSync.Logging;
using MegaplanSync.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MPIC
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            // Поддержка Windows Service
            builder.Services.AddWindowsService();

            // Регистрируем фоновую службу
            builder.Services.AddHostedService<MpicWorker>();

            var host = builder.Build();
            await host.RunAsync();
        }
    }
}