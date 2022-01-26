using System.Threading.Tasks;

namespace Anabasis.Common
{
    public interface IDispatchQueue<TMessage>
    {
        bool CanEnqueue();
        void Enqueue(TMessage message);
    }
}
