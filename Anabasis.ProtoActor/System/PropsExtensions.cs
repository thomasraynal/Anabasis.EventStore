using Microsoft.Extensions.Logging;
using Polly;
using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.System
{
    public static class PropsExtensions
    {
        public static Props WithExceptionHandler(this Props props, ILogger? logger = null)
        {

            return props.WithReceiverMiddleware(next => async (context, envelop) =>
                      {
                          try
                          {
                              await next(context, envelop);
                          }
                          catch (Exception ex)
                          {
                              logger?.LogError(ex, "An error occured while receiving a message");
                          }

                      }).WithSenderMiddleware(next => async (context, target, envelop) =>
                      {
                          try
                          {
                              await next(context, target, envelop);
                          }
                          catch (Exception ex)
                          {
                              logger?.LogError(ex, "An error occured while sending a message");
                          }

                      });
        }
    }
}
