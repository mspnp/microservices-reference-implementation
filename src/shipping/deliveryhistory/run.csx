#r "Microsoft.ServiceBus"
#load "Model.csx"
#load "historydocument.csx"

using System;
using System.Text;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


public static async Task Run(EventData[] EventHubMessages, IAsyncCollector<HistoryDocument> outputDocument, TraceWriter log)
{
    foreach (var message in EventHubMessages) 
    {
        // Save each event in CosmosDB
        var sr = new StreamReader(message.GetBodyStream());
        var serializer = new JsonSerializer();
        DeliveryHistory history  = (DeliveryHistory)serializer.Deserialize(sr, typeof(DeliveryHistory));
        HistoryDocument historyDocument = new HistoryDocument(history);
        await outputDocument.AddAsync(historyDocument);
    }
}
