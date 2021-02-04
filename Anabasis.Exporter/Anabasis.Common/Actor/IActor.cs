using Anabasis.Common.Infrastructure;
using System.Threading.Tasks;

namespace Anabasis.Common.Actor
{
  public interface IActor: IEventEmitter, IEventHandler
  {
    string StreamId { get; }
  }
}
