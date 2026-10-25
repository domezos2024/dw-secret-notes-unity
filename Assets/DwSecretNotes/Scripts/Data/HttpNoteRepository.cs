using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DwSecretNotes.Business;
using DwSecretNotes.Core;
using DwSecretNotes.Crypto;
using UnityEngine;
using UnityEngine.Networking;
namespace DwSecretNotes.Data
{
    public sealed class HttpNoteRepository : INoteRepository
    {
        const string Module = "NoteApi";
        const int TimeoutSeconds = 30;
        static string Store => AppInfo.BaseUrl + "/api/msg_store.php";
        [Serializable] sealed class GetResponse { public string status; public string pass_override; public NotePayload payload; }
        static UnityWebRequest Req(string url, string method)
        {
            var r = new UnityWebRequest(url, method) { downloadHandler = new DownloadHandlerBuffer(), timeout = TimeoutSeconds };
            r.SetRequestHeader("X-API-Key", AppInfo.ApiKey);
            return r;
        }
        public async Task<NoteSaveStatus> Save(string timestamp, string payload, CancellationToken cancellationToken = default)
        {
            using var req = Req($"{Store}?action=save&ts={Uri.EscapeDataString(timestamp)}", UnityWebRequest.kHttpVerbPOST);
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload)) { contentType = "application/json" };
            req.SetRequestHeader("Content-Type", "application/json");
            await WebRequestAwaiter.Send(req, cancellationToken);
            DwLog.Var(Module, "saveHttp", req.responseCode);
            if (req.result == UnityWebRequest.Result.Success) return NoteSaveStatus.Saved;
            return req.responseCode == 429 ? NoteSaveStatus.RateLimited : NoteSaveStatus.Failed;
        }
        public async Task<NoteFetchResult> Fetch(string alias, CancellationToken cancellationToken = default)
        {
            var com = alias.StartsWith("link:") ? alias.Substring(5) : alias;
            using var req = Req($"{Store}?action=get&com={Uri.EscapeDataString(com)}", UnityWebRequest.kHttpVerbGET);
            await WebRequestAwaiter.Send(req, cancellationToken);
            DwLog.Var(Module, "getHttp", req.responseCode);
            if (req.result != UnityWebRequest.Result.Success) return new NoteFetchResult { Status = NoteFetchStatus.Failed };
            GetResponse resp;
            try { resp = JsonUtility.FromJson<GetResponse>(req.downloadHandler.text); }
            catch (Exception e) { DwLog.E(Module, "bad json", e); return new NoteFetchResult { Status = NoteFetchStatus.Failed }; }
            if (resp == null || resp.status == "not_found") return new NoteFetchResult { Status = NoteFetchStatus.NotFound };
            if (resp.payload == null) return new NoteFetchResult { Status = NoteFetchStatus.Failed };
            return new NoteFetchResult { Status = NoteFetchStatus.Found, Payload = resp.payload, PassOverride = resp.pass_override, Alias = com };
        }
        public async Task Unlink(string alias, CancellationToken cancellationToken = default)
        {
            try { using var req = Req($"{Store}?action=unlink&com={Uri.EscapeDataString(alias)}", UnityWebRequest.kHttpVerbGET); await WebRequestAwaiter.Send(req, cancellationToken); DwLog.Var(Module, "unlinkHttp", req.responseCode); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception e) { DwLog.E(Module, "unlink failed", e); }
        }
        public async Task<ParsedLink?> ResolveLink(string input, CancellationToken cancellationToken = default)
        {
            var local = LinkParser.ParseLocal(input, out var url);
            if (local.HasValue || url == null) return local;
            var cur = url;
            try
            {
                for (int i = 0; i < 6; i++)
                {
                    using var req = new UnityWebRequest(cur, UnityWebRequest.kHttpVerbGET) { downloadHandler = new DownloadHandlerBuffer(), redirectLimit = 0, timeout = 4 };
                    await WebRequestAwaiter.Send(req, cancellationToken);
                    var loc = req.GetResponseHeader("Location");
                    if (req.responseCode < 300 || req.responseCode > 308 || string.IsNullOrEmpty(loc) || i == 5) break;
                    cur = loc.StartsWith("http") ? loc : new Uri(new Uri(cur), loc).ToString();
                    DwLog.Var(Module, "redirect", cur);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception e) { DwLog.W(Module, "resolve failed: " + e.Message); }
            var p = LinkParser.ParseUri(cur);
            return p.Alias.Length >= 5 ? p : (ParsedLink?)null;
        }
    }
}
