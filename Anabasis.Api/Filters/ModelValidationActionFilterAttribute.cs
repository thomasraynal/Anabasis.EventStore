using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Anabasis.Api.Filters
{
    public class ModelValidationActionFilterAttribute : ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.ModelState.IsValid)
            {
                var errors =
                    context.ModelState.Select(
                        modelState => new UserErrorMessage(
                            "BadRequest",
                             modelState.Value.Errors.Select(e => e.ErrorMessage).Aggregate((s1, s2) => $"{s1}{Environment.NewLine}{s2}"),
                            new Dictionary<string, object>()
                            {
                                { modelState.Key, modelState.Value?.RawValue?.ToString() ?? ""}
                            },
                            null)
                            )
                            .ToArray();

                var errorResponseMessage = new ErrorResponseMessage(errors);

                context.Result = new ContentResult
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Content = JsonConvert.SerializeObject(errorResponseMessage),
                    ContentType = "application/json",
                };

                return;
            }
            else
            {
                await next();
            }
        }
    }
}
