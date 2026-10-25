using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DwSecretNotes.Business;
using DwSecretNotes.Crypto;
using DwSecretNotes.Data;
using UnityEditor;
using UnityEngine;

namespace DwSecretNotes.EditorTools
{
    /// <summary>Tests the proposed standard crypto against the existing format and the live backend.</summary>
    public static class CryptoBackendCompatibility
    {
        const string Prefix = "[DW-CRYPTO-COMPAT] ";
        static readonly string ReportPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Logs", "crypto-backend-compat.txt");

        static void Log(string message)
        {
            var line = DateTime.UtcNow.ToString("O") + " " + message;
            Debug.Log(Prefix + message);
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.AppendAllText(ReportPath, line + Environment.NewLine);
        }

        static void Require(bool condition, string check)
        {
            Log((condition ? "PASS " : "FAIL ") + check);
            if (!condition) throw new InvalidOperationException(check);
        }

        static byte[] Bytes(int[] values) => values.Select(value => checked((byte)value)).ToArray();
        static int[] Ints(byte[] values) => values.Select(value => (int)value).ToArray();

        static byte[] StandardKey(string password)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(Encoding.UTF8.GetBytes(password), Encoding.UTF8.GetBytes("salt"), 100000, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(32);
        }

        static byte[] StandardEncrypt(byte[] key, byte[] iv, byte[] plain)
        {
            var cipher = new byte[plain.Length];
            var tag = new byte[16];
            using (var aes = new AesGcm(key)) aes.Encrypt(iv, plain, cipher, tag);
            var result = new byte[cipher.Length + tag.Length];
            Buffer.BlockCopy(cipher, 0, result, 0, cipher.Length);
            Buffer.BlockCopy(tag, 0, result, cipher.Length, tag.Length);
            return result;
        }

        static byte[] StandardDecrypt(byte[] key, byte[] iv, byte[] cipherAndTag)
        {
            if (cipherAndTag.Length < 16) throw new CryptographicException("Missing authentication tag");
            var cipher = new byte[cipherAndTag.Length - 16];
            var tag = new byte[16];
            var plain = new byte[cipher.Length];
            Buffer.BlockCopy(cipherAndTag, 0, cipher, 0, cipher.Length);
            Buffer.BlockCopy(cipherAndTag, cipher.Length, tag, 0, tag.Length);
            using (var aes = new AesGcm(key)) aes.Decrypt(iv, cipher, tag, plain);
            return plain;
        }

        static NotePayload StandardPayload(string password, string text, byte[] image)
        {
            var key = NoteCrypto.DeriveKey(password);
            var iv = new byte[12];
            RandomNumberGenerator.Fill(iv);
            var payload = new NotePayload { iv = Ints(iv), data = Ints(StandardEncrypt(key, iv, Encoding.UTF8.GetBytes(text))) };
            if (image != null)
            {
                var imageIv = new byte[12];
                RandomNumberGenerator.Fill(imageIv);
                payload.imgIv = Ints(imageIv);
                payload.imgData = Ints(StandardEncrypt(key, imageIv, image));
            }
            return payload;
        }

        static bool Equal(NotePayload left, NotePayload right) =>
            left != null && right != null && left.iv.SequenceEqual(right.iv) && left.data.SequenceEqual(right.data) &&
            (left.imgIv == null ? right.imgIv == null : right.imgIv != null && left.imgIv.SequenceEqual(right.imgIv)) &&
            (left.imgData == null ? right.imgData == null : right.imgData != null && left.imgData.SequenceEqual(right.imgData));

        static string Alias() => SecretNotesService.Timestamp(DateTime.Now);

        static async Task CheckBackend(INoteRepository repository, bool standardWrites)
        {
            var alias = Alias();
            var password = NoteCrypto.GeneratePassword();
            var text = "crypto compatibility " + Guid.NewGuid().ToString("N");
            var image = Encoding.UTF8.GetBytes("image compatibility " + Guid.NewGuid().ToString("N"));
            var payload = standardWrites ? StandardPayload(password, text, image) : NoteCrypto.Encrypt(password, text, image);
            try
            {
                Require(await repository.Save(alias, NoteCrypto.ToJson(payload)) == NoteSaveStatus.Saved,
                    standardWrites ? "backend accepts standard payload" : "backend accepts existing payload");
                var fetched = await repository.Fetch(alias);
                Require(fetched.Status == NoteFetchStatus.Found && Equal(payload, fetched.Payload),
                    standardWrites ? "backend preserves standard payload" : "backend preserves existing payload");
                var effectivePassword = string.IsNullOrEmpty(fetched.PassOverride) ? password : fetched.PassOverride;
                if (standardWrites)
                {
                    var result = NoteCrypto.Decrypt(fetched.Payload, effectivePassword);
                    Require(result.Text == text && result.Image.SequenceEqual(image), "existing crypto decrypts standard payload from backend");
                }
                else
                {
                    var key = NoteCrypto.DeriveKey(effectivePassword);
                    var decodedText = Encoding.UTF8.GetString(StandardDecrypt(key, Bytes(fetched.Payload.iv), Bytes(fetched.Payload.data)));
                    var decodedImage = StandardDecrypt(key, Bytes(fetched.Payload.imgIv), Bytes(fetched.Payload.imgData));
                    Require(decodedText == text && decodedImage.SequenceEqual(image), "standard crypto decrypts existing payload from backend");
                }
            }
            finally
            {
                await repository.Unlink(alias);
                var remaining = await repository.Fetch(alias);
                Require(remaining.Status == NoteFetchStatus.NotFound, "test payload removed from backend");
            }
        }

        static async Task<bool> Run()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, string.Empty);
            Log("BEGIN Unity crypto/backend compatibility test");
            try
            {
                var password = "1671330142412131832322139820315246110200182";
                var existingKey = NoteCrypto.DeriveKey(password);
                byte[] standardKey = null;
                try { standardKey = StandardKey(password); }
                catch (ArgumentException error) { Log("FAIL standard PBKDF2 rejects legacy four-byte salt: " + error.Message); }
                var standardPbkdf2Works = standardKey != null && existingKey.SequenceEqual(standardKey);
                Log((standardPbkdf2Works ? "PASS " : "FAIL ") + "PBKDF2 standard replacement matches existing implementation");
                var iv = Enumerable.Range(0, 12).Select(i => (byte)i).ToArray();
                var plain = Encoding.UTF8.GetBytes("Grüße 秘密 ✓ test");
                byte[] existingCipher;
                using (var existing = new AesGcm256(existingKey)) existingCipher = existing.Encrypt(iv, plain);
                var standardCipher = StandardEncrypt(existingKey, iv, plain);
                Require(existingCipher.SequenceEqual(standardCipher), "AES-GCM ciphertext and tag match existing implementation");
                Require(StandardDecrypt(existingKey, iv, existingCipher).SequenceEqual(plain), "standard AES-GCM decrypts existing vector");
                var modified = (byte[])standardCipher.Clone();
                modified[0] ^= 1;
                var rejectsTampering = false;
                try { StandardDecrypt(existingKey, iv, modified); }
                catch (CryptographicException) { rejectsTampering = true; }
                Require(rejectsTampering, "standard AES-GCM rejects tampered ciphertext");
                var repository = new HttpNoteRepository();
                await CheckBackend(repository, true);
                await CheckBackend(repository, false);
                Log(standardPbkdf2Works ? "RESULT PASS" : "RESULT FAIL: PBKDF2 standard replacement unavailable for legacy salt");
                return standardPbkdf2Works;
            }
            catch (Exception error)
            {
                Log("RESULT FAIL " + error.GetType().Name + ": " + error.Message);
                return false;
            }
        }

        [MenuItem("dw Secret Notes/Krypto-Backend-Kompatibilität", priority = 42)]
        public static void RunMenu() { _ = Run(); }

        public static void RunBatch()
        {
            var task = Run();
            var started = DateTime.UtcNow;
            void Poll()
            {
                if (!task.IsCompleted && (DateTime.UtcNow - started).TotalSeconds < 180) return;
                EditorApplication.update -= Poll;
                var passed = task.IsCompletedSuccessfully && task.Result;
                if (!task.IsCompleted) Log("RESULT FAIL timeout");
                EditorApplication.Exit(passed ? 0 : 1);
            }
            EditorApplication.update += Poll;
        }
    }
}
