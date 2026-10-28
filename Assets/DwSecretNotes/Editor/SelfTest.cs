using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DwSecretNotes.Business;
using DwSecretNotes.Core;
using DwSecretNotes.Crypto;
using DwSecretNotes.Data;
using UnityEditor;
using Debug = UnityEngine.Debug;
using L10n = DwSecretNotes.Core.L10n;
namespace DwSecretNotes.EditorTools
{
    public static class SelfTest
    {
        static int fails;
        static void Check(string name, bool ok, string detail = "") { if (!ok) fails++; Debug.Log($"[DW-TEST] {(ok ? "PASS" : "FAIL")} {name} {detail}"); }
        static string Hex(byte[] b) => string.Concat(b.Select(x => x.ToString("x2")));
        static byte[] Unhex(string s) { var b = new byte[s.Length / 2]; for (int i = 0; i < b.Length; i++) b[i] = Convert.ToByte(s.Substring(i * 2, 2), 16); return b; }
        [MenuItem("dw Secret Notes/Selbsttest", priority = 40)]
        static void RunMenu() => Watch(Run(), "selftest", false);

        public static async Task<bool> Run()
        {
            fails = 0;
            Check("sha256-abc", Hex(Pbkdf2Sha256.Sha256(Encoding.ASCII.GetBytes("abc"))) == "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
            var r1 = Pbkdf2Sha256.Derive(Encoding.ASCII.GetBytes("passwd"), Encoding.ASCII.GetBytes("salt"), 1, 64);
            Check("pbkdf2-rfc7914", Hex(r1) == "55ac046e56e3089fec1691c22544b605f94185216dde0465e68b9d57c20dacbc49ca9cccf179b645991664b39d77ef317c71b845b1e30bd509112041d3a19783", Hex(r1));
            using (var ref2 = new Rfc2898DeriveBytes(Encoding.UTF8.GetBytes("abc123"), Encoding.UTF8.GetBytes("saltsalt"), 1000, HashAlgorithmName.SHA256))
                Check("pbkdf2-vs-dotnet", Hex(ref2.GetBytes(32)) == Hex(Pbkdf2Sha256.Derive(Encoding.UTF8.GetBytes("abc123"), Encoding.UTF8.GetBytes("saltsalt"), 1000, 32)));
            var sw = Stopwatch.StartNew();
            var key = NoteCrypto.DeriveKey("1671330142412131832322139820315246110200182");
            sw.Stop();
            Check("pbkdf2-100k-python", Hex(key) == "a4b780c2d4663b7d4c904badd9d7384e6b792b22a2807377820f8fbb73d2c9ba", $"{sw.ElapsedMilliseconds} ms");
            using (var g = new AesGcm256(key))
            {
                var iv = Enumerable.Range(0, 12).Select(i => (byte)i).ToArray();
                var ct = g.Encrypt(iv, Encoding.UTF8.GetBytes("Grüße 秘密 ✓ test"));
                Check("gcm-python", Hex(ct) == "023536cd51580c2a72cabd449123080ac6cb5f3b4a5831c00a5324a86b5993d5aa372658972f0a", Hex(ct));
                Check("gcm-roundtrip", Encoding.UTF8.GetString(g.Decrypt(iv, ct)) == "Grüße 秘密 ✓ test");
                ct[3] ^= 1; bool threw = false; try { g.Decrypt(iv, ct); } catch (CryptographicException) { threw = true; }
                Check("gcm-tamper", threw);
            }
            using (var g = new AesGcm256(Unhex("feffe9928665731c6d6a8f9467308308feffe9928665731c6d6a8f9467308308")))
            {
                var ct = g.Encrypt(Unhex("cafebabefacedbaddecaf888"), Unhex("d9313225f88406e5a55909c5aff5269a86a7a9531534f7da2e4c303d8a318a721c3c0c95956809532fcf0e2449a6b525b16aedf5aa0de657ba637b391aafd255"));
                Check("gcm-nist-tc15", Hex(ct) == "522dc1f099567d07f47f37a32a84427d643a8cdcbfe5c0c97598a2bd2555d1aa8cb08e48590dbb3da7b08b1056828838c5f61e6393ba7a0abcc9f662898015ad" + "b094dac5d93471bdec1a502270e3cc6c", Hex(ct));
            }
            var big = new byte[300_001]; new System.Random(1).NextBytes(big);
            var pl = NoteCrypto.Encrypt("x", "hello", big); var dn = NoteCrypto.Decrypt(pl, "x");
            Check("note-roundtrip-image", dn.Text == "hello" && dn.Image.SequenceEqual(big));
            var json = NoteCrypto.ToJson(pl); var back = UnityEngine.JsonUtility.FromJson<NotePayload>(json);
            Check("payload-json", back.iv.SequenceEqual(pl.iv) && back.imgData.Length == pl.imgData.Length);
            var pw = NoteCrypto.GeneratePassword(); Check("password-format", pw.All(char.IsDigit) && pw.Length >= 16, pw.Length.ToString());
            Check("timestamp", System.Text.RegularExpressions.Regex.IsMatch(SecretNotesService.Timestamp(new DateTime(2026, 8, 17, 4, 15, 16, 7)), @"^17\.08\.2026_04-15-16-007$"));
            Check("deeplink", LinkParser.AliasFromDeepLink("https://domezos-ware.com/msges/view.php?com=17.08.2026_04-15-16-700&pass=123") == "17.08.2026_04-15-16-700|123");
            Check("sharetext", LinkParser.AliasFromShareText("I sent you https://domezos-ware.com/msges/view.php?com=AB&pass=9 ok") == "AB|9");
            Check("sharetext-alias", LinkParser.AliasFromShareText("code 12.10.2026_01-02-03-004 here") == "12.10.2026_01-02-03-004");
            var pr = LinkParser.ParseLocal("domezos-ware.com/msges/view.php?com=17.08.2026_04-15-16-700&pass=55", out _);
            Check("parse-local", pr.HasValue && pr.Value.Alias == "17.08.2026_04-15-16-700" && pr.Value.Pass == "55");
            Check("parse-short", LinkParser.ParseLocal("ABCDE", out _)?.Alias == "ABCDE");
            Check("plural-ru", L10n.PluralCategory("ru", 1) == "one" && L10n.PluralCategory("ru", 3) == "few" && L10n.PluralCategory("ru", 11) == "many" && L10n.PluralCategory("ru", 22) == "few");
            Check("no-arabic", L10n.Languages.All(l => l.Tag != "ar") && L10n.Languages.Count() == 14, L10n.Languages.Count().ToString());
            foreach (var l in L10n.Languages) { var t = L10n.TableFor(l.Tag); Check("l10n-" + l.Tag, KeysComplete(t, l.Tag, out var miss), miss); }
            Check("theme-count", ThemeCatalog.All.Count == 17);
            foreach (var theme in ThemeCatalog.All)
            {
                Check("theme-text-contrast-" + theme.Id, ColorMath.Contrast(theme.OnSurface, theme.Surface) >= 7.0);
                Check("theme-muted-text-contrast-" + theme.Id, ColorMath.Contrast(theme.OnSurfaceVariant, theme.SurfaceVariant) >= 4.5);
                Check("theme-accent-text-contrast-" + theme.Id, ColorMath.Contrast(theme.AccentText, theme.Surface) >= 4.5);
                Check("theme-tertiary-contrast-" + theme.Id, ColorMath.Contrast(theme.Tertiary, theme.Surface) >= 4.5);
                Check("theme-primary-content-contrast-" + theme.Id, ColorMath.Contrast(theme.OnPrimaryContainer, theme.PrimaryContainer) >= 4.5);
            }
            // The continuation only returns a value; Unity and Editor APIs stay on the main thread in Watch.
            var memoryRoundTrip = await BusinessMemoryRoundTrip().ConfigureAwait(false);
            var cancellation = await BusinessCancellation().ConfigureAwait(false);
            var removalVerification = await BusinessRemovalVerification().ConfigureAwait(false);
            return memoryRoundTrip && cancellation && removalVerification;
        }
        static async Task<bool> BusinessRemovalVerification()
        {
            var repository = new VerificationNoteRepository();
            var result = await new SecretNotesService(repository).FetchAndDecrypt("verify", "pass").ConfigureAwait(false);
            bool displayReady = result.Status == DecryptStatus.Ok && result.Note.Text == "verification"
                && repository.FetchCount == 1 && !repository.VerificationDone.Task.IsCompleted;
            repository.AllowUnlink.TrySetResult(true);
            var completed = await Task.WhenAny(repository.VerificationDone.Task, Task.Delay(10000)).ConfigureAwait(false);
            bool verified = completed == repository.VerificationDone.Task
                && repository.FetchCount == 2 && repository.VerificationDone.Task.Result;
            Check("background-removal-verification", displayReady && verified);
            return displayReady && verified;
        }
        sealed class VerificationNoteRepository : INoteRepository
        {
            public readonly TaskCompletionSource<bool> AllowUnlink = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            public readonly TaskCompletionSource<bool> VerificationDone = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            public int FetchCount;
            bool removed;
            readonly NotePayload payload = NoteCrypto.Encrypt("pass", "verification", null);
            public Task<NoteSaveStatus> Save(string timestamp, string data, CancellationToken cancellationToken = default) => Task.FromResult(NoteSaveStatus.Failed);
            public Task<NoteFetchResult> Fetch(string alias, CancellationToken cancellationToken = default)
            {
                FetchCount++;
                var result = removed
                    ? new NoteFetchResult { Status = NoteFetchStatus.NotFound }
                    : new NoteFetchResult { Status = NoteFetchStatus.Found, Alias = alias, Payload = payload };
                if (FetchCount == 2) VerificationDone.TrySetResult(result.Status == NoteFetchStatus.NotFound);
                return Task.FromResult(result);
            }
            public async Task Unlink(string alias, CancellationToken cancellationToken = default)
            {
                await AllowUnlink.Task.ConfigureAwait(false);
                removed = true;
            }
            public Task<ParsedLink?> ResolveLink(string input, CancellationToken cancellationToken = default) => Task.FromResult<ParsedLink?>(null);
        }
        static async Task<bool> BusinessCancellation()
        {
            var saveRepository = new BlockingNoteRepository();
            var saveService = new SecretNotesService(saveRepository);
            using var saveCancellation = new CancellationTokenSource();
            var save = saveService.EncryptAndStore("cancel save", null, saveCancellation.Token);
            if (await Task.WhenAny(saveRepository.Entered.Task, Task.Delay(10000)).ConfigureAwait(false) != saveRepository.Entered.Task)
            { saveCancellation.Cancel(); return false; }
            saveCancellation.Cancel();
            var saveCanceled = await IsCanceled(save).ConfigureAwait(false);

            var fetchRepository = new BlockingNoteRepository();
            var fetchService = new SecretNotesService(fetchRepository);
            using var fetchCancellation = new CancellationTokenSource();
            var fetch = fetchService.FetchAndDecrypt("test", "pass", fetchCancellation.Token);
            if (await Task.WhenAny(fetchRepository.Entered.Task, Task.Delay(10000)).ConfigureAwait(false) != fetchRepository.Entered.Task)
            { fetchCancellation.Cancel(); return false; }
            fetchCancellation.Cancel();
            return saveCanceled && await IsCanceled(fetch).ConfigureAwait(false);
        }
        static async Task<bool> IsCanceled<T>(Task<T> task)
        {
            try { await task.ConfigureAwait(false); return false; }
            catch (OperationCanceledException) { return true; }
        }
        sealed class BlockingNoteRepository : INoteRepository
        {
            public readonly TaskCompletionSource<bool> Entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            public async Task<NoteSaveStatus> Save(string timestamp, string payload, CancellationToken cancellationToken = default)
            {
                Entered.TrySetResult(true);
                await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
                return NoteSaveStatus.Failed;
            }
            public async Task<NoteFetchResult> Fetch(string alias, CancellationToken cancellationToken = default)
            {
                Entered.TrySetResult(true);
                await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
                return new NoteFetchResult { Status = NoteFetchStatus.Failed };
            }
            public Task Unlink(string alias, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<ParsedLink?> ResolveLink(string input, CancellationToken cancellationToken = default) => Task.FromResult<ParsedLink?>(null);
        }
        static async Task<bool> BusinessMemoryRoundTrip()
        {
            var service = new SecretNotesService(new MemoryNoteRepository());
            var encrypted = await service.EncryptAndStore("business layer ✓", null);
            if (!encrypted.Ok) return false;
            var decrypted = await service.DecryptLink(encrypted.Link);
            var secondRead = await service.DecryptLink(encrypted.Link);
            var literal = "Nachricht nicht gefunden is valid note text";
            var literalLink = await service.EncryptAndStore(literal, null);
            var literalDecrypted = literalLink.Ok ? await service.DecryptLink(literalLink.Link) : null;
            return decrypted.Status == DecryptStatus.Ok
                && decrypted.Note.Text == "business layer ✓"
                && secondRead.Status == DecryptStatus.NotFound
                && literalDecrypted?.Status == DecryptStatus.Ok
                && literalDecrypted.Note.Text == literal;
        }
        sealed class MemoryNoteRepository : INoteRepository
        {
            readonly Dictionary<string, NotePayload> entries = new Dictionary<string, NotePayload>();
            public Task<NoteSaveStatus> Save(string timestamp, string payload, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                entries[timestamp] = UnityEngine.JsonUtility.FromJson<NotePayload>(payload);
                return Task.FromResult(NoteSaveStatus.Saved);
            }
            public Task<NoteFetchResult> Fetch(string alias, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!entries.TryGetValue(alias, out var payload))
                    return Task.FromResult(new NoteFetchResult { Status = NoteFetchStatus.NotFound });
                return Task.FromResult(new NoteFetchResult { Status = NoteFetchStatus.Found, Alias = alias, Payload = payload });
            }
            public Task Unlink(string alias, CancellationToken cancellationToken = default) { cancellationToken.ThrowIfCancellationRequested(); entries.Remove(alias); return Task.CompletedTask; }
            public Task<ParsedLink?> ResolveLink(string input, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var link = LinkParser.ParseUri(input);
                return Task.FromResult<ParsedLink?>(link.Alias.Length > 0 ? link : (ParsedLink?)null);
            }
        }
        static bool KeysComplete(StringTable t, string tag, out string miss)
        {
            var en = UnityEngine.JsonUtility.FromJson<Keys>(UnityEngine.Resources.Load<UnityEngine.TextAsset>("i18n/en").text);
            var m = new System.Collections.Generic.List<string>();
            foreach (var k in en.keys) if (!k.StartsWith("note_auto_delete_") && !t.Has(k)) m.Add(k);
            foreach (var n in new[] { 0, 1, 2, 3, 5, 11, 21, 60 }) if (!t.Has("note_auto_delete_" + L10n.PluralCategory(tag, n))) m.Add("note_auto_delete_" + L10n.PluralCategory(tag, n));
            miss = string.Join(",", m.Distinct()); return m.Count == 0;
        }
        [Serializable] sealed class Keys { public string[] keys; }
        public static void RunBatch() => Watch(Run(), "selftest", true);
        public static void OnlineBatch() => Watch(OnlineTask(), "online", true);
        [MenuItem("dw Secret Notes/Online-Test (echtes Backend)", priority = 41)]
        public static void OnlineTest() => Watch(OnlineTask(), "online", false);
        static void Watch(Task<bool> task, string name, bool batch)
        {
            var start = DateTime.UtcNow;
            void Poll()
            {
                if (!task.IsCompleted && (DateTime.UtcNow - start).TotalSeconds <= 90) return;
                EditorApplication.update -= Poll;
                if (task.IsFaulted) Debug.LogException(task.Exception);
                var ok = task.Status == TaskStatus.RanToCompletion && task.Result;
                if (name == "selftest")
                {
                    Check("business-memory-one-time-and-cancellation", ok);
                    Debug.Log($"[DW-TEST] RESULT fails={fails}");
                    ok = fails == 0;
                }
                else Debug.Log($"[DW-TEST] online RESULT {(ok ? "PASS" : "FAIL")}");
                if (batch) EditorApplication.Exit(ok ? 0 : 1);
            }
            EditorApplication.update += Poll;
        }
        static async System.Threading.Tasks.Task<bool> OnlineTask()
        {
            var notes = new SecretNotesService(new HttpNoteRepository());
            var enc = await notes.EncryptAndStore("dw unity selftest " + DateTime.Now.ToString("O"), null);
            Debug.Log($"[DW-TEST] online encrypt ok={enc.Ok} err={enc.Error}");
            if (!enc.Ok) return false;
            var p = LinkParser.ParseUri(enc.Link);
            var dec = await notes.FetchAndDecrypt(p.Alias, p.Pass);
            bool ok = dec.Status == DecryptStatus.Ok && (dec.Note?.Text?.StartsWith("dw unity selftest") ?? false);
            Debug.Log($"[DW-TEST] online decrypt status={dec.Status} ok={ok}");
            await System.Threading.Tasks.Task.Delay(2000);
            var again = await notes.FetchAndDecrypt(p.Alias, p.Pass);
            Debug.Log($"[DW-TEST] online second read status={again.Status} (expected NotFound)");
            return ok && again.Status == DecryptStatus.NotFound;
        }
    }
}
