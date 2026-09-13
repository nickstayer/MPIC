using MegaplanSync.ApiClient;
using MegaplanSync.Core;
using MegaplanSync.Core.Models.Deal;
using MegaplanSync.Core.Interfaces;
using MegaplanSync.Logging;
using MegaplanSync.Service;
using Microsoft.Extensions.Configuration;
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

            // Единый источник JSON: только один appsettings.json, без вариаций по окружению.
            // CreateApplicationBuilder по умолчанию добавляет appsettings.json и
            // appsettings.{Environment}.json — переопределяем список источников явно
            // (в порядке возрастания приоритета: appsettings.json → user secrets → env vars).
            builder.Configuration.Sources.Clear();
            builder.Configuration
                .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
                    optional: false, reloadOnChange: false)
                .AddUserSecrets<Program>(optional: true)
                .AddEnvironmentVariables();

            // Привязываем настройки из секции "MPIC"
            builder.Services.Configure<RootSettings>(
                builder.Configuration.GetSection(RootSettings.SectionName));

            // Поддержка Windows Service
            builder.Services.AddWindowsService();

            // Регистрируем фоновую службу
            builder.Services.AddHostedService<MpicWorker>();

            var host = builder.Build();
            await host.RunAsync();
        }
    }
}
