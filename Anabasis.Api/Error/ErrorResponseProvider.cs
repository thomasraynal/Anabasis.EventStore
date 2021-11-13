using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Anabasis.Api.ErrorManagement
{
    public class ErrorResponseProvider : IErrorResponseProvider
    {
        public IActionResult CreateResponse(ErrorResponseContext context)
        {
            return new ErrorResponseMessageActionResult(
              new ErrorResponseMessage(new[] { new UserErrorMessage(context.ErrorCode, context.Message) }), HttpStatusCode.BadRequest);
        }
    }
}
