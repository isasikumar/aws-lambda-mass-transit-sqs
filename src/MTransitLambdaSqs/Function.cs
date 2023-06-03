using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace MTransitLambdaSqs
{
    public class Function
    {
        private static readonly IConfiguration Configuration = new ConfigurationBuilder()
            #pragma warning disable CS8604 // Possible null reference argument.
            .SetBasePath(Directory.GetParent(AppContext.BaseDirectory)?.FullName)
            #pragma warning restore CS8604 // Possible null reference argument.
            .AddJsonFile("appsettings.json")
            .Build();
        public static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).Build().RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddMassTransit(x =>
                {
                    x.SetKebabCaseEndpointNameFormatter();
                    x.SetInMemorySagaRepositoryProvider();

                    var entryAssembly = Assembly.GetEntryAssembly();

                    x.AddConsumers(entryAssembly);
                    x.AddSagaStateMachines(entryAssembly);
                    x.AddSagas(entryAssembly);
                    x.AddActivities(entryAssembly);

                    x.UsingAmazonSqs((context, cfg) =>
                    {
                        cfg.Host(Configuration["AmazonSqs:Region"], host =>
                        {
                            host.AccessKey(Configuration["AmazonSqs:AccessKey"]);
                            host.SecretKey(Configuration["AmazonSqs:SecretKey"]);
                        });

                        cfg.ConfigureEndpoints(context);
                    });
                });

                services.AddHostedService<Worker>();

            });
    }
}