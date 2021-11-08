using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Api
{
    public class ErrorResponseMessageActionResult : IActionResult
    {
        private readonly ErrorResponseMessage _errorResponseMessage;
        private readonly int _status;

        public ErrorResponseMessageActionResult(ErrorResponseMessage errorResponseMessage, int status)
        {
            _errorResponseMessage = errorResponseMessage;
            _status = status;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            var response = context.HttpContext.Response;
            response.StatusCode = _status;
            response.ContentType = "application/json";

            var bodyBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_errorResponseMessage));
            await response.Body.WriteAsync(bodyBytes.AsMemory(0, bodyBytes.Length));
        }
    }
}
