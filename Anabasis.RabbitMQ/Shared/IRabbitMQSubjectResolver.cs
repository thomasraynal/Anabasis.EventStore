using System.Linq.Expressions;

namespace Anabasis.RabbitMQ.Shared
{
    public interface IRabbitMQSubjectResolver
    {
        string GetSubject();
  
    }
}