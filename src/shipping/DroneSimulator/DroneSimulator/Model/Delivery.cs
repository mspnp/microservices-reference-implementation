using System;
using System.Collections.Generic;
using System.Text;

namespace DroneSimulator.Model
{
    public class Delivery
    {
        public Delivery() { }

        public Delivery(string id,
                        Location pickup,
                        Location dropoff,
                        string deadline,
                        bool expedited,
                        string droneid)
        {
            Id = id;
            Pickup = pickup;
            Dropoff = dropoff;
            Deadline = deadline;
            Expedited = expedited;
            DroneId = droneid;
        }

        public string Id { get; set; }
        public Location Pickup { get; set; }
        public Location Dropoff { get; set; }
        public string Deadline { get; set; }
        public bool Expedited { get; set; }
        public string DroneId { get; set; }
    }
}
