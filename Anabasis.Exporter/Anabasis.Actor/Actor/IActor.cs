namespace Anabasis.Actor
{
  public interface IActor
  {
    string ActorId { get; }
    bool CanConsume(IActorEvent message);
  }
}
