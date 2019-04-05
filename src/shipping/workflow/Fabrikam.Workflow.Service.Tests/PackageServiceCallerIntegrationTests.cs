// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Fabrikam.Workflow.Service.Models;
using Fabrikam.Workflow.Service.Services;
using Fabrikam.Workflow.Service.Tests.Utils;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Fabrikam.Workflow.Service.Tests
{
    public class PackageServiceCallerIntegrationTests : IDisposable, IClassFixture<ResiliencyEnvironmentVariablesFixture>
    {
        private const string PackageHost = "packagehost";
        private static readonly string PackageUri = $"http://{PackageHost}/api/packages/";

        private readonly TestServer _testServer;
        private RequestDelegate _handleHttpRequest = ctx => Task.CompletedTask;

        private readonly IPackageServiceCaller _caller;

        public PackageServiceCallerIntegrationTests()
        {
            var context = new HostBuilderContext(new Dictionary<object, object>());
            context.Configuration =
                new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string> { ["SERVICE_URI_PACKAGE"] = PackageUri })
                    .AddEnvironmentVariables()
                    .Build();
            context.HostingEnvironment =
                Mock.Of<Microsoft.Extensions.Hosting.IHostingEnvironment>(e => e.EnvironmentName == "Test");

            var serviceCollection = new ServiceCollection();
            ServiceStartup.ConfigureServices(context, serviceCollection);
            serviceCollection.AddLogging(builder => builder.AddDebug());

            _testServer =
                new TestServer(
                    new WebHostBuilder()
                        .Configure(builder =>
                        {
                            builder.UseMvc();
                            builder.Run(ctx => _handleHttpRequest(ctx));
                        })
                        .ConfigureServices(builder =>
                        {
                            builder.AddMvc();
                        }));

            serviceCollection.Replace(
                ServiceDescriptor.Transient<HttpMessageHandlerBuilder, TestServerMessageHandlerBuilder>(
                    sp => new TestServerMessageHandlerBuilder(_testServer)));
            var serviceProvider = serviceCollection.BuildServiceProvider();

            _caller = serviceProvider.GetService<IPackageServiceCaller>();
        }

        public void Dispose()
        {
            _testServer.Dispose();
        }

        [Fact]
        public async Task WhenCreatingPackage_ThenInvokesDroneSchedulerAPI()
        {
            string actualPackageId = null;
            PackageGen actualPackage = null;
            _handleHttpRequest = ctx =>
            {
                if (ctx.Request.Host.Host == PackageHost)
                {
                    actualPackageId = ctx.Request.Path;
                    actualPackage =
                        new JsonSerializer().Deserialize<PackageGen>(new JsonTextReader(new StreamReader(ctx.Request.Body, Encoding.UTF8)));
                    ctx.Response.StatusCode = StatusCodes.Status201Created;
                }
                else
                {
                    ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }

                return Task.CompletedTask;
            };

            var packageInfo = new PackageInfo { PackageId = "somePackageId", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d };
            await _caller.UpsertPackageAsync(packageInfo);

            Assert.NotNull(actualPackageId);
            Assert.Equal($"/api/packages/{packageInfo.PackageId}", actualPackageId);

            Assert.NotNull(actualPackage);
            Assert.Equal((int)packageInfo.Size, (int)actualPackage.Size);
            Assert.Equal(packageInfo.Tag, actualPackage.Tag);
            Assert.Equal(packageInfo.Weight, actualPackage.Weight);
        }

        [Fact]
        public async Task WhenUpdatingPackage_ThenInvokesDroneSchedulerAPI()
        {
            // Arrange
            string actualPackageId = null;
            PackageGen actualPackage = null;
            Stream body = null;
            _handleHttpRequest = ctx =>
            {
                if (ctx.Request.Host.Host == PackageHost &&
                    ctx.Request.Method.Equals("PUT"))
                {
                    ctx.Response.StatusCode = StatusCodes.Status204NoContent;
                    body = ctx.Request.Body;
                }
                else if (ctx.Request.Host.Host == PackageHost &&
                    ctx.Request.Method.Equals("GET"))
                {
                    actualPackageId = ctx.Request.Path;
                    actualPackage =
                        new JsonSerializer()
                               .Deserialize<PackageGen>(
                                       new JsonTextReader(
                                           new StreamReader(
                                               body,
                                               Encoding.UTF8)));
                }
                else
                {
                    ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }

                return Task.CompletedTask;
            };

            var packageInfo = new PackageInfo { PackageId = "somePackageId", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d };

            // Act
            await _caller.UpsertPackageAsync(packageInfo);

            // Assert
            Assert.NotNull(actualPackageId);
            Assert.Equal($"/api/packages/{packageInfo.PackageId}", actualPackageId);

            Assert.NotNull(actualPackage);
            Assert.Equal((int)packageInfo.Size, (int)actualPackage.Size);
            Assert.Equal(packageInfo.Tag, actualPackage.Tag);
            Assert.Equal(packageInfo.Weight, actualPackage.Weight);
        }

        [Fact]
        public async Task WhenPackageAPIReturnsOK_ThenReturnsGeneratedPackage()
        {
            _handleHttpRequest = async ctx =>
            {
                if (ctx.Request.Host.Host == PackageHost)
                {
                    await ctx.WriteResultAsync(
                        new ObjectResult(
                            new PackageGen { Id = "somePackageId", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d })
                        {
                            StatusCode = StatusCodes.Status201Created
                        });
                }
                else
                {
                    ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }
            };

            var packageInfo = new PackageInfo { PackageId = "somePackageId", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d };
            var actualPackage = await _caller.UpsertPackageAsync(packageInfo);

            Assert.NotNull(actualPackage);
            Assert.Equal((int)packageInfo.Size, (int)actualPackage.Size);
            Assert.Equal(packageInfo.Tag, actualPackage.Tag);
            Assert.Equal(packageInfo.Weight, actualPackage.Weight);
        }

        [Fact]
        public async Task WhenPackageAPIReturnsNoContent_ThenReturnsUpdatedPackage()
        {
            // Arrange
            _handleHttpRequest = async ctx =>
            {
                if (ctx.Request.Host.Host == PackageHost &&
                    ctx.Request.Method.Equals("PUT"))
                {
                    ctx.Response.StatusCode = StatusCodes.Status204NoContent;
                }
                else if (ctx.Request.Host.Host == PackageHost &&
                    ctx.Request.Method.Equals("GET"))
                {
                    await ctx.WriteResultAsync(
                        new ObjectResult(
                            new PackageGen { Id = "somePackageId", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d })
                        {
                            StatusCode = StatusCodes.Status200OK
                        });

                }
                else
                {
                    ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }
            };

            var packageInfo = new PackageInfo { PackageId = "somePackageId", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d };

            // Act
            var actualPackage = await _caller.UpsertPackageAsync(packageInfo);

            // Assert
            Assert.NotNull(actualPackage);
            Assert.Equal((int)packageInfo.Size, (int)actualPackage.Size);
            Assert.Equal(packageInfo.Tag, actualPackage.Tag);
            Assert.Equal(packageInfo.Weight, actualPackage.Weight);
        }

        [Fact]
        public async Task WhenPackageAPIDoesNotReturnOK_ThenThrows()
        {
            _handleHttpRequest = ctx =>
            {
                if (ctx.Request.Host.Host == PackageHost)
                {
                    ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                }
                else
                {
                    ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }

                return Task.CompletedTask;
            };

            var packageInfo = new PackageInfo { PackageId = "somePackageId", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d };

            await Assert.ThrowsAsync<BackendServiceCallFailedException>(() => _caller.UpsertPackageAsync(packageInfo));
        }

        [Fact]
        public async Task WhenRequestsFail_TheyAreRetried()
        {
            var receivedRequests = 0;

            _handleHttpRequest = async ctx =>
            {
                if (ctx.Request.Host.Host == PackageHost)
                {
                    await ctx.WriteResultAsync(
                        new ObjectResult(
                            new PackageGen { Id = "somePackageId", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d })
                        {
                            StatusCode = Interlocked.Increment(ref receivedRequests) <= 2 ? StatusCodes.Status500InternalServerError : StatusCodes.Status201Created
                        });
                }
                else
                {
                    ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }
            };

            var result =
                await _caller.UpsertPackageAsync(new PackageInfo { PackageId = "package", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d });

            Assert.Equal(3, receivedRequests);
        }

        [Fact]
        public async Task WhenMultipleRequestsAreIssued_ThenBulkheadRestrictsConcurrentAccess()
        {
            const int totalRequests = 20;
            var receivedRequests = 0;
            var successfulRequests = 0;
            var failedRequests = 0;

            _handleHttpRequest = async ctx =>
            {
                if (ctx.Request.Host.Host == PackageHost)
                {
                    Interlocked.Increment(ref receivedRequests);
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    await ctx.WriteResultAsync(
                        new ObjectResult(
                            new PackageGen { Id = "somePackageId", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d })
                        {
                            StatusCode = StatusCodes.Status201Created
                        });
                }
                else
                {
                    ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }
            };

            var requestTasks =
                Enumerable.Range(1, totalRequests)
                    .Select(async i =>
                    {
                        try
                        {
                            var result =
                                await _caller.UpsertPackageAsync(new PackageInfo { PackageId = $"package{i}", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d });
                            Interlocked.Increment(ref successfulRequests);
                        }
                        catch
                        {
                            Interlocked.Increment(ref failedRequests);
                        }
                    });
            await Task.WhenAll(requestTasks);

            Assert.Equal(8, receivedRequests);
            Assert.Equal(8, successfulRequests);
            Assert.Equal(12, failedRequests);
        }

        [Fact]
        public async Task WhenRequestsFailAboveTheThreshold_ThenCircuitBreakerBreaks()
        {
            const int totalRequests = 100;
            var receivedRequests = 0;
            var successfulRequests = 0;
            var failedRequests = 0;

            _handleHttpRequest = ctx =>
            {
                Interlocked.Increment(ref receivedRequests);
                ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                return Task.CompletedTask;
            };

            var requestTasks =
                Enumerable.Range(1, totalRequests)
                    .Select(async i =>
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(i / 10));
                            var result =
                                await _caller.UpsertPackageAsync(new PackageInfo { PackageId = $"package{i}", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d });
                            Interlocked.Increment(ref successfulRequests);
                        }
                        catch
                        {
                            Interlocked.Increment(ref failedRequests);
                        }
                    });
            await Task.WhenAll(requestTasks);

            Assert.NotEqual(totalRequests * 4, receivedRequests);
            Assert.Equal(0, successfulRequests);
            Assert.Equal(totalRequests, failedRequests);
        }
    }
}
