using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.IO;

namespace Anabasis.Importer
{
  public abstract class BaseExportFile :  IExportFile
  {
    private StreamWriter _streamWriter;

    private bool _isDisposed;
    private readonly object _syncLock = new object();

    protected JsonTextWriter JsonTextWriter { get; private set; }

    public void StartWriting(string path)
    {
      lock (_syncLock)
      {
        if (_isDisposed) return;

        var directory = Path.GetDirectoryName(path);

        Directory.CreateDirectory(directory);

        _streamWriter = File.CreateText(path);
        JsonTextWriter = new JsonTextWriter(_streamWriter);

        JsonTextWriter.WriteStartArray();
      }

    }

    public void EndWriting()
    {
      lock (_syncLock)
      {
        if (_isDisposed) return;
        JsonTextWriter.WriteEndArray();

      }
    }

    public abstract void Append(string item);

    public void Dispose()
    {
      lock (_syncLock)
      {

        if (_isDisposed) return;

        _isDisposed = true;

        _streamWriter.Dispose();
        JsonTextWriter.Close();

      }


    }
  }

}
