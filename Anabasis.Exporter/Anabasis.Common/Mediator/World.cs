using Lamar;

namespace Anabasis.Common.Mediator
{
  public static class World
  {
    public static IMediator Create<TRegistry>()
      where TRegistry : ServiceRegistry, new()
    {
      var container = new Container(configuration =>
      {
        configuration.IncludeRegistry<TRegistry>();
      });

      var simpleMediator = new SimpleMediator(container);

      return simpleMediator;

    }
  }
}
