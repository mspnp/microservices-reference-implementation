#load "Model.csx"
using System;


public class HistoryDocument
{
    public HistoryDocument(DeliveryHistory history)
    {
        //Set the first character of deliveryID as partition key
        this.PartitionKey = history.Id.ToCharArray()[0].ToString();
        this.DeliveryTrackingEvents = history.DeliveryTrackingEvents;
       
    }
    public string PartitionKey {get;}
    public DeliveryTrackingEvent[] DeliveryTrackingEvents { get; }

}
