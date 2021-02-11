using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DroneSimulator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SimulatorController : ControllerBase
    {
        ILogger<SimulatorController> _logger;
        FlightEngine _flightEngine;
        private TelemetryClient _telemetry;
        public SimulatorController(ILogger<SimulatorController> logger, FlightEngine flightEngine, TelemetryClient telemetry)
        {
            _logger = logger;
            _flightEngine = flightEngine;
            _telemetry = telemetry;
        }

        [HttpPut]
        [ActionName("StartTracking")]
        public IActionResult StartTracking(string deliveryId)
        {
            try
            {
                _telemetry.TrackEvent($"SimulationReceived");
                _ = Task.Run(async () =>
                {
                    try
                    {
                        //var flightEngine = new FlightEngine(new DeliveryApi(trackingUrl));
                        _telemetry.TrackEvent($"SimulationStarted");
                        _logger.LogInformation("Request Received for delivery Id" + deliveryId);
                        await _flightEngine.ExecuteDroneDelivery(deliveryId);
                        _logger.LogInformation("Request Processed for delivery Id" + deliveryId);
                        _telemetry.TrackEvent($"SimulationEnded");
                    }
                    catch (AggregateException ex)
                    {
                        _logger.LogError(ex.Message);
                        _telemetry.TrackEvent($"SimulationException");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                        _telemetry.TrackEvent($"SimulationException");
                    }
                });
            }
            catch (AggregateException ex)
            {
                _logger.LogError(ex.Message);
                _telemetry.TrackEvent($"SimulationException");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _telemetry.TrackEvent($"SimulationException");
            }

            return Ok();
        }
    }
}
