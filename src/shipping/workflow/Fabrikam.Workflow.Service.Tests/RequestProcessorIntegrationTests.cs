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
using Fabrikam.Workflow.Service.RequestProcessing;
using Fabrikam.Workflow.Service.Tests.Utils;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Fabrikam.Workflow.Service.Tests
{
    public class RequestProcessorIntegrationTests : IDisposable, IClassFixture<ResiliencyEnvironmentVariablesFixture>
    {
        private const string DeliveryHost = "deliveryhost";
        private static readonly string DeliveryUri = $"http://{DeliveryHost}/api/Deliveries/";
        private const string DroneSchedulerHost = "dronescheduler";
        private static readonly string DroneSchedulerUri = $"http://{DroneSchedulerHost}/api/DroneDeliveries/";
        private const string PackageHost = "packagehost";
        private static readonly string PackageUri = $"http://{PackageHost}/api/packages/";

        private readonly IRequestProcessor _requestProcessor;
        private readonly TestServer _testServer;
        private RequestDelegate _handleHttpRequest = ctx => Task.CompletedTask;

        public RequestProcessorIntegrationTests()
        {
            var context = new HostBuilderContext(new Dictionary<object, object>());
            context.Configuration =
                new ConfigurationBuilder()
                    .AddInMemoryCollection(
                        new Dictionary<string, string>
                        {
                            ["SERVICE_URI_DELIVERY"] = DeliveryUri,
                            ["SERVICE_URI_DRONE"] = DroneSchedulerUri,
                            ["SERVICE_URI_PACKAGE"] = PackageUri
                        })
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

            _requestProcessor = serviceProvider.GetService<IRequestProcessor>();
        }

        public void Dispose()
        {
            _testServer.Dispose();
        }

        [Fact]
        public async Task ProcessingDelivery_InvokesPackageServiceAndDroneSchedulerService()
        {
            PackageGen actualPackage = null;
            DroneDelivery actualDelivery = null;
            DeliverySchedule actualDeliverySchedule = null;
            _handleHttpRequest = async ctx =>
            {
                var serializer = new JsonSerializer();

                if (ctx.Request.Host.Host == PackageHost)
                {
                    actualPackage = serializer.Deserialize<PackageGen>(new JsonTextReader(new StreamReader(ctx.Request.Body, Encoding.UTF8)));

                    await ctx.WriteResultAsync(
                        new ObjectResult(
                            new PackageGen { Id = "somePackageId", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d })
                        {
                            StatusCode = StatusCodes.Status201Created
                        });
                }
                else if (ctx.Request.Host.Host == DroneSchedulerHost)
                {
                    actualDelivery = serializer.Deserialize<DroneDelivery>(new JsonTextReader(new StreamReader(ctx.Request.Body, Encoding.UTF8)));

                    await ctx.WriteResultAsync(new ContentResult { Content = "someDroneId", StatusCode = StatusCodes.Status200OK });
                }
                else if (ctx.Request.Host.Host == DeliveryHost)
                {
                    actualDeliverySchedule = serializer.Deserialize<DeliverySchedule>(new JsonTextReader(new StreamReader(ctx.Request.Body, Encoding.UTF8)));

                    await ctx.WriteResultAsync(
                        new ObjectResult(new DeliverySchedule { Id = "someDeliveryId" })
                        {
                            StatusCode = StatusCodes.Status201Created
                        });
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
            await _requestProcessor.ProcessDeliveryRequestAsync(delivery, new Dictionary<string, object>());

            Assert.NotNull(actualPackage);
            Assert.Equal((int)delivery.PackageInfo.Size, (int)actualPackage.Size);
            Assert.Equal(delivery.PackageInfo.Tag, actualPackage.Tag);
            Assert.Equal(delivery.PackageInfo.Weight, actualPackage.Weight);

            Assert.NotNull(actualDelivery);
            Assert.Equal(delivery.DeliveryId, actualDelivery.DeliveryId);
            Assert.Equal(delivery.PackageInfo.PackageId, actualDelivery.PackageDetail.Id);
            Assert.Equal((int)delivery.PackageInfo.Size, (int)actualDelivery.PackageDetail.Size);

            Assert.NotNull(actualDeliverySchedule);
            Assert.Equal(delivery.DeliveryId, actualDeliverySchedule.Id);
            Assert.Equal("someDroneId", actualDeliverySchedule.DroneId);
        }
    }
}

