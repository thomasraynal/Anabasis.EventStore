namespace Anabasis.Common.Infrastructure
{
  public interface ICommand: IStreamable
  {
    string CallerId { get;  }
  }
}
