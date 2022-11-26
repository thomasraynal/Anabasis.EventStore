using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Anabasis.Common;

namespace Anabasis.Api.Shared
{
    public static class ControllerReponseExtensions
    {
        public static ObjectResult WithErrorFormatting(this ObjectResult objectResult)
        {
            if(objectResult.StatusCode >= 300)
            {
      
                var errorResponseMessage = new ErrorResponseMessage(new[]
                        {
                        new UserErrorMessage((HttpStatusCode)objectResult.StatusCode,objectResult.Value )
                        });


                objectResult.Value = errorResponseMessage;

            }

            return objectResult;
        }
    }
}
