using System;
using System.Threading.Tasks;

namespace Fabrikam.DroneDelivery.Drone
{
    class Program
    {
        private static string apiUrl = null;
        private static string trackingId = null;

        static void Main(string[] args)
        {
            Console.WriteLine("Drone Delivery Starting...");

            var p = new Program();

            foreach (var arg in args)
            {
                if (arg.Contains("="))
                {
                    var key = arg.Split('=')[0].ToLower();
                    var value = arg.Split('=')[1];

                    switch(key)
                    {
                        case "apiurl":
                            apiUrl = value;
                            break;
                        case "trackingid":
                            trackingId = value;
                            break;
                    }
                }
            }

            Console.WriteLine($"ApiUrl: {apiUrl}");
            Console.WriteLine($"TrackingId: {trackingId}");

            var flightEngine = new FlightEngine(new DeliveryApi(apiUrl));
            flightEngine.ExecuteDroneDelivery(trackingId).GetAwaiter().GetResult();
        }
    }
}
