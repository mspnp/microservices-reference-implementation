using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Fabrikam.Workflow.Service
{
    internal static class ResiliencyExtensions
    {
        public static IHttpClientBuilder AddResiliencyPolicies(this IHttpClientBuilder builder, IConfiguration configuration)
        {
            var resiliencyConfiguration = configuration.GetSection("ServiceRequest").Get<ResiliencyConfiguration>();

            builder
                .AddPolicyHandler(
                    Policy.BulkheadAsync<HttpResponseMessage>(resiliencyConfiguration.MaxBulkheadSize, resiliencyConfiguration.MaxBulkheadQueueSize))
                .AddTransientHttpErrorPolicy(p =>
                    p.AdvancedCircuitBreakerAsync(
                        resiliencyConfiguration.CircuitBreakerThreshold,
                        TimeSpan.FromSeconds(resiliencyConfiguration.CircuitBreakerSamplingPeriodSeconds),
                        resiliencyConfiguration.CircuitBreakerMinimumThroughput,
                        TimeSpan.FromSeconds(resiliencyConfiguration.CircuitBreakerBreakDurationSeconds)))
                .AddTransientHttpErrorPolicy(p =>
                    p.WaitAndRetryAsync(resiliencyConfiguration.MaxRetries, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) - 2)));

            return builder;
        }

        private class ResiliencyConfiguration
        {
            public int MaxRetries { get; set; }

            public double CircuitBreakerThreshold { get; set; }

            public int CircuitBreakerSamplingPeriodSeconds { get; set; }

            public int CircuitBreakerMinimumThroughput { get; set; }

            public int CircuitBreakerBreakDurationSeconds { get; set; }

            public int MaxBulkheadSize { get; set; }

            public int MaxBulkheadQueueSize { get; set; }
        }
    }
}
