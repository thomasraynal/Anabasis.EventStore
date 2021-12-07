using Anabasis.Common;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Api
{
    public class ErrorResponseMessageActionResult : IActionResult
    {
        private readonly ErrorResponseMessage _errorResponseMessage;
        private readonly HttpStatusCode _httpStatusCode;

        public ErrorResponseMessageActionResult(ErrorResponseMessage errorResponseMessage, HttpStatusCode httpStatusCode)
        {
            _errorResponseMessage = errorResponseMessage;
            _httpStatusCode = httpStatusCode;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            var response = context.HttpContext.Response;
            response.StatusCode = (int)_httpStatusCode;
            response.ContentType = "application/json";

            var bodyBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_errorResponseMessage, Json.GetDefaultJsonSerializerSettings()));
            await response.Body.WriteAsync(bodyBytes.AsMemory(0, bodyBytes.Length));
        }
    }
}
