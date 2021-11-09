using Microsoft.AspNetCore.Mvc;
using System;

//Logging
    //Multiple Logger => Serilog 
//Tracing
//Configuration
//HealthChecks => WithHealthCheck - use .net healthcheck extensions
    //print healthcheck to log => HosteedService/GenericHost
//Handlers
    //ErrorLogger
    //ExMessages
    //Required parameters
    //Model validation
//preconditions management
//authentication
//HateOAS
//OpenTracing
//internal short term cache
    
namespace Anabasis.Api
{
    [ApiController]
    public abstract class BaseController : ControllerBase
    {

    }
}
