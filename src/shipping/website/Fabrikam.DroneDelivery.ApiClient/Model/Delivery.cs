using System;

namespace Fabrikam.DroneDelivery.ApiClient.Model
{
    public class Delivery
    {
        public Guid Id { get; set; }

        public Owner Owner { get; set; }

        public Location Pickup { get; set; }

        public Location Dropoff { get; set; }

        public string Deadline { get; set; }

        public bool Expedited { get; set; }

        public int ConfirmationRequired { get; set; }

        public string DroneId { get; set; }
    }
}
