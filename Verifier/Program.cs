using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Verifier;
using Verifier.Serivces;

public class Program
{
    private static void Main(string[] args)
    {
        IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<BgSerivce>();
        services.AddScoped<Pop3EmailService>();
        services.AddScoped<IMAPService>();
        services.AddScoped<RedditService>();

    })
    .Build();
        host.Run();
    }
}