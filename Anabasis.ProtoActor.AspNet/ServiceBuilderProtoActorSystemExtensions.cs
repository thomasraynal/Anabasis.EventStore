using Anabasis.ProtoActor.System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Anabasis.ProtoActor.AspNet
{
    //https://proto.actor/docs/logging/
    //https://proto.actor/docs/health-checks/
    //https://proto.actor/docs/metrics/
    public static class ServiceBuilderProtoActorSystemExtensions
    {
        private static ProtoActorSystemBuilder? _protoActorSystemBuilder = null;

        public static ProtoActorSystemBuilder AddProtoActorSystem(this IServiceCollection serviceCollection)
        {
            if(null != _protoActorSystemBuilder)
            {
                throw new InvalidOperationException("ProtoActorSystem alreday exists");
            }

            serviceCollection.AddSingleton<IProtoActorSystem, ProtoActorSystem>();

            _protoActorSystemBuilder = new ProtoActorSystemBuilder(serviceCollection);

            return _protoActorSystemBuilder;
        }

        public static IApplicationBuilder UseProtoActorSystem(this IApplicationBuilder applicationBuilder)
        {
            if(null == _protoActorSystemBuilder)
            {
                throw new InvalidOperationException("No ProtoActorSystem defined - did you AddProtoActorSystem() ?");
            }

            _protoActorSystemBuilder.Create(applicationBuilder);


            return applicationBuilder;
        }

    }
}