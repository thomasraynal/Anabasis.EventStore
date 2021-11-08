﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;

namespace Anabasis.Api.Filters
{
    public class ExceptionActionFilterAttribute : ExceptionFilterAttribute
    {
        private readonly Dictionary<Type, HttpStatusCode> _defaultErrors;
        private readonly AppContext _appContext;

        public ExceptionActionFilterAttribute(AppContext appContext)
        {
            _defaultErrors = new Dictionary<Type, HttpStatusCode>
                {
                    {typeof(ArgumentException), HttpStatusCode.InternalServerError},
                    {typeof(ValidationException), HttpStatusCode.BadRequest},
                    {typeof(JsonSerializationException), HttpStatusCode.BadRequest},
                };

            _appContext = appContext;
        }

        public override void OnException(ExceptionContext context)
        {
            var response = context.HttpContext.Response;
            var request = context.HttpContext.Request;
            var exception = context.Exception;

            var correlationId = context.HttpContext.GetCorrelationId();
            var requestId = context.HttpContext.GetRequestId();
            var url = UriHelper.GetDisplayUrl(context.HttpContext.Request);

            exception.SetData(ExceptionData.MachineName, _appContext.MachineName);
            exception.SetData(ExceptionData.CorrelationId, correlationId);
            exception.SetData(ExceptionData.RequestId, requestId);
            exception.SetData(ExceptionData.HttpMethod, context.HttpContext.Request.Method);
            exception.SetData(ExceptionData.CalledUrl, url);

            if (request.ContentLength.HasValue)
            {
                // CurrentHttpRequest.Set(request);
                // _logger.LogException(exception);
                // CurrentHttpRequest.Clear();
            }
            else
            {
               // _logger.LogException(exception);
            }

            var exType = exception.GetType();
            var actionName = context.ActionDescriptor.GetActionName();

            if (_defaultErrors.TryGetValue(exType, out var status))
            {
                context.Result = GetErrorResult(exception, status, actionName);
                return;
            }

            context.Result = GetErrorResult(exception, HttpStatusCode.InternalServerError, actionName);
            return;
        }

        private ErrorResponseMessageActionResult GetErrorResult(Exception exception, HttpStatusCode statusCode, string actionName)
        {

            string docUrl = null;

            if (actionName != null)
            {
                docUrl = actionName;
            }

            return new ErrorResponseMessageActionResult(

                new ErrorResponseMessage(new[] { new UserErrorMessage(
                    exception.GetType().FullName,
                    exception.Message,
                    string.IsNullOrWhiteSpace(CultureInfo.InvariantCulture.Name) ? null : CultureInfo.InvariantCulture.Name,
                    null,
                    null,
                    exception.StackTrace?.ToString()
                    )}),

                (int)statusCode
            );
        }
    }
}
