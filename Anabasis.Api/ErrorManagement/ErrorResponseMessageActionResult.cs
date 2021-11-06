using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
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

            var bodyBytes = _errorResponseMessage.ToJsonToBytes();
            await response.Body.WriteAsync(bodyBytes, 0, bodyBytes.Length).CAF();
        }
    }
}
