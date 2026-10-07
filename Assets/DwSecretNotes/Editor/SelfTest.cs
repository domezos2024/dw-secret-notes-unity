using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DwSecretNotes.Core;
using DwSecretNotes.Crypto;
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
        public static void Run()
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
            Check("timestamp", System.Text.RegularExpressions.Regex.IsMatch(Net.NoteApi.Timestamp(new DateTime(2026, 8, 17, 4, 15, 16, 7)), @"^17\.08\.2026_04-15-16-007$"));
            Check("deeplink", LinkParser.AliasFromDeepLink("https://domezos-ware.com/msges/view.php?com=17.08.2026_04-15-16-700&pass=123") == "17.08.2026_04-15-16-700|123");
            Check("sharetext", LinkParser.AliasFromShareText("I sent you https://domezos-ware.com/msges/view.php?com=AB&pass=9 ok") == "AB|9");
            Check("sharetext-alias", LinkParser.AliasFromShareText("code 12.10.2026_01-02-03-004 here") == "12.10.2026_01-02-03-004");
            var pr = LinkParser.ParseLocal("domezos-ware.com/msges/view.php?com=17.08.2026_04-15-16-700&pass=55", out _);
            Check("parse-local", pr.HasValue && pr.Value.Alias == "17.08.2026_04-15-16-700" && pr.Value.Pass == "55");
            Check("parse-short", LinkParser.ParseLocal("ABCDE", out _)?.Alias == "ABCDE");
            Check("plural-ru", L10n.PluralCategory("ru", 1) == "one" && L10n.PluralCategory("ru", 3) == "few" && L10n.PluralCategory("ru", 11) == "many" && L10n.PluralCategory("ru", 22) == "few");
            Check("plural-ar", L10n.PluralCategory("ar", 0) == "zero" && L10n.PluralCategory("ar", 2) == "two" && L10n.PluralCategory("ar", 5) == "few" && L10n.PluralCategory("ar", 15) == "many" && L10n.PluralCategory("ar", 100) == "other");
            foreach (var l in L10n.Languages) { var t = L10n.TableFor(l.Tag); Check("l10n-" + l.Tag, t.Has("btn_encrypt") && t.Has("note_auto_delete_other")); }
            Check("theme-count", ThemeCatalog.All.Count == 17);
            Debug.Log($"[DW-TEST] RESULT fails={fails}");
        }
        public static void RunBatch() { Run(); EditorApplication.Exit(fails == 0 ? 0 : 1); }
        public static void OnlineBatch()
        {
            var t = OnlineTask(); var start = DateTime.Now;
            EditorApplication.update += () => { if (t.IsCompleted || (DateTime.Now - start).TotalSeconds > 90) EditorApplication.Exit(t.IsCompleted && t.Result ? 0 : 1); };
        }
        [MenuItem("dw Secret Notes/Online-Test (echtes Backend)", priority = 41)]
        public static async void OnlineTest() => await OnlineTask();
        static async System.Threading.Tasks.Task<bool> OnlineTask()
        {
            var enc = await Net.NoteApi.EncryptAndStore("dw unity selftest " + DateTime.Now.ToString("O"), null);
            Debug.Log($"[DW-TEST] online encrypt ok={enc.Ok} err={enc.Error}");
            if (!enc.Ok) return false;
            var p = LinkParser.ParseUri(enc.Link);
            var dec = await Net.NoteApi.FetchAndDecrypt(p.Alias, p.Pass);
            bool ok = dec.Status == Net.DecryptStatus.Ok && (dec.Note?.Text?.StartsWith("dw unity selftest") ?? false);
            Debug.Log($"[DW-TEST] online decrypt status={dec.Status} ok={ok}");
            await System.Threading.Tasks.Task.Delay(2000);
            var again = await Net.NoteApi.FetchAndDecrypt(p.Alias, p.Pass);
            Debug.Log($"[DW-TEST] online second read status={again.Status} (expected NotFound)");
            return ok && again.Status == Net.DecryptStatus.NotFound;
        }
    }
}
