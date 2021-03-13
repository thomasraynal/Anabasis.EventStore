using System;

namespace Anabasis.Importer
{
  public class Export<TExportFile> : IDisposable
        where TExportFile : IExportFile, new()
  {
    public TExportFile Documents { get; }
    public TExportFile Indices { get; }
    public string[] ExpectedDocumentIds { get; }
    public int ImportedDocumentCount { get; set; }
    public int ImportedIndicesCount { get; set; }

    public bool IsDone { get; set; }

    public Export( string[] documentIds)
    {
      Documents = new TExportFile();
      Indices = new TExportFile();
      ExpectedDocumentIds = documentIds;
    }

    public void Dispose()
    {
      Documents.Dispose();
      Indices.Dispose();
    }
  }
}
