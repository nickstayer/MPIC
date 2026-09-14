using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MPIC
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Configuration.Sources.Clear();
            builder.Configuration
                .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
                    optional: false, reloadOnChange: false)
                .AddUserSecrets<Program>(optional: true)
                .AddEnvironmentVariables();
            builder.Services.Configure<RootSettings>(
                builder.Configuration.GetSection(RootSettings.SectionName));
            builder.Services.AddWindowsService();
            builder.Services.AddHostedService<MpicWorker>();

            var host = builder.Build();
            await host.RunAsync();
        }
    }
}
