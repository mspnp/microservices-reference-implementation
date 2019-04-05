// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Fabrikam.Workflow.Service.Models;
using Fabrikam.Workflow.Service.RequestProcessing;
using Newtonsoft.Json;

namespace Fabrikam.Workflow.Service
{
    internal class WorkflowService : IHostedService
    {
        private readonly JsonSerializer _serializer;

        private readonly ILogger<WorkflowService> _logger;
        private readonly IRequestProcessor _requestProcessor;
        private readonly Func<IOptions<WorkflowServiceOptions>, IQueueClient> _createQueueClient;
        private readonly IOptions<WorkflowServiceOptions> _options;
        private IQueueClient _receiveClient;

        public WorkflowService(IOptions<WorkflowServiceOptions> options, ILogger<WorkflowService> logger, IRequestProcessor requestProcessor)
            : this(options, logger, requestProcessor, CreateQueueClient)
        { }

        public WorkflowService(IOptions<WorkflowServiceOptions> options, ILogger<WorkflowService> logger, IRequestProcessor requestProcessor, Func<IOptions<WorkflowServiceOptions>, IQueueClient> createQueueClient)
        {
            _options = options;
            _logger = logger;
            _requestProcessor = requestProcessor;
            _createQueueClient = createQueueClient;

            _serializer = new JsonSerializer();
        }

        private static IQueueClient CreateQueueClient(IOptions<WorkflowServiceOptions> options)
        {
            var connectionStringBuilder = new ServiceBusConnectionStringBuilder
            {
                Endpoint = options.Value.QueueEndpoint,
                EntityPath = options.Value.QueueName,
                SasKeyName = options.Value.QueueAccessPolicyName,
                SasKey = options.Value.QueueAccessPolicyKey,
                TransportType = TransportType.Amqp
            };

            return new QueueClient(connectionStringBuilder)
            {
                PrefetchCount = options.Value.PrefetchCount
            };
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _receiveClient = _createQueueClient(_options);

            _receiveClient.RegisterMessageHandler(
                ProcessMessageAsync,
                new MessageHandlerOptions(ProcessMessageExceptionAsync)
                {
                    AutoComplete = false,
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

        private async Task ProcessMessageAsync(Message message, CancellationToken ct)
        {
            _logger.LogInformation("Processing message {messageId}", message.MessageId);

            if (TryGetDelivery(message, out var delivery))
            {
                try
                {
                    if (await _requestProcessor.ProcessDeliveryRequestAsync(delivery, new ReadOnlyDictionary<string, object>(message.UserProperties)))
                    {
                        await _receiveClient.CompleteAsync(message.SystemProperties.LockToken);
                        return;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error processing message {messageId}", message.MessageId);
                }
            }

            try
            {
                await _receiveClient.DeadLetterAsync(message.SystemProperties.LockToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error moving message {messageId} to dead letter queue", message.MessageId);
            }

            return;
        }

        private Task ProcessMessageExceptionAsync(ExceptionReceivedEventArgs exceptionEvent)
        {
            _logger.LogError(exceptionEvent.Exception, "Exception processing message");

            return Task.CompletedTask;
        }

        private bool TryGetDelivery(Message message, out Delivery delivery)
        {
            try
            {
                using (var payloadStream = new MemoryStream(message.Body, false))
                using (var streamReader = new StreamReader(payloadStream, Encoding.UTF8))
                using (var jsonReader = new JsonTextReader(streamReader))
                {
                    delivery = _serializer.Deserialize<Delivery>(jsonReader);
                }

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Cannot parse payload from message {messageId}", message.MessageId);
            }

            delivery = null;
            return false;
        }
    }
}
