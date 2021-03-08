namespace Anabasis.Common
{
  public interface IAnabasisDocumentItem
  {
    string Content { get; set; }
    string DocumentId { get; set; }
    string Id { get; set; }
    bool IsEmphasis { get; set; }
    bool IsMainTitle { get; set; }
    bool IsRootDocument { get; }
    bool IsSecondaryTitle { get; set; }
    bool IsUrl { get; set; }
    string MainTitleId { get; set; }
    string ParentId { get; set; }
    int Position { get; set; }
    string SecondaryTitleId { get; set; }

  }
}
