namespace Anabasis.Actor
{
  public interface IActor: IDispatchQueue<IActorEvent>
  {
    string ActorId { get; }
    string StreamId { get; }
    bool CanConsume(IActorEvent message);
  }
}
