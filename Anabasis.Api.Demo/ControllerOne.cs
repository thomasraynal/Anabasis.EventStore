using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Api.Demo
{
    [ApiController]
    [Route("event")]
    public class ControllerOne: Controller
    {
        [HttpPut]
        public IActionResult CreateEvent()
        {
            return Ok(_marketDataSink.CurrentPrices.Items);
        }

    }
}
