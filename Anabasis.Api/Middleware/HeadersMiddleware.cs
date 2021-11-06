using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.Api.Middleware
{
    public class HeadersMiddleware
    {
        public const string DEFAULT_CULTURE = "en";

        private readonly RequestDelegate _next;
        private readonly ApplicationName _applicationName;
        private readonly BeezUPAppContext _appContext;

        public HeadersMiddleware(RequestDelegate next, AppContext appContext)
        {
            _next = next;
            _applicationName = appContext.ApplicationName;
            _appContext = appContext;
        }

        public const string BeezUPUserUserIdPropertyName = "BeezUPUserId";
        public const string BeezUPUserUserEmailPropertyName = "BeezUPUserEmail";
        public const string BeezUPUserGroupNamesPropertyName = "BeezUPUserGroupNames";

        public async Task Invoke(HttpContext context)
        {
            var request = context.Request;

            var requestId = (
                request.Headers.TryGetValue(WebConstants.REQUEST_ID_HTTP_HEADER, out var values)
                && values.Count != 0
                && Guid.TryParse(values[0], out var requestIdFromHeader)
                )
                ? requestIdFromHeader
                : GuiDate.NewGuid()
                ;
            
            var correlationId = (
                request.Headers.TryGetValue(WebConstants.CORRELATION_ID_HTTP_HEADER, out var values2)
                && values2.Count != 0
                && Guid.TryParse(values2[0], out var correlationIdFromHeader)
                )
                ? correlationIdFromHeader
                : requestId
                ;

            request.HttpContext.Items[WebConstants.REQUEST_ID_HTTP_HEADER] = requestId;
            request.HttpContext.Items[WebConstants.CORRELATION_ID_HTTP_HEADER] = correlationId;

            // USER ID/CONTEXT
            var userId = GetUserId(request);
            var userEmail = GetUserEmail(request);
            var userGroupNames = GetBeezUPUserGroupNames(request);

            var cultureFromRequest = request.GetTypedHeaders().AcceptLanguage?.FirstOrDefault()?.Value.Value;
            var cultureName = string.IsNullOrWhiteSpace(cultureFromRequest)
                    ? DEFAULT_CULTURE
                    : cultureFromRequest;

            var userContext = new BeezUPUserContext(
                userId != Guid.Empty ? new UserId(userId) : null,
                userEmail != null ? new Email(userEmail) : null,
                new CultureName(cultureName),
                userGroupNames
                );

            request.SetBeezUPUserContext(userContext);

            var response = context.Response;
            response.Headers[WebConstants.BEEZUP_APP_NAME_HTTP_HEADER] = _applicationName.FullName;

            response.OnStarting(() =>
            {
                var activeSpan = _appContext.Tracer.ActiveSpan;
                if (activeSpan != null)
                {
                    response.SetTraceId(activeSpan.Context);
                }

                if (response.StatusCode != (int)HttpStatusCode.NotModified && response.StatusCode != (int)HttpStatusCode.PreconditionFailed)
                {
                    response.SetRequestId(requestId);
                }

                return Task.CompletedTask;
            });

            await _next(context).CAF();
        }

        #region UserContext
        internal static string GetUserEmail(HttpRequest request)
        {
            if (request.Headers.TryGetValue(BeezUPUserUserEmailPropertyName, out var values))
            {
                return values.FirstOrDefault(w => !string.IsNullOrWhiteSpace(w));
            }

            if (request.IsLocal())
                return DefaultLocalUserEmail;

            return null;
        }

        // "A8E032C1-31E3-4048-A32C-C3FCA9734793" => // Jérôme Rouaix
        public static readonly Guid DefaultLocalUserGuid = Guid.Parse("33AE754C-D30C-4BCB-9F5C-0048580AABA9"); // BeezUP TEST
        public static readonly string DefaultLocalUserEmail = "test@beezup.com"; // BeezUP TEST
        public static readonly string DefaultLocalUserGroupName = "BeezUP Users"; // BeezUP TEST

        internal static Guid GetUserId(HttpRequest request)
        {
            if (request.Headers.TryGetValue(BeezUPUserUserIdPropertyName, out var values))
            {
                if (values.Count != 0 && !string.IsNullOrWhiteSpace(values[0]))
                {
                    if (Guid.TryParse(values[0], out Guid userId))
                        return userId;
                }
            }

            if (request.IsLocal())
                return DefaultLocalUserGuid;

            return Guid.Empty;
        }

        internal static string[] GetBeezUPUserGroupNames(HttpRequest request)
        {
            if (request.Headers.TryGetValue(BeezUPUserGroupNamesPropertyName, out var values))
            {
                if (values.Count != 0 && !string.IsNullOrWhiteSpace(values[0]))
                {
                    return values[0].Split(',');
                }
            }

            if (request.IsLocal())
                return new[] { DefaultLocalUserGroupName };

            return new string[0];
        }
        #endregion
    }

    #region ExtensionMethod
    public static class BeezUPHeadersAndErrorHandlingMiddlewareExtension
    {
        public static IApplicationBuilder WithBeezUPMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<BeezUPHeadersMiddleware>();
            return app;
        }
    }
    #endregion
}
