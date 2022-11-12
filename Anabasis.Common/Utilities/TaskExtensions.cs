using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.Common
{

    public static class TaskExtensions
    {
        public const int defaultBatchSize = 5;

        private static async Task TryAndCatch(Task task, ConcurrentBag<Exception> exceptions)
        {
            try
            {
                if(task.Status == TaskStatus.Created)
                {
                    task.Start();
                }

                await task;
            }
            catch (AggregateException aggregateException)
            {
                foreach (var exception in aggregateException.Flatten().InnerExceptions)
                {
                    exceptions.Add(exception);
                }
            }
            catch (Exception exception)
            {
                exceptions.Add(exception);
            }
        }

        public static async Task<T> WaitOrTimeout<T>(this Task<T> task, TimeSpan timeout)
        {
            var result = await Task.WhenAny(new[] { task, Task.Delay(timeout) });

            if (task == result) return task.Result;

            throw new TimeoutException($"Task timeout - Timeout => {timeout}");

        }

        public static Task Execute(this IEnumerable<Action> actions,
            int batchSize = defaultBatchSize,
            TimeSpan? timeout = null,
            CancellationToken? cancellationToken = null)
        {

            cancellationToken ??= CancellationToken.None;

            if (timeout.HasValue)
            {
                var timeoutCancellationTokenSource = new CancellationTokenSource();
                timeoutCancellationTokenSource.CancelAfter(timeout.Value);

                cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken.Value, timeoutCancellationTokenSource.Token).Token;
            }

            var tasks = actions.Select(action => new Task(action, cancellationToken.Value));

            return ExecuteAndWaitForCompletion(tasks, batchSize);

        }

        public static async Task ExecuteAndWaitForCompletion(this IEnumerable<Task> tasks, 
            int batchSize = defaultBatchSize,
            TimeSpan? timeout = null,
            CancellationToken? cancellationToken = null)
        {

            cancellationToken ??= CancellationToken.None;

            if (timeout.HasValue)
            {
                var timeoutCancellationTokenSource = new CancellationTokenSource();
                timeoutCancellationTokenSource.CancelAfter(timeout.Value);

                cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken.Value, timeoutCancellationTokenSource.Token).Token;
            }

            var semaphore = new SemaphoreSlim(batchSize);
            var exceptions = new ConcurrentBag<Exception>();
            var resultTasks = new List<Task>();

            var runningTasks = tasks.Select(async task =>
            {
                try
                {
                    await TryAndCatch(task, exceptions);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            void removeCompletedTasks() => resultTasks.RemoveAll(task => task.IsCompleted);

            await semaphore.WaitAsync();

            var count = 0L;
       
            foreach (var task in runningTasks)
            {
                cancellationToken?.ThrowIfCancellationRequested();

                resultTasks.Add(task);

                count++;

                if (count % batchSize == 0)
                {
                    removeCompletedTasks();
                }

                await semaphore.WaitAsync();
            }

            try
            {
                await Task.WhenAll(resultTasks);
            }
            catch 
            {
            }

            if (exceptions.Count != 0)
            {
                throw new AggregateException(exceptions);
            }

        }

    }
}
