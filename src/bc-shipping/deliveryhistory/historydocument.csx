#load "Model.csx"
using System;


public class HistoryDocument
{
    public HistoryDocument(DeliveryHistory history)
    {
        //Set the first character of deliveryID as partition key
        this.PartitionKey = history.Id.ToCharArray()[0].ToString();
        this.DeliveryStatus = history.DeliveryStatus;
       
    }
    public string PartitionKey {get;}
    public DeliveryStatusEvent[] DeliveryStatus { get; }

}
