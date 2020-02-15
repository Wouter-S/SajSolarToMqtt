using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace SajSolarToMqtt
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
                .ConfigureServices((services) =>
                {
                    services.AddHostedService<SolarService>();
                });

            await builder.RunConsoleAsync();
        }
    }
}
