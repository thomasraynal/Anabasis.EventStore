using Microsoft.AspNetCore.Mvc;
using System;

//Tracing
//HealthChecks => WithHealthCheck - use .net healthcheck extensions
    //print healthcheck to log => HosteedService/GenericHost
//Handlers
    //ExMessages
//preconditions management
//authentication
//HateOAS
//OpenTracing
//json - switch to ms json

namespace Anabasis.Api
{
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
    }
}
