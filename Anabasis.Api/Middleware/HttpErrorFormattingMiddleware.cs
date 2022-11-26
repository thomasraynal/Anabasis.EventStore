//using Microsoft.AspNetCore.Http;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Net;
//using System.Text;
//using System.Threading.Tasks;
//using Anabasis.Common;

//namespace Anabasis.Api.Middleware
//{
//    public class HttpErrorFormattingMiddleware
//    {
//        private readonly RequestDelegate _next;

//        public const string IsFormatted = "isFormatted";

//        public HttpErrorFormattingMiddleware(RequestDelegate next)
//        {
//            _next = next;
//        }

//        public async Task Invoke(HttpContext context)
//        {

//            context.Response.OnStarting(async () =>
//            {

//                var originalBody = context.Response.Body;
//                using var newBody = new MemoryStream();
//                context.Response.Body = newBody;

//                if (context.Response.StatusCode >= 300)
//                {
//                    if (context.Items.ContainsKey(IsFormatted))
//                    {
//                        return;
//                    }
//                    try
//                    {
//                        newBody.Seek(0, SeekOrigin.Begin);
//                        var bodyText = await new StreamReader(context.Response.Body).ReadToEndAsync();
                     
//                        newBody.Seek(0, SeekOrigin.Begin);
//                        await newBody.CopyToAsync(originalBody);


//                        context.Response.Body.Seek(0, SeekOrigin.Begin);
//                        var reader = new StreamReader(context.Response.Body);
//                        var errorMessage = reader.ReadToEnd();
                      
//                        var errorResponseMessage = new ErrorResponseMessage(new[]
//                        {
//                        new UserErrorMessage((HttpStatusCode)context.Response.StatusCode, errorMessage)
//                        });

//                       await context.Response.WriteAsync(errorResponseMessage.ToJson(), Encoding.UTF8);

//                        context.Items[IsFormatted] = true;

//                    }
//                    catch (Exception ex)
//                    {

//                    }

//                }
//            });

//            await _next(context);
//        }

//    }
//}
