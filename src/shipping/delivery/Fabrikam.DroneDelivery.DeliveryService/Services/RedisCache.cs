// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using Fabrikam.DroneDelivery.DeliveryService.Models;

namespace Fabrikam.DroneDelivery.DeliveryService.Services
{
    public static class RedisCache<T> where T : BaseCache
    {
        private static ConfigurationOptions ConfigOptions;
        private static int DB;
        private static ILogger logger;

        private static Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            // automatically disposed when the AppDomain is torn down
            var connection = ConnectionMultiplexer.Connect(ConfigOptions);

            return connection;
        });

        private static Lazy<IDatabase> lazyCache = new Lazy<IDatabase>(() =>
        {
            return connection.GetDatabase(db: DB);
        });

        private static ConnectionMultiplexer connection
        {
            get
            {
                return lazyConnection.Value;
            }
        }

        private static IDatabase cache
        {
            get
            {
                return lazyCache.Value;
            }
        }

        public static void Configure(int db, string endPoint, string key, ILoggerFactory loggerFactory)
        {
            DB = db;
            ConfigOptions = new ConfigurationOptions
            {
                Password = key,
                EndPoints = { { endPoint } },
                Ssl = true,
                AbortOnConnectFail = false
            };
            logger = loggerFactory.CreateLogger(nameof(RedisCache<T>));
        }

        public static async Task<T> GetItemAsync(string id)
        {
            using (logger.BeginScope(nameof(GetItemAsync)))
            {
                logger.LogInformation("id: {Id}", id);

                logger.LogInformation("Start: reading value from Redis");
                var item = await cache.StringGetAsync(id);
                logger.LogInformation("End: reading value from Redis");
                //TODO: log info (this call took this long");

                if (!item.HasValue)
                    return default(T);

                //TODO: Validate if the deserialize is still necessary
                //TODO: Validate Task.Factory usage...
                return await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<T>(item)).ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        public static async Task<IEnumerable<T>> GetItemsAsync(IEnumerable<string> ids)
        {
            using (logger.BeginScope(nameof(GetItemsAsync)))
            {
                logger.LogInformation("ids: {@Ids}", ids);

                if (ids.Count() == 0)
                    return default(IEnumerable<T>);

                IEnumerable<Task<T>> allTasks = ids.Select(id => GetItemAsync(id));
                // async + in parallel (non-blocking)
                IEnumerable<T> allResults = await Task.WhenAll(allTasks).ConfigureAwait(continueOnCapturedContext: false);

                return allResults.Where(s => s != null);
            }
        }

        public static async Task<bool> CreateItemAsync(T item)
        {
            using (logger.BeginScope(nameof(CreateItemAsync)))
            {
                logger.LogInformation("Start: storing item in Redis");
                string jsonItem = await Task.Factory.StartNew(() => JsonConvert.SerializeObject(item));
                await cache.StringSetAsync(item.Key, jsonItem);
                var success = await cache.KeyExpireAsync(item.Key, DateTime.MaxValue.ToUniversalTime()).ConfigureAwait(continueOnCapturedContext: false);
                logger.LogInformation("End: storing item in Redis");

                return success;
            }
        }

        public static async Task<bool> UpdateItemAsync(string id, T item)
        {
            using (logger.BeginScope(nameof(UpdateItemAsync)))
            {
                logger.LogInformation("id: {Id}", id);

                logger.LogInformation("Start: updating item in Redis");
                string jsonItem = await Task.Factory.StartNew(() => JsonConvert.SerializeObject(item));
                var success = await cache.StringSetAsync(id, jsonItem).ConfigureAwait(continueOnCapturedContext: false);
                logger.LogInformation("End: updating item in Redis");

                return success;
            }
        }

        public static async Task<bool> DeleteItemAsync(string id, InternalDelivery delivery)
        {
            using (logger.BeginScope(nameof(DeleteItemAsync)))
            {
                logger.LogInformation("id: {Id}", id);

                logger.LogInformation("Start: updating item's TTL in Redis");
                var success = await cache.KeyExpireAsync(id, DateTime.UtcNow.AddDays(7)).ConfigureAwait(continueOnCapturedContext: false);
                logger.LogInformation("End: updating item's TTL in Redis");

                return success;
            }
        }
    }
}