using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Anabasis.RabbitMQ.Shared
{
    public class DirectExchangeRabbitMQSubjectResolver : IRabbitMQSubjectResolver
    {
        private readonly Type _declaringType;

        public DirectExchangeRabbitMQSubjectResolver(Type declaringType)
        {
            _declaringType = declaringType;
        }

        public string GetSubject()
        {
            return _declaringType.Name;
        }

        public Expression Visit(Expression expr)
        {
            throw new NotImplementedException();
        }
    }
}
