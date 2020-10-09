using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Exporter
{
  public interface IDocumentService
  {
    Task ExportFolder();

    Task<Document[]> GetDocuments(bool fetchDocumentItems = false);
  
    Task<DocumentItem[]> GetMainTitlesByDocumentId(string documentId);
    Task<DocumentItem[]> GetSecondaryTitlesByMainTitleId(string mainTitle);
    Task<DocumentItem[]> GetDocumentItemsByMainTitleId(string mainTitleId);
    Task<DocumentItem[]> GetDocumentItemsByMainSecondaryTitleId(string secondaryTitleId);
  }

}
