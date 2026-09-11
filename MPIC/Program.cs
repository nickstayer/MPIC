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
                    // Убираем источники по умолчанию (включая appsettings.{env}.json),
                    // и добавляем только то, что нужно:
                    //   appsettings.json + User Secrets + Переменные окружения + Командная строка
                    config.Sources.Clear();

                    // 1. appsettings.json — обязательный, без перезагрузки
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

                    // 2. User Secrets (только в Development, нужен UserSecretsId в .csproj)
                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        config.AddUserSecrets<Program>();
                    }

                    // 3. Переменные окружения с префиксом MPIC_
                    //    Например: MPIC_MpicSettings__Username переопределит MpicSettings:Username
                    config.AddEnvironmentVariables("MPIC_");

                    // 4. Аргументы командной строки (для запуска вручную / отладки)
                    if (args is { Length: > 0 })
                    {
                        config.AddCommandLine(args);
                    }
                })
                .ConfigureServices((context, services) =>
                {
                    // Привязываем секцию MpicSettings к strongly-typed модели RootSettings
                    services.Configure<RootSettings>(context.Configuration.GetSection("MpicSettings"));

                    services.AddSingleton<NotificationManager>();
                    services.AddHostedService<MpicWorker>();
                })
                .Build();

            await host.RunAsync();
        }
    }
}