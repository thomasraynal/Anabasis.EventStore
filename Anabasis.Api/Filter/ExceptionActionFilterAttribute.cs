using Anabasis.Common;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace Anabasis.Api.Filters
{
    public class ExceptionActionFilterAttribute : ExceptionFilterAttribute
    {
        private readonly Dictionary<Type, HttpStatusCode> _defaultErrors;
        private readonly AnabasisAppContext _appContext;

        public ExceptionActionFilterAttribute(AnabasisAppContext appContext)
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

            var exception = context.Exception;

            var correlationId = context.HttpContext.GetCorrelationId();
            var requestId = context.HttpContext.GetRequestId();

            var url = UriHelper.GetDisplayUrl(context.HttpContext.Request);

            exception.SetData(ExceptionData.MachineName, _appContext.MachineName);
            exception.SetData(ExceptionData.CorrelationId, correlationId);
            exception.SetData(ExceptionData.RequestId, requestId);
            exception.SetData(ExceptionData.HttpMethod, context.HttpContext.Request.Method);
            exception.SetData(ExceptionData.CalledUrl, url);

            Log.Error(exception, string.Empty);
            
            var exceptionType = exception.GetType();
            var actionName = context.ActionDescriptor.GetActionName();

            if (_defaultErrors.TryGetValue(exceptionType, out var status))
            {
                context.Result = GetErrorResult(exception, status, actionName);
                return;
            }

            if (exception is ICanMapToHttpError)
            {
                var canMapToHttpError = exception as ICanMapToHttpError;

                context.Result = GetErrorResult(exception, canMapToHttpError.HttpStatusCode, actionName, canMapToHttpError.Message);

                return;
            }

            context.Result = GetErrorResult(exception, HttpStatusCode.InternalServerError, actionName);

            return;
        }

        private ErrorResponseMessageActionResult GetErrorResult(Exception exception, HttpStatusCode statusCode, string actionName, string customMessage = null)
        {

            var docUrl = actionName == null ? null : DocUrlHelper.GetDocUrl(actionName, _appContext.DocUrl);

            var userErrorMessages = new[] { new UserErrorMessage(
                    null,
                    customMessage ?? exception.Message,
                    docUrl: docUrl,
                    stackTrace: exception.StackTrace?.ToString()
                    )};

            return new ErrorResponseMessageActionResult(new ErrorResponseMessage(userErrorMessages), statusCode);
        }
    }
}
