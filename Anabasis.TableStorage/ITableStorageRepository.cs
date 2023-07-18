using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.TableStorage
{
    public interface ITableStorageRepository
    {
        IQueryable<TEntity> Entities<TEntity>() where TEntity : class, ITableEntity, new();
        Task CreateOrUpdateMany<TEntity>(TEntity[] entities, TableTransactionActionType tableTransactionActionType = TableTransactionActionType.UpsertReplace, int batchSize = 100, CancellationToken cancellationToken = default) where TEntity : ITableEntity;
        Task CreateOrUpdateOne<TEntity>(TEntity entity, TableTransactionActionType tableTransactionActionType = TableTransactionActionType.UpsertReplace, CancellationToken cancellationToken = default) where TEntity : ITableEntity;
        Task DeleteMany<TEntity>(TEntity[] entities, int batchSize = 100, CancellationToken cancellationToken = default) where TEntity : ITableEntity;
        Task DeleteOne<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : ITableEntity;
        Task DeleteOne<TEntity>(string partitionKey, string rowKey, CancellationToken cancellationToken = default) where TEntity : ITableEntity;
        IAsyncEnumerable<TEntity> GetAll<TEntity>(CancellationToken cancellationToken = default) where TEntity : class, ITableEntity, new();
        IAsyncEnumerable<TEntity> GetMany<TEntity>(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default) where TEntity : class, ITableEntity, new();
        IAsyncEnumerable<TEntity> GetMany<TEntity>(string partitionKey, CancellationToken cancellationToken = default) where TEntity : class, ITableEntity, new();
        Task<TEntity?> GetOne<TEntity>(string partitionKey, string rowKey, CancellationToken cancellationToken = default) where TEntity : class, ITableEntity, new();
        Task<TEntity?> GetOne<TEntity>(Expression<Func<TEntity, bool>> filter, CancellationToken cancellationToken = default) where TEntity : class, ITableEntity, new();
        Task<bool> DoesEntityExist<TEntity>(string partitionKey, string rowKey, CancellationToken cancellationToken = default) where TEntity : class, ITableEntity, new();
        Task Truncate();
    }
}