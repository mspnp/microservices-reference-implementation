using Fabrikam.DroneDelivery.WebSite.Extentions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabrikam.DroneDelivery.WebSite.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConfigurationController : ControllerBase
    {
        private IConfiguration _configuration;

        public ConfigurationController(IConfiguration configuration)
        {
            this._configuration = configuration;
        }

        /// <summary>
        /// Returns the API Url
        /// </summary>
        /// <returns></returns>
        [HttpGet("apiUrl")]
        public string GetApiUrl()
        {
            return _configuration["ApiUrl"].TrimEndChar("/");
        }

        /// <summary>
        /// Returns Bing map key
        /// </summary>
        /// <returns></returns>
        [HttpGet("bingMapKey")]
        public string GetBingMapKey()
        {
            return _configuration["BingMapKey"];
        }
    }
}
