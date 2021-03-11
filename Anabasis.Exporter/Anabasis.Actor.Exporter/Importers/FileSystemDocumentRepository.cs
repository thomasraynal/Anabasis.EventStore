using Anabasis.Actor;
using Anabasis.Common;
using Anabasis.Common.Events;
using Anabasis.Common.Events.Commands;
using Anabasis.EventStore;
using Anabasis.EventStore.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Anabasis.Importer
{
  public class ExportFile: IDisposable
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

    public void Append(object item)
    {
      var jObject = JObject.FromObject(item, _jsonSerializer);

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

  public class FileSystemDocumentRepository : BaseDocumentRepository<FileSystemDocumentRepositoryConfiguration>
  {

    private readonly Dictionary<Guid, Export> _exports;
    private readonly Dictionary<Guid, ICommand> _exportCallers;

    private readonly object _syncLock = new object();

    public FileSystemDocumentRepository(FileSystemDocumentRepositoryConfiguration configuration, IEventStoreRepository eventStoreRepository) : base(configuration, eventStoreRepository)
    {
      _exports = new Dictionary<Guid, Export>();
      _exportCallers = new Dictionary<Guid, ICommand>();
    }

    public override Task Handle(StartExportCommand startExportRequest)
    {
      _exportCallers.Add(startExportRequest.ExportId, startExportRequest);

      return Task.CompletedTask;
    }

    public override Task Handle(ExportStarted exportStarted)
    {
      var export = _exports[exportStarted.CorrelationID] = new Export(
        Path.Combine(Configuration.LocalDocumentFolder, exportStarted.StreamId, "export.json"),
        Path.Combine(Configuration.LocalDocumentFolder, exportStarted.StreamId, "index.json"),
        exportStarted.DocumentsIds);

      export.Documents.StartWriting();
      export.Indices.StartWriting();

      return Task.CompletedTask;
    }

    private void TryCompleteExport(IAnabasisExporterEvent exportEnded, bool isIndex)
    {
      lock (_syncLock)
      {

        var export = _exports[exportEnded.CorrelationID];

        if (export.IsDone) return;

        if (isIndex) export.ImportedIndicesCount++;
        else
        {
          export.ImportedDocumentCount++;
        }

        if (export.ImportedDocumentCount == export.ExpectedDocumentIds.Length &&
          export.ImportedIndicesCount == export.ExpectedDocumentIds.Length)
        {

          export.IsDone = true;

          try
          {

            export.Documents.EndWriting();
            export.Indices.EndWriting();
          }
          catch (Exception ex)
          {

          }

          export.Dispose();

          var exportCommand = _exportCallers[exportEnded.CorrelationID];

          Emit(new StartExportCommandResponse(exportCommand.EventID, exportEnded.CorrelationID, exportEnded.StreamId, exportEnded.TopicId)).Wait();
        }

      }
    }

    public async override Task Handle(DocumentCreated documentCreated)
    {
      var export = _exports[documentCreated.ExportId];

      AnabasisDocument anabasisDocument = null;

      if (documentCreated.DocumentUri.IsFile)
      {
        anabasisDocument = JsonConvert.DeserializeObject<AnabasisDocument>(File.ReadAllText(documentCreated.DocumentUri.AbsolutePath));
      }

      //work out double deserialization
      export.Documents.Append(anabasisDocument);


      await Emit(new DocumentImported(anabasisDocument.Id, documentCreated.CorrelationID, documentCreated.StreamId, documentCreated.TopicId));

      TryCompleteExport(documentCreated, false);

    }

    public async override Task Handle(IndexCreated indexCreated)
    {
      var export = _exports[indexCreated.ExportId];

      AnabasisDocumentIndex anabasisDocumentIndex = null;

      if (indexCreated.AnabasisDocumentIndexUri.IsFile)
      {
        anabasisDocumentIndex = JsonConvert.DeserializeObject<AnabasisDocumentIndex>(File.ReadAllText(indexCreated.AnabasisDocumentIndexUri.AbsolutePath));
      }
      //work out double deserialization
      export.Indices.Append(anabasisDocumentIndex);



      await Emit(new IndexImported(indexCreated.DocumentId, indexCreated.CorrelationID, indexCreated.StreamId, indexCreated.TopicId));

      TryCompleteExport(indexCreated, true);

    }
  }
}
