﻿using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Anabasis.Api.Filters
{

    //https://stackoverflow.com/questions/19278759/web-api-required-parameter/19322688#19322688
    public class RequiredParametersActionFilterAttribute : ActionFilterAttribute
    {

        private readonly ConcurrentDictionary<Tuple<string, string>, List<string>> _cache = new();

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var requiredParameters = this.GetRequiredParameters(context);

            if (ValidateParameters(context, requiredParameters, out var hasNullParameter))
            {
                await next();
            }
            else
            {
                var errorResponseMessage = new ErrorResponseMessage(
                    hasNullParameter.Select(
                        parameter =>
                            new UserErrorMessage(
                                "BadRequest",
                                $"The parameter '{parameter}' is required. Please check the specification.",
                                "en-US",
                                new Dictionary<string, object>()
                                {
                                    { "parameterName", parameter}
                                },
                               null)).ToArray());

                context.Result = new ErrorResponseMessageActionResult(errorResponseMessage,  (int)HttpStatusCode.BadRequest);
            }
        }

        private bool ValidateParameters(ActionExecutingContext actionContext, List<string> requiredParameters, out string[] hasNullParameter)
        {
            if (requiredParameters == null || requiredParameters.Count == 0)
            {
                hasNullParameter = Array.Empty<string>();
                return true;
            }

            hasNullParameter = requiredParameters.Where(r =>
                !actionContext
                 .ActionArguments
                 .Any(a => a.Key == r && a.Value != null)).ToArray();

            return hasNullParameter.Length == 0;
        }

        private List<string> GetRequiredParameters(ActionExecutingContext actionContext)
        {
            var httpContext = actionContext.HttpContext;
            var request = httpContext.Request;

            var requestKey = new Tuple<string, string>(request.Method, request.Path.ToString());

            if (_cache.TryGetValue(requestKey, out var result)) return result;

            var parameters = (actionContext.ActionDescriptor as ControllerActionDescriptor)?.MethodInfo.GetParameters();
            if (parameters == null) result = new List<string>(0);
            else
            {
                result = parameters
                    .Where(p => p.GetCustomAttributes(true).Any(att => att is RequiredAttribute))
                    .Select(p => p.Name)
                    .ToList();
            }

            _cache.TryAdd(requestKey, result);

            return result;
        }

    }
}