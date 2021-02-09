using System.Threading.Tasks;

namespace Anabasis.Common.Mediator
{
  public interface IDispatchQueue<TMessage>
  {
    void Enqueue(TMessage message);
  }
}
