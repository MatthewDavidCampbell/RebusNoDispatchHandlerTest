using System;
using System.Threading;
using System.Threading.Tasks;

namespace Rebus.NoDispatchHandlers.Tests
{
    public static class WaitHandleExtensions {
        public static Task WaitOneAsync(
            this WaitHandle waitHandle,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            if (waitHandle == null) {
                throw new ArgumentNullException(Messages.WhenArgumentIsNull(nameof(waitHandle)));
            }

            var tcs = new TaskCompletionSource<bool>();

            var rwh = ThreadPool.RegisterWaitForSingleObject(
                waitHandle, 
                delegate { tcs.TrySetResult(true); }, 
                null, 
                -1, 
                true 
            );

            var t = tcs.Task;
            t.ContinueWith( (afterMe) => rwh.Unregister(null), cancellationToken ); 

            return t;
        }
    }
}