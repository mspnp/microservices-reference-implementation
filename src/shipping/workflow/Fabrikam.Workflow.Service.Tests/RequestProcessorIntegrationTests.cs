// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Fabrikam.Workflow.Service.Tests
{
    public class RequestProcessorFixture : IDisposable
    {
        private const string PackageHost = "package";
        private static readonly string PackageUri = $"http://{PackageHost}/api/packages/";

        private readonly IRequestProcessor _requestProcessor;
        private readonly TestServer _testServer;
        private RequestDelegate _handleHttpRequest = ctx => Task.CompletedTask;

        public RequestProcessorFixture()
        {
            var context = new HostBuilderContext(new Dictionary<object, object>());
            context.Configuration =
                new ConfigurationBuilder().AddInMemoryCollection(
                    new Dictionary<string, string>
                    {
                        ["SERVICE_URI_PACKAGE"] = PackageUri
                    }).Build();
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
        public async Task ProcessingDelivery_InvokesPackageService()
        {
            JObject actualPackageInfo = null;
            _handleHttpRequest = async ctx =>
            {
                if (ctx.Request.Host.Host == PackageHost)
                {
                    actualPackageInfo = await JObject.LoadAsync(new JsonTextReader(new StreamReader(ctx.Request.Body, Encoding.UTF8)));

                    await ctx.WriteResultAsync(
                        new ObjectResult(
                            new PackageGen { Id = "package", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d })
                        {
                            StatusCode = (int)HttpStatusCode.Created
                        });
                }
                else
                {
                    ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }
            };

            var delivery =
                new Delivery
                {
                    DeliveryId = "delivery",
                    PackageInfo = new PackageInfo { PackageId = "package", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d }
                };
            var success = await _requestProcessor.ProcessDeliveryRequestAsync(delivery, new Dictionary<string, object>());

            Assert.True(success);

            Assert.NotNull(actualPackageInfo);
            Assert.Equal("package", actualPackageInfo["PackageId"].Value<string>());
            Assert.Equal((int)ContainerSize.Medium, actualPackageInfo["Size"].Value<int>());
            Assert.Equal("sometag", actualPackageInfo["Tag"].Value<string>());
            Assert.Equal(100d, actualPackageInfo["Weight"].Value<double>());
        }

        [Fact]
        public async Task WhenPackageServiceSucceeds_ThenRequestSucceeds()
        {
            _handleHttpRequest = async ctx =>
            {
                if (ctx.Request.Host.Host == PackageHost)
                {
                    await ctx.WriteResultAsync(
                        new ObjectResult(
                            new PackageGen { Id = "package", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d })
                        {
                            StatusCode = (int)HttpStatusCode.Created
                        });
                }
                else
                {
                    ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }
            };

            var delivery =
                new Delivery
                {
                    DeliveryId = "delivery",
                    PackageInfo = new PackageInfo { PackageId = "package", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d }
                };
            var success = await _requestProcessor.ProcessDeliveryRequestAsync(delivery, new Dictionary<string, object>());

            Assert.True(success);
        }

        [Fact]
        public async Task WhenPackageServiceFails_ThenRequestFails()
        {
            _handleHttpRequest = ctx =>
            {
                ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return Task.CompletedTask;
            };

            var delivery =
                new Delivery
                {
                    DeliveryId = "delivery",
                    PackageInfo = new PackageInfo { PackageId = "package", Size = ContainerSize.Medium, Tag = "sometag", Weight = 100d }
                };
            var success = await _requestProcessor.ProcessDeliveryRequestAsync(delivery, new Dictionary<string, object>());

            Assert.False(success);
        }
    }
}

