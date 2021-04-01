using DroneSimulator.Model;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DroneSimulator
{
    public class DeliveryApi
    {
        public string apiUrl = null;
        private ILogger<DeliveryApi> _logger;
        private TelemetryClient _telemetry;
        public DeliveryApi(ILogger<DeliveryApi> logger, IConfiguration configuration, TelemetryClient telemetry)
        {
            apiUrl = configuration.GetValue<string>("ApiUrl");
            _telemetry = telemetry;
            _logger = logger;
        }

        public async Task<Delivery> GetDroneDelivery(string deliveryId)
        {

            try
            {
                var response = await Client.GetAsync($"{this.apiUrl}/api/Deliveries/{deliveryId}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var delivery = JsonConvert.DeserializeObject<Delivery>(json);
                return delivery;
            }
            catch (Exception ex)
            {
                var props = new Dictionary<string, string>() { { "deliveryId", deliveryId } };
                _telemetry.TrackEvent($"{nameof(GetDroneDelivery)}Exception", props);
                _logger.LogError(ex, $"Exception in {nameof(GetDroneDelivery)} for delivery Id : {deliveryId} . Exception Message : {ex.Message}");
                throw;
            }
        }

        public async Task<DroneLocation> GetDroneLocation(string deliveryId)
        {
            try
            {
                var response = await Client.GetAsync($"{this.apiUrl}/api/Drone/{deliveryId}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var delivery = JsonConvert.DeserializeObject<DroneLocation>(json);
                return delivery;
            }
            catch (Exception ex)
            {
                var props = new Dictionary<string, string>() { { "deliveryId", deliveryId } };
                _telemetry.TrackEvent($"{nameof(GetDroneLocation)}Exception", props);
                _logger.LogError(ex, $"Exception in {nameof(GetDroneLocation)} for delivery Id : {deliveryId} . Exception Message : {ex.Message}");
                return new DroneLocation()
                {
                    LastKnownLocation = new LastKnownLocation()
                    {
                        Altitude = 0,
                        Latitude = 0,
                        Longitude = 0
                    },
                    Stage = DeliveryStage.Created
                };
            }
        }

        public async Task UpdateDroneLocation(string deliveryId, DeliveryStage stage, double altitude, double latitude, double longitude)
        {
            try
            {
                Console.Write($"Stage: {stage} | Altitude: {altitude} | Latitude: {latitude} | Longitude: {longitude} | ");

                var deliveryTracking = new DeliveryTracking()
                {
                    DeliveryId = deliveryId,
                    Stage = stage,
                    Location = new DroneSimulator.Model.Location()
                    {
                        Altitude = altitude,
                        Latitude = latitude,
                        Longitude = longitude
                    }
                };

                var json = JsonConvert.SerializeObject(deliveryTracking);
                var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
                var response = await Client.PutAsync($"{this.apiUrl}/api/Drone/{deliveryId}", content);
            }
            catch (Exception ex)
            {
                var props = new Dictionary<string, string>() { { "deliveryId", deliveryId } };
                _telemetry.TrackEvent($"{nameof(UpdateDroneLocation)}Exception", props);
                _logger.LogError(ex, $"Exception in {nameof(UpdateDroneLocation)} for delivery Id : {deliveryId} . Exception Message : {ex.Message}");
            }
        }

        private static HttpClient client = null;

        public HttpClient Client
        {
            get
            {
                if (client == null)
                {
                    var httpClientHandler = new HttpClientHandler();
                    httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
                    client = new HttpClient(httpClientHandler);
                }
                return client;
            }
        }
    }
}
