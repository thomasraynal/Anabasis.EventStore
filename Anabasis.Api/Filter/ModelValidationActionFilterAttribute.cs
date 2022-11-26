using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Anabasis.Common;
using Anabasis.Api.Middleware;

namespace Anabasis.Api.Filters
{
    public class ModelValidationActionFilterAttribute : ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState.Select(modelState =>
                    {
                        var errorMessage = string.Join(Environment.NewLine, modelState.Value.Errors.Select(modelError => modelError.ErrorMessage));

                        return new UserErrorMessage(
                                 HttpStatusCode.BadRequest,
                                 errorMessage,
                                 new Dictionary<string, object>()
                                 {
                                    { modelState.Key, modelState.Value?.RawValue?.ToString() ?? ""}
                                 });

                    }).ToArray();

                var errorResponseMessage = new ErrorResponseMessage(errors);

                context.Result = new ContentResult
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Content = errorResponseMessage.ToJson(),
                    ContentType = "application/json; charset=utf-8"
                };

              //  context.HttpContext.Items[HttpErrorFormattingMiddleware.IsFormatted] = true;

                return;
            }
            else
            {
                await next();
            }
        }
    }
}
