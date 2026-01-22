using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Scrapile.Application.Helpers;

/// <summary>
/// Helper class for running async tasks synchronously without deadlocks.
/// Uses a custom SynchronizationContext with a message pump to avoid
/// the deadlock issues that occur with GetAwaiter().GetResult().
/// </summary>
public static class AsyncHelper
{
    private class ExclusiveSynchronizationContext : SynchronizationContext
    {
        private readonly struct WorkItem(SendOrPostCallback callback, object? state)
        {
            public readonly SendOrPostCallback Callback { get; } = callback;
            public readonly object? State { get; } = state;
        }

        private volatile bool done;
        private readonly AutoResetEvent workItemsWaiting = new(false);
        private readonly ConcurrentQueue<WorkItem> items = new();

        public Exception? InnerException { get; set; }

        public override void Send(SendOrPostCallback callback, object? state)
        {
            throw new NotSupportedException();
        }

        public override void Post(SendOrPostCallback callback, object? state)
        {
            items.Enqueue(new WorkItem(callback, state));
            workItemsWaiting.Set();
        }

        public void EndMessageLoop()
        {
            Post(_ => done = true, null);
        }

        public void BeginMessageLoop()
        {
            while (!done)
            {
                if (items.TryDequeue(out var task))
                {
                    task.Callback(task.State);

                    if (InnerException != null)
                        throw InnerException;
                }
                else
                {
                    workItemsWaiting.WaitOne();
                }
            }
        }

        public override SynchronizationContext CreateCopy() => this;
    }

    /// <summary>
    /// Executes an async <see cref="Task"/> method synchronously without deadlocks.
    /// </summary>
    /// <param name="task">The async task to execute.</param>
    public static void RunSync(Func<Task> task)
    {
        var oldContext = SynchronizationContext.Current;
        var synch = new ExclusiveSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(synch);
        synch.Post(async _ =>
        {
            try
            {
                await task();
            }
            catch (Exception e)
            {
                synch.InnerException = e;
                throw;
            }
            finally
            {
                synch.EndMessageLoop();
            }
        }, null);
        synch.BeginMessageLoop();

        SynchronizationContext.SetSynchronizationContext(oldContext);
    }

    /// <summary>
    /// Executes an async <see cref="Task{TResult}"/> method synchronously without deadlocks.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="task">The async task to execute.</param>
    /// <returns>The result of the task.</returns>
    public static T? RunSync<T>(Func<Task<T>> task)
    {
        var oldContext = SynchronizationContext.Current;
        var synch = new ExclusiveSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(synch);
        T? ret = default;
        synch.Post(async _ =>
        {
            try
            {
                ret = await task();
            }
            catch (Exception e)
            {
                synch.InnerException = e;
                throw;
            }
            finally
            {
                synch.EndMessageLoop();
            }
        }, null);
        synch.BeginMessageLoop();
        SynchronizationContext.SetSynchronizationContext(oldContext);
        return ret;
    }
}
