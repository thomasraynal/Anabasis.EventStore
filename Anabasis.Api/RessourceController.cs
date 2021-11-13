using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;

namespace Anabasis.Api
{
    public class BoomOperationException : Exception, ICanMapToHttpError
    {
        public BoomOperationException()
        {
        }

        public BoomOperationException(string message) : base(message)
        {
        }

        public BoomOperationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected BoomOperationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public HttpStatusCode HttpStatusCode => HttpStatusCode.InternalServerError;

    }

    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/data")]
    public class RessourceController : BaseController
    {
        private readonly IDataService _dataService;
        private readonly IOptions<SomeOtherConfigurationOptions> _someOtherConfigurationOptions;

        public RessourceController(IDataService dataService, IOptions<SomeOtherConfigurationOptions> someOtherConfigurationOptions)
        {
            _dataService = dataService;
            _someOtherConfigurationOptions = someOtherConfigurationOptions;
        }

        [HttpGet]
        [SwaggerResponse(500, type: typeof(ErrorResponseMessage), Description = "Occurs when something goes wrong")]
        [SwaggerResponse(200, type: typeof(Ressource), Description = "Specified ressource")]
        [ResponseCache(NoStore = false, Duration = 60, Location = ResponseCacheLocation.Any)]
        public Ressource Get()
        {
            return _dataService.GetData();
        }

        [HttpGet("byId/{ressourceId}", Name = "GetById")]
        [SwaggerResponse(500, type: typeof(ErrorResponseMessage), Description = "Occurs when something goes wrong")]
        [SwaggerResponse(404, type: typeof(ErrorResponseMessage), Description = "Specified ressourceId not found")]
        [SwaggerResponse(200, type: typeof(RessourceObject), Description = "Specified ressourceId object")]
        [SwaggerOperation("GetById", "Get a ressource by id")]
        [Produces("application/json")]
        public IActionResult GetById([Required] string ressourceId)
        {
            var ressourceObject = _dataService.GetData().RessourceObjects.FirstOrDefault(obj => obj.Id == ressourceId);

            if (null == ressourceObject) return NotFound(ressourceId);

            return Ok(ressourceObject);
        }

        [HttpGet("byName/{ressourceName}", Name = "GetByName")]
        [SwaggerResponse(500, type: typeof(ErrorResponseMessage), Description = "Occurs when something goes wrong")]
        [SwaggerResponse(404, type: typeof(ErrorResponseMessage), Description = "Specified ressourceName not found")]
        [SwaggerResponse(200, type: typeof(RessourceObject), Description = "Specified ressourceName object")]
        [SwaggerOperation("GetByName", "Get a ressource by name")]
        [Produces("application/json")]
        public IActionResult GetByName([Required] string ressourceName)
        {
            throw new BoomOperationException("boom");

            var ressourceObject = _dataService.GetData().RessourceObjects.FirstOrDefault(obj => obj.Name == ressourceName);

            if (null == ressourceObject) return NotFound(ressourceName);

            return Ok(ressourceObject);
        }

    }
}
