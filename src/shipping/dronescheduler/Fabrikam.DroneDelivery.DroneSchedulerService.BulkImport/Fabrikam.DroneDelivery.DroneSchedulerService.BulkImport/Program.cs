// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.CosmosDB.BulkExecutor;
using Microsoft.Azure.CosmosDB.BulkExecutor.BulkImport;
using Microsoft.Extensions.Configuration;

namespace Fabrikam.DroneDelivery.DroneSchedulerService.BulkImport
{
    class Program
    {
        private static readonly ConnectionPolicy ConnectionPolicy =
            new ConnectionPolicy
            {
                ConnectionMode = ConnectionMode.Direct,
                ConnectionProtocol = Protocol.Tcp
            };

        private static readonly Dictionary<string, string> SwitchMappings =
            new Dictionary<string, string>
            {
                { "--auth-key", "importConfig:authorizationKey" },
                { "--endpoint-url", "importConfig:endpointUrl" },
                { "--database-name", "importConfig:databaseName" },
                { "--collection-name", "importConfig:collectionName" },
                { "--collection-partition-key", "importConfig:collectionPartitionKey" },
                { "--collection-throughput", "importConfig:collectionThroughput" },
                { "--document-type-name", "importConfig:documentTypeName" },
                { "--flatten-partition-key", "importConfig:flattenPartitionKey" },
                { "--number-of-batches", "importConfig:numberOfBatches" },
                { "--number-of-documents", "importConfig:numberOfDocuments" },
                { "--number-of-documents-exp-factor", "importConfig:numberOfDocumentsExpFactor" }
            };

        private readonly DocumentClient _client;
        private readonly ImportConfiguration _importConfig;

        private readonly AsyncLazy<DocumentCollection> _col;
        private readonly AsyncLazy<BulkExecutor> _bulkExecutor;

        private Program(
            DocumentClient client,
            ImportConfiguration importConfig)
        {
            this._client = client;
            this._importConfig = importConfig;

            this._col = new AsyncLazy<DocumentCollection>(async ()
                => await InitDbAndCollectionAsync().ConfigureAwait(false));

            this._bulkExecutor = new AsyncLazy<BulkExecutor>(async () =>
            {
                // Set retry options high for initialization (default values).
                _client.ConnectionPolicy.RetryOptions.MaxRetryWaitTimeInSeconds = 30;
                _client.ConnectionPolicy.RetryOptions.MaxRetryAttemptsOnThrottledRequests = 9;

                var bulkExecutor = new BulkExecutor(_client, await _col.Value);
                await bulkExecutor.InitializeAsync();

                // Set retries to 0 to pass control to bulk executor.
                _client.ConnectionPolicy.RetryOptions.MaxRetryWaitTimeInSeconds = 0;
                _client.ConnectionPolicy.RetryOptions.MaxRetryAttemptsOnThrottledRequests = 0;

                return bulkExecutor;
            });
        }

        private static async Task Main(string[] args)
        {
            try
            {
                IConfiguration config = new ConfigurationBuilder()
                    .AddCommandLine(args, SwitchMappings)
                    .Build();

                var importCfg = config
                    .GetSection("importConfig")
                    .Get<ImportConfiguration>();

                await Console.Out.WriteLineAsync("\n--------------------------------------------------------------------- ");
                await Console.Out.WriteLineAsync($"Endpoint: {importCfg.EndpointUrl}");
                await Console.Out.WriteLineAsync($"Collections : {importCfg.DatabaseName}.{importCfg.CollectionName}");
                await Console.Out.WriteLineAsync("--------------------------------------------------------------------- \n");

                using (var client = new DocumentClient(
                            new Uri(importCfg.EndpointUrl),
                            importCfg.AuthorizationKey,
                            ConnectionPolicy))
                {
                    var program = new Program(client, importCfg);

                    await program.BulkImportAsync()
                        .ConfigureAwait(false);
                }
            }
            catch (AggregateException e)
            {
                await Console.Out.WriteLineAsync($"Caught AggregateException in Main, Inner Exception:\n {e.Message}");
            }
            catch (Exception e)
            {
                await Console.Out.WriteLineAsync($"Caught Exception in Main:\n {e.Message}");
            }
            finally
            {
                await Console.Out.WriteLineAsync("\nPress any key to exit.");
                Console.ReadKey();
            }
        }

        private async Task BulkImportAsync()
        {
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;
            string partitionKeyProperty = (await _col.Value)
                .PartitionKey
                .Paths[0]
                .Replace("/", "");

            long totalNumberOfDocumentsInserted = 0;
            double totalRequestUnitsConsumed = 0;
            double totalTimeTakenSec = 0;

            for (int i = 0; i < _importConfig.NumberOfBatches; i++)
            {
                long numberDocsCurrentBatch =
                    _importConfig.NumberDocumentsPerPartitionExpFactor > 0
                    ? ExpNumberOfDocumentsPerBatch(i)
                    : _importConfig.NumberOfDocumentsPerBatch();

                var documentsToImportInBatch = new List<string>();
                long seed = i * numberDocsCurrentBatch;

                await Console.Out.WriteLineAsync($"\nGenerating {numberDocsCurrentBatch} documents to import for batch {i}");
                for (int j = 0; j < numberDocsCurrentBatch; j++)
                {
                    var id = (seed + j).ToString();
                    string doc = Utils.GenerateSyntheticDoc(
                            id,
                            i,
                            j,
                            _importConfig.DocumentTypeName,
                            partitionKeyProperty,
                            _importConfig.FlattenPartitionKey);

                    documentsToImportInBatch.Add(doc);
                }

                var (batchDocsImported, batchReqUnitsConsumed, batchSeconds) =
                    await ImportBatch(i, documentsToImportInBatch, token);

                totalNumberOfDocumentsInserted += batchDocsImported;
                totalRequestUnitsConsumed += batchReqUnitsConsumed;
                totalTimeTakenSec += batchSeconds;
            }

            await Console.Out.WriteLineAsync("\nOverall Summary:");
            await Console.Out.WriteLineAsync("--------------------------------------------------------------------- ");
            await Console.Out.WriteLineAsync(String.Format("Inserted {0} docs @ {1} writes/s, {2} RU/s in {3} sec",
                        totalNumberOfDocumentsInserted,
                        Math.Round(totalNumberOfDocumentsInserted / totalTimeTakenSec),
                        Math.Round(totalRequestUnitsConsumed / totalTimeTakenSec),
                        totalTimeTakenSec));
            await Console.Out.WriteLineAsync(String.Format("Average RU consumption per document: {0}",
                        (totalRequestUnitsConsumed / totalNumberOfDocumentsInserted)));
            await Console.Out.WriteLineAsync("--------------------------------------------------------------------- ");
        }

        private async Task<Tuple<long, double, double>> ImportBatch(
            int batchNuber,
            IEnumerable<string> documentsToImportInBatch,
            CancellationToken token)
        {
            await Console.Out.WriteLineAsync($"Executing bulk import for batch {batchNuber}");
            BulkImportResponse bulkImportResponse = null;

            do
            {
                try
                {
                    bulkImportResponse =
                    await (await _bulkExecutor.Value).BulkImportAsync(
                            documents: documentsToImportInBatch,
                            enableUpsert: false,
                            disableAutomaticIdGeneration: true,
                            maxConcurrencyPerPartitionKeyRange: null,
                            maxInMemorySortingBatchSize: null,
                            cancellationToken: token);
                }
                catch (DocumentClientException de)
                {
                    await Console.Out.WriteLineAsync($"Document client exception: {de.Message}");
                    break;
                }
                catch (Exception e)
                {
                    await Console.Out.WriteLineAsync($"Exception: {e.Message}");
                    break;
                }
            } while (bulkImportResponse.NumberOfDocumentsImported < documentsToImportInBatch.Count());

            await Console.Out.WriteLineAsync($"\nBatch Summary {batchNuber}:");
            await Console.Out.WriteLineAsync("--------------------------------------------------------------------- ");
            await Console.Out.WriteLineAsync(String.Format("Inserted {0} docs @ {1} writes/s, {2} RU/s in {3} sec",
                        bulkImportResponse.NumberOfDocumentsImported,
                        Math.Round(bulkImportResponse.NumberOfDocumentsImported / bulkImportResponse.TotalTimeTaken.TotalSeconds),
                        Math.Round(bulkImportResponse.TotalRequestUnitsConsumed / bulkImportResponse.TotalTimeTaken.TotalSeconds),
                        bulkImportResponse.TotalTimeTaken.TotalSeconds));
            await Console.Out.WriteLineAsync(String.Format("Average RU consumption per document: {0}",
                        (bulkImportResponse.TotalRequestUnitsConsumed / bulkImportResponse.NumberOfDocumentsImported)));
            await Console.Out.WriteLineAsync("--------------------------------------------------------------------- ");

            return Tuple.Create(
                bulkImportResponse.NumberOfDocumentsImported,
                bulkImportResponse.TotalRequestUnitsConsumed,
                bulkImportResponse.TotalTimeTaken.TotalSeconds);
        }

        private long ExpNumberOfDocumentsPerBatch(double x)
            => (long)(Math.Exp(x) * _importConfig.NumberDocumentsPerPartitionExpFactor);

        private async Task<DocumentCollection> InitDbAndCollectionAsync()
        {
            try
            {
                Database db = _client
                    .CreateDatabaseQuery()
                    .Where(d => d.Id == _importConfig.DatabaseName)
                    .AsEnumerable()
                    .FirstOrDefault();

                if (db != null)
                {
                    await Console.Out.WriteLineAsync($"Deleting pre-existent database {_importConfig.DatabaseName}...");
                    await _client.DeleteDatabaseAsync(db.SelfLink);
                }

                await Console.Out.WriteLineAsync($"Creating database {_importConfig.DatabaseName}...");
                db = await _client.CreateDatabaseAsync(
                        new Database
                        {
                            Id = _importConfig.DatabaseName
                        });

                var partitionKey = new PartitionKeyDefinition
                {
                    Paths = new Collection<string>
                    {
                        _importConfig.CollectionPartitionKey
                    }
                };

                var collection = new DocumentCollection
                {
                    Id = _importConfig.CollectionName,
                    PartitionKey = partitionKey
                };

                await Console.Out.WriteLineAsync($"Creating collection {_importConfig.CollectionName} with {_importConfig.CollectionThroughput} RU/s...");
                collection = await _client.CreateDocumentCollectionAsync(
                        db.SelfLink,
                        collection,
                        new RequestOptions
                        {
                            OfferThroughput = _importConfig.CollectionThroughput
                        });

                return collection;
            }
            catch (Exception e)
            {
                await Console.Out.WriteLineAsync($"Unable to initialize, exception message: {e.Message}");
                throw;
            }
        }

        private class AsyncLazy<T> : Lazy<Task<T>>
        {
            public AsyncLazy(Func<T> valueFactory) :
                base(() => Task.Run(valueFactory))
            {
            }

            public AsyncLazy(Func<Task<T>> taskFactory) :
                base(() => Task.Run(taskFactory))
            {
            }
        }
    }
}