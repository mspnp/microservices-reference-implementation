using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fabrikam.Workflow.Service
{
    internal class WorkflowService : IHostedService
    {
        private readonly ILogger<WorkflowService> _logger;
        private Timer _timer;

        public WorkflowService(ILogger<WorkflowService> logger)
        {
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(c =>
                {
                    _logger.LogInformation("Processing...");
                }, null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(30));


            _logger.LogInformation("Started");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _timer.Dispose();

            _logger.LogInformation("Stopped");
            return Task.CompletedTask;
        }
    }
}
