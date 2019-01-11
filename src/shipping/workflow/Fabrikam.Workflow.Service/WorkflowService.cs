using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Primitives;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fabrikam.Workflow.Service
{
    internal class WorkflowService : IHostedService
    {
        private readonly ILogger<WorkflowService> _logger;
        private readonly IOptions<WorkflowServiceOptions> _options;
        private QueueClient _receiveClient;

        public WorkflowService(ILogger<WorkflowService> logger, IOptions<WorkflowServiceOptions> options)
        {
            _logger = logger;
            _options = options;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            TokenProvider tokenProvider = TokenProvider.CreateManagedServiceIdentityTokenProvider();
            _receiveClient = new QueueClient(_options.Value.QueueEndpoint, _options.Value.QueueName, tokenProvider, receiveMode: ReceiveMode.PeekLock);
            _receiveClient.RegisterMessageHandler(
                ProcessMessageAsync,
                new MessageHandlerOptions(ProcessMessageExceptionAsync)
                {
                    AutoComplete = true,
                    MaxConcurrentCalls = _options.Value.MaxConcurrency
                });

            _logger.LogInformation("Started");
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _receiveClient?.CloseAsync();

            _logger.LogInformation("Stopped");
        }

        private Task ProcessMessageAsync(Message message, CancellationToken ct)
        {
            _logger.LogInformation("Processing message {messageId}", message.MessageId);

            return Task.CompletedTask;
        }

        private Task ProcessMessageExceptionAsync(ExceptionReceivedEventArgs exceptionEvent)
        {
            _logger.LogWarning("Exception processing message {exception}", exceptionEvent.Exception);

            return Task.CompletedTask;
        }
    }
}
