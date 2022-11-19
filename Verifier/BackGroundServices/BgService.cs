using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Verifier.BackGroundServices
{
    public class BgService : BackgroundService
    {
        private readonly RedditService _program;
        private readonly ILogger<BgService> _logger;

        public BgService(RedditService program, ILogger<BgService> logger)
        {
            _program = program;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                 //   _program.

                     await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Message}", ex.Message);
                Environment.Exit(2);
            }
        }
    }
}
