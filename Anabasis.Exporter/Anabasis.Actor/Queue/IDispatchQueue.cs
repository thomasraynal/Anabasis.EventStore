using System.Threading.Tasks;

namespace Anabasis.Actor
{
  public interface IDispatchQueue<TMessage>
  {
    void Enqueue(TMessage message);
  }
}
