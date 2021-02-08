using Anabasis.Common.Infrastructure;
using System.Threading.Tasks;

namespace Anabasis.Common.Actor
{
  public interface IActor
  {
    string StreamId { get; }
    Task OnMessageReceived(Message message);
  }
}
