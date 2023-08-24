using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Verifier
{
    internal class BgSerivce : BackgroundService
    {

        private readonly IServiceProvider _services;


        public BgSerivce(IServiceProvider services)
        {
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CallRedditService();
                }
                catch
                {
                }
                finally
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        private async Task CallRedditService()
        {

            var scope = _services.CreateScope();
            var rdSerivce = scope.ServiceProvider.GetRequiredService<RedditService>();
            await rdSerivce.StartSerivce();
        }
    }
}
