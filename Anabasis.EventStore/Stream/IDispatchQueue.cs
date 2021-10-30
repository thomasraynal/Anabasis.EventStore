using System.Threading.Tasks;

namespace Anabasis.EventStore.Stream
{
  public interface IDispatchStream<TMessage>
  {
    void Enstream(TMessage message);
  }
}
