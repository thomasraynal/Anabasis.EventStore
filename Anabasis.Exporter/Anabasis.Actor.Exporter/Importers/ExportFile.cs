using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;

namespace Anabasis.Importer
{
  public class ExportFile : IDisposable
  {
    private readonly StreamWriter _streamWriter;
    private readonly JsonTextWriter _jsonTextWriter;
    private readonly JsonSerializer _jsonSerializer;

    private bool _isDisposed;

    private readonly object _syncLock = new object();

    public ExportFile(string path)
    {
      var directory = Path.GetDirectoryName(path);

      Directory.CreateDirectory(directory);

      _streamWriter = File.CreateText(path);
      _jsonTextWriter = new JsonTextWriter(_streamWriter);

      _jsonSerializer = new JsonSerializer()
      {
        ContractResolver = new DefaultContractResolver
        {
          NamingStrategy = new CamelCaseNamingStrategy()
        },

        Formatting = Formatting.Indented

      };
    }

    public void StartWriting()
    {
      lock (_syncLock)
      {
        if (_isDisposed) return;
        _jsonTextWriter.WriteStartArray();
      }

    }

    public void EndWriting()
    {
      lock (_syncLock)
      {
        if (_isDisposed) return;
        _jsonTextWriter.WriteEndArray();

      }
    }

    public void Append(string item)
    {
      var jObject = JObject.Parse(item);

      jObject.WriteTo(_jsonTextWriter);
    }

    public void Dispose()
    {
      lock (_syncLock)
      {

        if (_isDisposed) return;

        _isDisposed = true;

        _streamWriter.Dispose();
        _jsonTextWriter.Close();

      }


    }
  }

}
