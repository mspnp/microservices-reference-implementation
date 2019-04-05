// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
    public class DroneSchedulerServiceCallerIntegrationTests : IDisposable, IClassFixture<ResiliencyEnvironmentVariablesFixture>
    {
        private const string DroneSchedulerHost = "dronescheduler";
        private static readonly string DroneSchedulerUri = $"http://{DroneSchedulerHost}/api/DroneDeliveries/";

        private readonly TestServer _testServer;
        private RequestDelegate _handleHttpRequest = ctx => Task.CompletedTask;

        private readonly IDroneSchedulerServiceCaller _caller;

        public DroneSchedulerServiceCallerIntegrationTests()
        {
            var context = new HostBuilderContext(new Dictionary<object, object>());
            context.Configuration =
                new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string> { ["SERVICE_URI_DRONE"] = DroneSchedulerUri })
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

            _caller = serviceProvider.GetService<IDroneSchedulerServiceCaller>();
        }

        public void Dispose()
        {
            _testServer.Dispose();
        }

        [Fact]
        public async Task WhenGettingDroneId_ThenInvokesDroneSchedulerAPI()
        {
            string actualDeliveryId = null;
            DroneDelivery actualDelivery = null;
            _handleHttpRequest = ctx =>
            {
                if (ctx.Request.Host.Host == DroneSchedulerHost)
                {
                    actualDeliveryId = ctx.Request.Path;
                    actualDelivery =
                        new JsonSerializer().Deserialize<DroneDelivery>(new JsonTextReader(new StreamReader(ctx.Request.Body, Encoding.UTF8)));
                }
                else
                {
                    ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }

                return Task.CompletedTask;
            };

            var delivery =
                new Delivery
                {
                    DeliveryId = "someDeliveryId",
                    PackageInfo = new PackageInfo { PackageId = "somePackageId", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d }
                };
            await _caller.GetDroneIdAsync(delivery);

            Assert.NotNull(actualDeliveryId);
            Assert.Equal($"/api/DroneDeliveries/{delivery.DeliveryId}", actualDeliveryId);

            Assert.NotNull(actualDelivery);
            Assert.Equal(delivery.DeliveryId, actualDelivery.DeliveryId);
            Assert.Equal(delivery.PackageInfo.PackageId, actualDelivery.PackageDetail.Id);
            Assert.Equal((int)delivery.PackageInfo.Size, (int)actualDelivery.PackageDetail.Size);
        }

        [Fact]
        public async Task WhenDroneSchedulerAPIReturnsOK_ThenReturnsDroneId()
        {
            _handleHttpRequest = async ctx =>
            {
                if (ctx.Request.Host.Host == DroneSchedulerHost)
                {
                    await ctx.WriteResultAsync(new ContentResult { Content = "someDroneId", StatusCode = StatusCodes.Status200OK });
                }
                else
                {
                    ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }
            };

            var delivery =
                new Delivery
                {
                    DeliveryId = "someDeliveryId",
                    PackageInfo = new PackageInfo { PackageId = "somePackageId", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d }
                };
            var actualDroneId = await _caller.GetDroneIdAsync(delivery);

            Assert.Equal("someDroneId", actualDroneId);
        }

        [Fact]
        public async Task WhenDroneSchedulerAPIDoesNotReturnOK_ThenThrows()
        {
            _handleHttpRequest = ctx =>
            {
                if (ctx.Request.Host.Host == DroneSchedulerHost)
                {
                    ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                }
                else
                {
                    ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }

                return Task.CompletedTask;
            };

            var delivery =
                new Delivery
                {
                    DeliveryId = "someDeliveryId",
                    PackageInfo = new PackageInfo { PackageId = "somePackageId", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d }
                };

            await Assert.ThrowsAsync<BackendServiceCallFailedException>(() => _caller.GetDroneIdAsync(delivery));
        }
    }
}

