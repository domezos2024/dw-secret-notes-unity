using System.Threading.Tasks;
using UnityEngine.Networking;
namespace DwSecretNotes.Net
{
    public static class WebRequestAwaiter
    {
        public static Task<UnityWebRequest> Send(UnityWebRequest req)
        {
            var tcs = new TaskCompletionSource<UnityWebRequest>();
            req.SendWebRequest().completed += _ => tcs.TrySetResult(req);
            return tcs.Task;
        }
    }
}
