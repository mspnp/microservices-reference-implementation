using System;
using System.Collections.ObjectModel;
using Newtonsoft.Json;

    public abstract class BaseCache
    {
        public abstract string Key { get; }
    }
    public class DateTimeStamp
    {
        public DateTimeStamp(string dateTimeValue)
        {
            DateTimeValue = dateTimeValue;
        }

        public string DateTimeValue { get; set; }
    }
    public class Location
    {
        public Location(double altitude, double latitude, double longitude)
        {
            Altitude = altitude;
            Latitude = latitude;
            Longitude = longitude;
        }
        public double Altitude { get; }
        public double Latitude { get; }
        public double Longitude { get; }
    }
    public enum ConfirmationType
    {
        FingerPrint,
        Picture,
        Voice,
        None
    }
    public class Confirmation
    {
        public Confirmation(DateTimeStamp dateTime, Location geoCoordinates, ConfirmationType confirmationType, string confirmationBlob)
        {
            DateTime = dateTime;
            GeoCoordinates = geoCoordinates;
            ConfirmationType = confirmationType;
            ConfirmationBlob = confirmationBlob;
        }
        public DateTimeStamp DateTime { get; }
        public Location GeoCoordinates { get; }
        public ConfirmationType ConfirmationType { get; }
        public string ConfirmationBlob { get; }
    }
    public enum DeliveryEventType
    {
        Created,
        Rescheduled,
        DroneHeadingToPickupLocation,
        InTransit,
        DeliveryComplete,
        Cancelled
    }
    public class DeliveryStatusEvent : BaseCache
    {
        public string DeliveryId { get; set; }
        public DeliveryEventType Stage { get; set; }
        public Location Location { get; set; }
        public override string Key => $"{this.DeliveryId}_{this.Stage.ToString()}";
    }

    public class InternalDelivery : BaseCache
    {
        public InternalDelivery(string id, 
                        UserAccount owner, 
                        Location pickup, 
                        Location dropoff, 
                        ReadOnlyCollection<string> packageids, 
                        string deadline, 
                        bool expedited, 
                        ConfirmationType confirmationRequired,
                        string droneid)
        {
            Id = id;
            Owner = owner;
            Pickup = pickup;
            Dropoff = dropoff;
            PackageIds = packageids;
            Deadline = deadline;
            Expedited = expedited;
            ConfirmationRequired = confirmationRequired;
            DroneId = droneid;
        }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; }
        public UserAccount Owner { get; }
        public Location Pickup { get; }
        public Location Dropoff { get; }
        public ReadOnlyCollection<string> PackageIds { get; }
        public string Deadline { get; }
        public bool Expedited { get; }
        public ConfirmationType ConfirmationRequired { get; }
        public string DroneId { get; }
        public override string Key => this.Id;
    }
    public class BaseMessage
    {
        [JsonProperty(PropertyName = "partitionKey")]
        public string PartitionKey { get; set; }
        public string MessageType { get; set; }

    }
    
   public class UserAccount
    {
        public UserAccount(string userid, string accountid)
        {
            UserId = userid;
            AccountId = accountid;
        }
        public string UserId { get; }
        public string AccountId { get; }
    }
    public class DeliveryStatus
    {
        public DeliveryStatus(DeliveryEventType deliveryStage, Location lastKnownLocation, string pickupeta, string deliveryeta)
        {
            Stage = deliveryStage;
            LastKnownLocation = lastKnownLocation;
            PickupETA = pickupeta;
            DeliveryETA = deliveryeta;
        }
        public DeliveryEventType Stage { get; }
        public Location LastKnownLocation { get; }
        public string PickupETA { get; }
        public string DeliveryETA { get; }
    }
    public class Delivery
    {
        public Delivery(string id, 
                        UserAccount owner, 
                        Location pickup, 
                        Location dropoff, 
                        ReadOnlyCollection<string> packageids, 
                        string deadline, 
                        bool expedited, 
                        ConfirmationType confirmationRequired,
                        string droneid)
        {
            Id = id;
            Owner = owner;
            Pickup = pickup;
            Dropoff = dropoff;
            PackageIds = packageids;
            Deadline = deadline;
            Expedited = expedited;
            ConfirmationRequired = confirmationRequired;
            DroneId = droneid;
        }

        public string Id { get; }
        public UserAccount Owner { get; }
        public Location Pickup { get; }
        public Location Dropoff { get; }
        public ReadOnlyCollection<string> PackageIds { get; }
        public string Deadline { get; }
        public bool Expedited { get; }
        public ConfirmationType ConfirmationRequired { get; }
        public string DroneId { get; }
    }
     public class DeliveryHistory : BaseMessage
    {
        public DeliveryHistory(string id, 
                        InternalDelivery delivery,
                        params DeliveryStatusEvent[] deliveryStatus)
        {
            Id = id;
            Delivery = delivery;
            DeliveryStatus = deliveryStatus;
        }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; }
        public InternalDelivery Delivery { get; }
        public DeliveryStatusEvent[] DeliveryStatus { get; }
    }
    
