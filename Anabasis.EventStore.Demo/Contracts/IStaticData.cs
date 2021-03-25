
namespace Anabasis.EventStore.Demo
{
  public interface IStaticData
  {
    CurrencyPair[] CurrencyPairs { get; }
    string[] Customers { get; }
  }
}
