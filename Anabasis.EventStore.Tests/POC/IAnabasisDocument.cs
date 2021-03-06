namespace Anabasis.EventStore.Tests.POC
{
  public interface IAnabasisDocument
  {
    string Author { get; set; }
    string Content { get; set; }
    string Id { get; set; }
    bool IsRootDocument { get; }
    string ParentId { get; set; }
    string Tag { get; set; }
  }
}