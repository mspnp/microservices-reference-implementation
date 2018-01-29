// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Newtonsoft.Json;
using Fabrikam.DroneDelivery.DeliveryService.Models;

namespace Fabrikam.DroneDelivery.DeliveryService.Services
{
    public static class EventHubSender<T> where T : BaseMessage
    {
        private static string EhConnectionString;
        private static string EhEntityPath;

        private static Lazy<EventHubClient> lazyConnection = new Lazy<EventHubClient>(() =>
        {
            // it is does guarantee thread-safety
            var connectionStringBuilder = new EventHubsConnectionStringBuilder(EhConnectionString)
            {
                EntityPath = EhEntityPath
            };

            return EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());

        });

        private static EventHubClient connection
        {
            get
            {
                return lazyConnection.Value;
            }
        }

        public static void Configure(string connectionString, string entityPath)
        {
            EhConnectionString = connectionString;
            EhEntityPath = entityPath;
        }

        public static async Task SendMessageAsync(DeliveryHistory deliveryHistory, string messageType, string partitionKey)
        {
            deliveryHistory.PartitionKey = partitionKey;
            deliveryHistory.MessageType = messageType;
            string jsonDeliveryHistory = await Task.Factory.StartNew(() => JsonConvert.SerializeObject(deliveryHistory));
            // TODO: send a batch to EH improves the performance a lot. Therefore, instead of sending milestones, we could send them all in a batch (TBD)
            await connection.SendAsync(new EventData(Encoding.UTF8.GetBytes(jsonDeliveryHistory)), partitionKey).ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}