
using System;

namespace Anabasis.Common
{
  public abstract class BaseAnabasisDocument : IAnabasisDocument
  {
    protected BaseAnabasisDocument()
    {
      Type = GetType().Name;
    }

    protected BaseAnabasisDocument(string author, string content, string id, string parentId, string tag)
    {
      Author = author;
      Content = content;
      Id = id;
      ParentId = parentId;
      Tag = tag;
      Type = GetType().Name;
    }

    public string Author { get; set; }
    public string Content { get; set; }
    public string Id { get; set; }
    public bool IsRootDocument => ParentId == null;
    public string ParentId { get; set; }
    public string Tag { get; set; }
    public string Type { get; set; }
    public override bool Equals(object obj)
    {
      return obj is BaseAnabasisDocument document &&
             Author == document.Author &&
             Content == document.Content &&
             Id == document.Id &&
             IsRootDocument == document.IsRootDocument &&
             ParentId == document.ParentId &&
             Tag == document.Tag &&
             Type == document.Type;
    }

    public string Hash => Content == null ? Id.Md5() : Content.Md5();

    public override int GetHashCode()
    {
      return HashCode.Combine(Author, Content, Id, IsRootDocument, ParentId, Tag, Type);
    }
  }
}
