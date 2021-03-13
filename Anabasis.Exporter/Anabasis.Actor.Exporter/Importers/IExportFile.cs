using System;

namespace Anabasis.Importer
{
  public interface IExportFile: IDisposable
  {
    void Append(string item);
    void EndWriting();
    void StartWriting(string path);
  }
}
