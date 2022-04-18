using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.RabbitMQ.Shared.SubjectResolver
{
    public interface ISubjectResolverFactory
    {
        IRabbitMQSubjectResolver GetRabbitMQSubjectResolver(Type actorType, Type eventType);
    }
}
