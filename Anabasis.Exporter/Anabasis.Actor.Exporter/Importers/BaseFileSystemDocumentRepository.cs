using Anabasis.Actor;
using Anabasis.Common;
using Anabasis.Common.Events;
using Anabasis.Common.Events.Commands;
using Anabasis.EventStore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Anabasis.Importer
{

  public abstract class BaseFileSystemDocumentRepository : BaseDocumentRepository<FileSystemDocumentRepositoryConfiguration>
  {

    private readonly Dictionary<Guid, Export> _exports;
    private readonly Dictionary<Guid, ICommand> _exportCallers;

    private readonly object _syncLock = new object();

    public BaseFileSystemDocumentRepository(FileSystemDocumentRepositoryConfiguration configuration, IEventStoreRepository eventStoreRepository) : base(configuration, eventStoreRepository)
    {
      _exports = new Dictionary<Guid, Export>();
      _exportCallers = new Dictionary<Guid, ICommand>();
    }

    public abstract bool UseIndex { get; }

    public override Task Handle(RunExportCommand startExportRequest)
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

        if (export.ImportedDocumentCount != export.ExpectedDocumentIds.Length) return;

        if (UseIndex && export.ImportedIndicesCount != export.ExpectedDocumentIds.Length) return;

        export.IsDone = true;

        export.Documents.EndWriting();
        export.Indices.EndWriting();

        export.Dispose();

        var exportCommand = _exportCallers[exportEnded.CorrelationID];

        Emit(new RunExportCommandResponse(exportCommand.EventID, exportEnded.CorrelationID, exportEnded.StreamId)).Wait();

      }
    }

    public async override Task Handle(DocumentCreated documentCreated)
    {
      var export = _exports[documentCreated.ExportId];

      if (documentCreated.DocumentUri.IsFile)
      {
        export.Documents.Append(File.ReadAllText(documentCreated.DocumentUri.AbsolutePath));
      }
      else
      {
        throw new InvalidOperationException("not a file");
      }

      await Emit(new DocumentImported(documentCreated.DocumentId, documentCreated.CorrelationID, documentCreated.StreamId));

      TryCompleteExport(documentCreated, false);

    }

    public async override Task Handle(IndexCreated indexCreated)
    {
      var export = _exports[indexCreated.ExportId];

      if (indexCreated.AnabasisDocumentIndexUri.IsFile)
      {
        export.Indices.Append(File.ReadAllText(indexCreated.AnabasisDocumentIndexUri.AbsolutePath));
      }
      else
      {
        throw new InvalidOperationException("not a file");
      }

      await Emit(new IndexImported(indexCreated.DocumentId, indexCreated.CorrelationID, indexCreated.StreamId));

      TryCompleteExport(indexCreated, true);

    }
  }
}
