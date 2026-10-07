using System;
using System.Security.Cryptography;
using System.Text;
using DwSecretNotes.Core;
namespace DwSecretNotes.Crypto
{
    [Serializable] public sealed class NotePayload { public int[] iv; public int[] data; public int[] imgIv; public int[] imgData; }
    public sealed class DecryptedNote { public string Text; public byte[] Image; }
    public static class NoteCrypto
    {
        const int Iterations = 100000;
        static readonly byte[] Salt = Encoding.UTF8.GetBytes("salt");
        static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();
        static byte[] Random(int n) { var b = new byte[n]; lock (Rng) Rng.GetBytes(b); return b; }
        public static string GeneratePassword() { var b = Random(16); var sb = new StringBuilder(48); foreach (var x in b) sb.Append(x); return sb.ToString(); }
        public static byte[] DeriveKey(string pass) => Pbkdf2Sha256.Derive(Encoding.UTF8.GetBytes(pass), Salt, Iterations, 32);
        static int[] ToInts(byte[] b) { var r = new int[b.Length]; for (int i = 0; i < b.Length; i++) r[i] = b[i]; return r; }
        static byte[] ToBytes(int[] a) { var r = new byte[a.Length]; for (int i = 0; i < a.Length; i++) r[i] = (byte)a[i]; return r; }
        public static NotePayload Encrypt(string pass, string text, byte[] image)
        {
            DwLog.I("Crypto", $"encrypt textLen={text.Length} imageBytes={(image?.Length ?? 0)}");
            using var gcm = new AesGcm256(DeriveKey(pass));
            var iv = Random(12);
            var p = new NotePayload { iv = ToInts(iv), data = ToInts(gcm.Encrypt(iv, Encoding.UTF8.GetBytes(text))) };
            if (image != null && image.Length > 0)
            {
                try { var imgIv = Random(12); p.imgIv = ToInts(imgIv); p.imgData = ToInts(gcm.Encrypt(imgIv, image)); }
                catch (Exception e) { p.imgIv = null; p.imgData = null; DwLog.W("Crypto", "image encrypt failed: " + e.Message); }
            }
            return p;
        }
        public static DecryptedNote Decrypt(NotePayload p, string pass)
        {
            if (p == null || p.iv == null || p.iv.Length == 0 || p.data == null) throw new CryptographicException("no payload");
            using var gcm = new AesGcm256(DeriveKey(pass));
            var note = new DecryptedNote { Text = Encoding.UTF8.GetString(gcm.Decrypt(ToBytes(p.iv), ToBytes(p.data))) };
            if (p.imgIv != null && p.imgIv.Length > 0 && p.imgData != null && p.imgData.Length > 0)
            {
                try { note.Image = gcm.Decrypt(ToBytes(p.imgIv), ToBytes(p.imgData)); }
                catch (Exception e) { DwLog.W("Crypto", "image decrypt failed: " + e.Message); }
            }
            DwLog.I("Crypto", $"decrypt ok textLen={note.Text.Length} imageBytes={(note.Image?.Length ?? 0)}");
            return note;
        }
        public static string ToJson(NotePayload p)
        {
            var sb = new StringBuilder(64 + (p.data.Length + (p.imgData?.Length ?? 0)) * 4);
            sb.Append('{'); Arr(sb, "iv", p.iv); sb.Append(','); Arr(sb, "data", p.data);
            if (p.imgIv != null && p.imgData != null) { sb.Append(','); Arr(sb, "imgIv", p.imgIv); sb.Append(','); Arr(sb, "imgData", p.imgData); }
            return sb.Append('}').ToString();
        }
        static void Arr(StringBuilder sb, string name, int[] a)
        {
            sb.Append('"').Append(name).Append("\":[");
            for (int i = 0; i < a.Length; i++) { if (i > 0) sb.Append(','); sb.Append(a[i]); }
            sb.Append(']');
        }
    }
}
