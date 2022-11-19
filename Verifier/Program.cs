using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Verifier;

public class Program
{
    private static void Main(string[] args)
    {
        IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<BgSerivce>();
        services.AddScoped<RedditService>();
    })
    .Build();
        host.Run();
    }
}