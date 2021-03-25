using System.Threading.Tasks;

namespace Anabasis.EventStore.Queue
{
  public interface IDispatchQueue<TMessage>
  {
    void Enqueue(TMessage message);
  }
}
