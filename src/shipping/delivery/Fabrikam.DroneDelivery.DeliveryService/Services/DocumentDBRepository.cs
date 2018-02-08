// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Logging;
using Fabrikam.DroneDelivery.DeliveryService.Models;

namespace Fabrikam.DroneDelivery.DeliveryService.Services
{
    public static class DocumentDBRepository<T> where T : BaseDocument
    {
        private static DocumentClient client;

        internal static string Endpoint;
        internal static string Key;
        internal static string DatabaseId;
        internal static string CollectionId;
        internal static ILogger logger;

        public static void Configure(string endpoint, string key, string databaseId, string collectionId, ILoggerFactory loggerFactory)
        {
            Endpoint = endpoint;
            Key = key;
            DatabaseId = databaseId;
            CollectionId = collectionId;

            client = new DocumentClient(new Uri(Endpoint), Key);
            logger = loggerFactory.CreateLogger(nameof(DocumentDBRepository<T>));
            logger.LogInformation($"Creating CosmosDb Database {DatabaseId} if not exists...");
            var taskCreateDb = client.CreateDatabaseIfNotExistsAsync(new Database { Id = DatabaseId });
            taskCreateDb.GetAwaiter().GetResult();
            logger.LogInformation($"CosmosDb Database {DatabaseId} creation if not exists: OK!");
            var dataBaseUri = UriFactory.CreateDatabaseUri(DatabaseId);
            logger.LogInformation($"Creating CosmosDb Collection {CollectionId} for {dataBaseUri.ToString()} if not exists...");
            var taskCreateDocCollection = client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(DatabaseId),
                new DocumentCollection { Id = CollectionId },
                new RequestOptions { OfferThroughput = 1000 });
            taskCreateDocCollection.GetAwaiter().GetResult();
            logger.LogInformation($"CosmosDb Collection {CollectionId} creation if not exists: OK!");
        }

        public static async Task<T> GetItemAsync(string id, string partitionKey)
        {
            using (logger.BeginScope(nameof(GetItemAsync)))
            {
                logger.LogInformation("id: {Id}, partitionKey: {PartitionKey}", id, partitionKey);

                try
                {
                    logger.LogInformation("Start: Using DocumentClient to read document");
                    Document document = await client.ReadDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id),
                                                                       new RequestOptions { PartitionKey = new PartitionKey(partitionKey) });
                    logger.LogInformation("End: Using DocumentClient to read document");

                    return (T)(dynamic)document;
                }
                catch (DocumentClientException e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
            }
        }

        public static async Task<IEnumerable<T>> GetItemsAsync(Expression<Func<T, bool>> predicate, string partitionKey)
        {
            using (logger.BeginScope(nameof(GetItemsAsync)))
            {
                logger.LogInformation("partitionKey: {PartitionKey}", partitionKey);

                IDocumentQuery<T> query = client.CreateDocumentQuery<T>(
                UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId),
                new FeedOptions { MaxItemCount = -1, PartitionKey = new PartitionKey(partitionKey) })
                .Where(predicate)
                .Where(d => d.DocumentType == typeof(T).ToString())
                .AsDocumentQuery();

                List<T> results = new List<T>();

                logger.LogInformation("Start: reading results from query");
                while (query.HasMoreResults)
                {
                    results.AddRange(await query.ExecuteNextAsync<T>());
                }
                logger.LogInformation("End: reading results from query");

                return results;
            }
        }

        public static async Task<Document> CreateItemAsync(T item, string partitionKey)
        {
            using (logger.BeginScope(nameof(CreateItemAsync)))
            {
                logger.LogInformation("partitionKey: {PartitionKey}", partitionKey);

                item.DocumentType = typeof(T).ToString();
                item.PartitionKey = partitionKey;

                try
                {
                    logger.LogInformation("Start: creating document");
                    var response = await client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId),
                                                            item,
                                                            new RequestOptions { PartitionKey = new PartitionKey(partitionKey) });
                    logger.LogInformation("End: creating document");

                    return response;
                }
                catch (DocumentClientException e) when (e.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    throw new DuplicateResourceException("DocDB Conflict", e);
                }
            }
        }

        public static async Task<Document> UpdateItemAsync(string id, T item, string partitionKey)
        {
            using (logger.BeginScope(nameof(UpdateItemAsync)))
            {
                logger.LogInformation("id: {Id}, partitionKey: {PartitionKey}", id, partitionKey);

                item.DocumentType = typeof(T).ToString();

                logger.LogInformation("Start: replacing document");
                var document = await client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, CollectionId, id),
                                                         item,
                                                         new RequestOptions { PartitionKey = new PartitionKey(partitionKey) });
                logger.LogInformation("End: replacing document");

                return document;
            }
        }
    }
}