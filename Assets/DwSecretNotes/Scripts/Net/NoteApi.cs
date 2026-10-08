using System;
using System.Text;
using System.Threading.Tasks;
using DwSecretNotes.Core;
using DwSecretNotes.Crypto;
using UnityEngine;
using UnityEngine.Networking;
namespace DwSecretNotes.Net
{
    public enum DecryptStatus { Ok, NotFound, Failed }
    public sealed class DecryptResult { public DecryptStatus Status; public DecryptedNote Note; }
    public sealed class EncryptResult { public bool Ok; public string Link; public string Error; }
    public static class NoteApi
    {
        const string Module = "NoteApi";
        const int TimeoutSeconds = 30;
        static string Store => AppInfo.BaseUrl + "/api/msg_store.php";
        [Serializable] sealed class GetResponse { public string status; public string pass_override; public NotePayload payload; }
        public static string Timestamp(DateTime now) => now.ToString("dd.MM.yyyy_HH-mm-ss-", System.Globalization.CultureInfo.InvariantCulture) + now.Millisecond.ToString("000");
        public static string BuildLink(string ts, string pass) => $"{AppInfo.BaseUrl}/msges/view.php?com={Uri.EscapeDataString(ts)}&pass={Uri.EscapeDataString(pass)}";
        static UnityWebRequest Req(string url, string method)
        {
            var r = new UnityWebRequest(url, method) { downloadHandler = new DownloadHandlerBuffer(), timeout = TimeoutSeconds };
            r.SetRequestHeader("X-API-Key", AppInfo.ApiKey);
            return r;
        }
        public static async Task<EncryptResult> EncryptAndStore(string text, byte[] image)
        {
            DwLog.Call(Module);
            string json;
            var pass = NoteCrypto.GeneratePassword();
            try { json = await Task.Run(() => NoteCrypto.ToJson(NoteCrypto.Encrypt(pass, text, image))); }
            catch (Exception e) { DwLog.E(Module, "encrypt failed", e); return new EncryptResult { Error = L10n.T("error_encrypt_failed") }; }
            var ts = Timestamp(DateTime.Now);
            using var req = Req($"{Store}?action=save&ts={Uri.EscapeDataString(ts)}", UnityWebRequest.kHttpVerbPOST);
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)) { contentType = "application/json" };
            req.SetRequestHeader("Content-Type", "application/json");
            await WebRequestAwaiter.Send(req);
            DwLog.Var(Module, "saveHttp", req.responseCode);
            if (req.result != UnityWebRequest.Result.Success) return new EncryptResult { Error = L10n.T(req.responseCode == 429 ? "error_rate_limit" : "error_encrypt_network") };
            return new EncryptResult { Ok = true, Link = BuildLink(ts, pass) };
        }
        public static async Task<DecryptResult> FetchAndDecrypt(string alias, string pass)
        {
            DwLog.Call(Module);
            var com = alias.StartsWith("link:") ? alias.Substring(5) : alias;
            using var req = Req($"{Store}?action=get&com={Uri.EscapeDataString(com)}", UnityWebRequest.kHttpVerbGET);
            await WebRequestAwaiter.Send(req);
            DwLog.Var(Module, "getHttp", req.responseCode);
            if (req.result != UnityWebRequest.Result.Success) return new DecryptResult { Status = DecryptStatus.Failed };
            GetResponse resp;
            try { resp = JsonUtility.FromJson<GetResponse>(req.downloadHandler.text); }
            catch (Exception e) { DwLog.E(Module, "bad json", e); return new DecryptResult { Status = DecryptStatus.Failed }; }
            if (resp == null || resp.status == "not_found") return new DecryptResult { Status = DecryptStatus.NotFound };
            var finalPass = string.IsNullOrEmpty(resp.pass_override) ? pass : resp.pass_override;
            DecryptedNote note;
            try { note = await Task.Run(() => NoteCrypto.Decrypt(resp.payload, finalPass)); }
            catch (Exception e) { DwLog.W(Module, "decrypt failed: " + e.Message); return new DecryptResult { Status = DecryptStatus.NotFound }; }
            if (note.Text.Contains("Nachricht nicht gefunden")) return new DecryptResult { Status = DecryptStatus.NotFound };
            Unlink(com);
            return new DecryptResult { Status = DecryptStatus.Ok, Note = note };
        }
        static async void Unlink(string com)
        {
            try { using var req = Req($"{Store}?action=unlink&com={Uri.EscapeDataString(com)}", UnityWebRequest.kHttpVerbGET); await WebRequestAwaiter.Send(req); DwLog.Var(Module, "unlinkHttp", req.responseCode); }
            catch (Exception e) { DwLog.E(Module, "unlink failed", e); }
        }
        public static async Task<ParsedLink?> ResolveLink(string input)
        {
            var local = LinkParser.ParseLocal(input, out var url);
            if (local.HasValue || url == null) return local;
            var cur = url;
            try
            {
                for (int i = 0; i < 6; i++)
                {
                    using var req = new UnityWebRequest(cur, UnityWebRequest.kHttpVerbGET) { downloadHandler = new DownloadHandlerBuffer(), redirectLimit = 0, timeout = 4 };
                    await WebRequestAwaiter.Send(req);
                    var loc = req.GetResponseHeader("Location");
                    if (req.responseCode < 300 || req.responseCode > 308 || string.IsNullOrEmpty(loc) || i == 5) break;
                    cur = loc.StartsWith("http") ? loc : new Uri(new Uri(cur), loc).ToString();
                    DwLog.Var(Module, "redirect", cur);
                }
            }
            catch (Exception e) { DwLog.W(Module, "resolve failed: " + e.Message); }
            var p = LinkParser.ParseUri(cur);
            return p.Alias.Length >= 5 ? p : (ParsedLink?)null;
        }
    }
}
