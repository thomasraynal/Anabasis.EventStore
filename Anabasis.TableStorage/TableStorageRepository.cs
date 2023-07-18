using Azure.Data.Tables;
using Anabasis.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Threading;
using System.Runtime.CompilerServices;
using Azure;

namespace Anabasis.TableStorage
{
    public class TableStorageRepository : ITableStorageRepository
    {
        public const int TableStorageMaxBatchSize = 100;
        private readonly TableServiceClient _tableServiceClient;
        private readonly TableClient _tableClient;

        public TableStorageRepository(Uri storageUri, TableSharedKeyCredential tableSharedKeyCredential, string tableName)
        {
            _tableServiceClient = new TableServiceClient(storageUri, tableSharedKeyCredential);
            _tableClient = _tableServiceClient.GetTableClient(tableName);
        }

    
        private async Task ExecuteBatchOperation<TEntity>(TEntity[] entities, TableTransactionActionType tableTransactionActionType, int batchSize, CancellationToken cancellationToken) where TEntity : ITableEntity
        {
            await _tableClient.CreateIfNotExistsAsync(cancellationToken);

            var entitiesGroups = entities.GroupBy(entity => new { entity.PartitionKey });

            foreach (var entityGroup in entitiesGroups)
            {
                foreach (var entityBatch in entityGroup.Batch(batchSize))
                {
                    var tableTransactionActions = entityBatch.Select(entity => new TableTransactionAction(tableTransactionActionType, entity)).ToList();

                    var batchResults = await _tableClient.SubmitTransactionAsync(tableTransactionActions, cancellationToken);

                    var batchErrors = batchResults.Value.Where(batchResult => batchResult.IsError).ToArray();

                    if (batchErrors.Any())
                    {
                        throw new TableStorageBatchExecutionExceptions(batchErrors);
                    }
                }
            }
        }

        public async Task CreateOrUpdateOne<TEntity>(TEntity entity, TableTransactionActionType tableTransactionActionType = TableTransactionActionType.UpsertReplace, CancellationToken cancellationToken = default) where TEntity : ITableEntity
        {
            await CreateOrUpdateMany(new[] { entity }, tableTransactionActionType, cancellationToken: cancellationToken);
        }

        public async Task CreateOrUpdateMany<TEntity>(TEntity[] entities, TableTransactionActionType tableTransactionActionType = TableTransactionActionType.UpsertReplace, int batchSize = TableStorageMaxBatchSize, CancellationToken cancellationToken = default) where TEntity : ITableEntity
        {
            await ExecuteBatchOperation(entities, tableTransactionActionType, batchSize, cancellationToken);
        }

        public async Task DeleteMany<TEntity>(TEntity[] entities, int batchSize = TableStorageMaxBatchSize, CancellationToken cancellationToken = default) where TEntity : ITableEntity
        {
            await ExecuteBatchOperation(entities, TableTransactionActionType.Delete, batchSize, cancellationToken);
        }

        public async Task DeleteOne<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : ITableEntity
        {
            await DeleteMany(new TEntity[] { entity }, cancellationToken: cancellationToken);
        }

        public async Task DeleteOne<TEntity>(string partitionKey, string rowKey, CancellationToken cancellationToken = default) where TEntity : ITableEntity
        {
            await _tableClient.DeleteEntityAsync(partitionKey, rowKey, cancellationToken: cancellationToken);
        }

        public async IAsyncEnumerable<TEntity> GetAll<TEntity>([EnumeratorCancellation] CancellationToken cancellationToken = default) where TEntity : class, ITableEntity, new()
        {

            await _tableClient.CreateIfNotExistsAsync(cancellationToken);

            var asyncPageable = _tableClient.QueryAsync<TEntity>(cancellationToken: cancellationToken);

            await foreach (var page in asyncPageable.AsPages())
            {
                foreach (var entity in page.Values)
                {
                    yield return entity;
                }
            }

        }

        public async IAsyncEnumerable<TEntity> GetMany<TEntity>(Expression<Func<TEntity, bool>> filter, [EnumeratorCancellation] CancellationToken cancellationToken = default) where TEntity : class, ITableEntity, new()
        {

            await _tableClient.CreateIfNotExistsAsync(cancellationToken);

            var asyncPageable = _tableClient.QueryAsync(filter, cancellationToken: cancellationToken);

            await foreach (var page in asyncPageable.AsPages())
            {
                foreach (var entity in page.Values)
                {
                    yield return entity;
                }
            }

        }

        public IAsyncEnumerable<TEntity> GetMany<TEntity>(string partitionKey, CancellationToken cancellationToken = default) where TEntity : class, ITableEntity, new()
        {
            return GetMany<TEntity>((property) => property.PartitionKey == partitionKey, cancellationToken);
        }

        public async Task<TEntity?> GetOne<TEntity>(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default) where TEntity : class, ITableEntity, new()
        {
            return await GetMany(filter, cancellationToken: cancellationToken).FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<TEntity?> GetOne<TEntity>(string partitionKey, string rowKey, CancellationToken cancellationToken = default) where TEntity : class, ITableEntity, new()
        {
            try
            {

                await _tableClient.CreateIfNotExistsAsync(cancellationToken);

                var azureResponse = await _tableClient.GetEntityAsync<TEntity>(partitionKey, rowKey, cancellationToken: cancellationToken);

                return azureResponse.Value;

            }
            catch (Exception ex)
            {
                if (ex.IsEntityNotFound())
                {
                    return null;
                }

                throw;
            }
        }

        public async Task Truncate()
        {
          await _tableClient.DeleteAsync();
        }

        public async Task<bool> DoesEntityExist<TEntity>(string partitionKey, string rowKey, CancellationToken cancellationToken) where TEntity : class, ITableEntity, new()
        {
            var user = await GetOne<TEntity>(partitionKey, rowKey, cancellationToken);

            return user != null;

        }

        public IQueryable<TEntity> Entities<TEntity>() where TEntity : class, ITableEntity, new()
        {
            var tableStorageQueryProvider = new TableStorageQueryProvider<TEntity>(_tableClient);

            return new QueryableTableStorage<TEntity>(tableStorageQueryProvider);
        }
    }
}
