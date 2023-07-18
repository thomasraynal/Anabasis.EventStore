using Azure;
using Azure.Data.Tables;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Anabasis.TableStorage
{

    public class TableStorageQueryProvider<TEntity> : IQueryProvider where TEntity : class, ITableEntity, new()
    {
        private readonly TableClient _tableClient;

        public TableStorageQueryProvider(TableClient tableClient)
        {
            _tableClient = tableClient;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new QueryableTableStorage<TEntity>(this, expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return (IQueryable<TElement>)new QueryableTableStorage<TEntity>(this, expression);
        }

        public object? Execute(Expression expression)
        {
            return Execute<TEntity>(expression);
        }
        //todo: interpreta TS query
        public TResult Execute<TResult>(Expression expression)
        {
            var isEnumerable = (typeof(TResult).Name == "IEnumerable`1");

            if (!isEnumerable)
            {
                throw new NotSupportedException("Cannot handle scalar query");
            }

            _tableClient.CreateIfNotExists();

           // var compile = Expression.Lambda(expression).Compile();

            var pageable = _tableClient.Query<TEntity>();

            //var entities = new List<TEntity>();

            //foreach (var entity in pageable)
            //{
            //    //var isFilteredIn = (bool)compile.DynamicInvoke(entity);

            //    //if (isFilteredIn)
            //    //{
            //    entities.Add(entity);
            //    //}
            //}

            return (TResult)(object)pageable.ToArray();

        }
    }

    public class QueryableTableStorage<TEntity> : IQueryable<TEntity> where TEntity : class, ITableEntity, new()
    {

        public QueryableTableStorage(TableStorageQueryProvider<TEntity> tableStorageQueryProvider, Expression? expression = null)
        {
            Provider = tableStorageQueryProvider;
            Expression = expression ?? Expression.Constant(this);
        }

        public Type ElementType => typeof(TEntity);

        public Expression Expression { get; } 

        public IQueryProvider Provider { get; }

        public IEnumerator<TEntity> GetEnumerator()
        {
            return Provider.Execute<IEnumerable<TEntity>>(Expression).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
