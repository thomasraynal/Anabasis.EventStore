using System;

namespace Anabasis.Importer
{
  public class Export : IDisposable
  {
    public ExportFile Documents { get; }
    public ExportFile Indices { get; }
    public string[] ExpectedDocumentIds { get; }
    public int ImportedDocumentCount { get; set; }
    public int ImportedIndicesCount { get; set; }

    public bool IsDone { get; set; }

    public Export(string documentPath, string indicesPath, string[] documentIds)
    {
      Documents = new ExportFile(documentPath);
      Indices = new ExportFile(indicesPath);
      ExpectedDocumentIds = documentIds;
    }

    public void Dispose()
    {
      Documents.Dispose();
      Indices.Dispose();
    }
  }
}
