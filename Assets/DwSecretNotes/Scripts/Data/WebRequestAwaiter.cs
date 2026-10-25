using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;
namespace DwSecretNotes.Data
{
    public static class WebRequestAwaiter
    {
        public static async Task<UnityWebRequest> Send(UnityWebRequest req, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tcs = new TaskCompletionSource<UnityWebRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
            var operation = req.SendWebRequest();
            operation.completed += _ =>
            {
                if (cancellationToken.IsCancellationRequested) tcs.TrySetCanceled(cancellationToken);
                else tcs.TrySetResult(req);
            };
            using (cancellationToken.Register(() =>
            {
                req.Abort();
                tcs.TrySetCanceled(cancellationToken);
            }))
                return await tcs.Task;
        }
    }
}
