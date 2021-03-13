namespace Anabasis.Common
{
  public interface IAnabasisDocument
  {
    string Author { get; set; }
    string Content { get; set; }
    string Id { get; set; }
    string Tag { get; set; }
    string Type { get; set; }
  }
}
