using Anabasis.Insights;
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
        private readonly IBusOne _busOne;
        private readonly ITracer _tracer;

        public ControllerOne(IBusOne busOne, ITracer tracer)
        {
            _busOne = busOne;
            _tracer = tracer;
        }

        [HttpPut]
        public IActionResult CreateEvent()
        {
            var eventCreated = new EventCreated("nothing", traceId: Guid.NewGuid());

            using (var mainSpan = _tracer.StartActiveSpan("ControllerOne", traceId: eventCreated.TraceId.Value, startTime: DateTime.UtcNow))
            {

                mainSpan.AddEvent("CreateEventStart");

                _busOne.Push(eventCreated);

                mainSpan.AddEvent("CreateEventEnd");
            }
          
            return Ok();
        }

    }
}
