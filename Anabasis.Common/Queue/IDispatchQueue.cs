using System.Threading.Tasks;

namespace Anabasis.Common
{
  public interface IDispatchQueue<TMessage>
  {
    void Enqueue(TMessage message);
  }
}
