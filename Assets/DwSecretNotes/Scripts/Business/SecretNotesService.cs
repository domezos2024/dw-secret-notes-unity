using System;
using System.Threading;
using System.Threading.Tasks;
using DwSecretNotes.Core;
using DwSecretNotes.Crypto;

namespace DwSecretNotes.Business
{
    public enum NoteSaveStatus { Saved, RateLimited, Failed }
    public enum NoteFetchStatus { Found, NotFound, Failed }
    public enum DecryptStatus { Ok, NotFound, Failed }

    public sealed class NoteFetchResult
    {
        public NoteFetchStatus Status;
        public NotePayload Payload;
        public string PassOverride;
        public string Alias;
    }

    public sealed class EncryptResult
    {
        public bool Ok;
        public string Link;
        public string Error;
    }

    public sealed class DecryptResult
    {
        public DecryptStatus Status;
        public SecretNote Note;
    }

    public sealed class SecretNote
    {
        public string Text;
        public byte[] Image;
    }

    public interface INoteRepository
    {
        Task<NoteSaveStatus> Save(string timestamp, string payload, CancellationToken cancellationToken = default);
        Task<NoteFetchResult> Fetch(string alias, CancellationToken cancellationToken = default);
        Task Unlink(string alias, CancellationToken cancellationToken = default);
        Task<ParsedLink?> ResolveLink(string input, CancellationToken cancellationToken = default);
    }

    public interface IAppPreferences
    {
        string Language { get; set; }
        string Theme { get; set; }
        bool HasRatedApp { get; set; }
        bool HasSeenRatePrompt { get; set; }
        int IncrementRunCount();
    }

    public sealed class SecretNotesService
    {
        const string Module = "SecretNotes";
        readonly INoteRepository repository;

        public SecretNotesService(INoteRepository repository)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public static string Timestamp(DateTime now) =>
            now.ToString("dd.MM.yyyy_HH-mm-ss-", System.Globalization.CultureInfo.InvariantCulture) + now.Millisecond.ToString("000");

        public static string BuildLink(string timestamp, string pass) =>
            $"{AppInfo.BaseUrl}/msges/view.php?com={Uri.EscapeDataString(timestamp)}&pass={Uri.EscapeDataString(pass)}";

        public async Task<EncryptResult> EncryptAndStore(string text, byte[] image, CancellationToken cancellationToken = default)
        {
            DwLog.Call(Module);
            cancellationToken.ThrowIfCancellationRequested();
            var pass = NoteCrypto.GeneratePassword();
            string payload;
            try { payload = await Task.Run(() => NoteCrypto.ToJson(NoteCrypto.Encrypt(pass, text, image)), cancellationToken); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception e)
            {
                DwLog.E(Module, "encrypt failed", e);
                return new EncryptResult { Error = L10n.T("error_encrypt_failed") };
            }

            cancellationToken.ThrowIfCancellationRequested();
            var timestamp = Timestamp(DateTime.Now);
            var saved = await repository.Save(timestamp, payload, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (saved != NoteSaveStatus.Saved)
                return new EncryptResult { Error = L10n.T(saved == NoteSaveStatus.RateLimited ? "error_rate_limit" : "error_encrypt_network") };
            return new EncryptResult { Ok = true, Link = BuildLink(timestamp, pass) };
        }

        public async Task<DecryptResult> FetchAndDecrypt(string alias, string pass, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fetched = await repository.Fetch(alias, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (fetched.Status != NoteFetchStatus.Found)
                return new DecryptResult { Status = fetched.Status == NoteFetchStatus.NotFound ? DecryptStatus.NotFound : DecryptStatus.Failed };

            var finalPass = string.IsNullOrEmpty(fetched.PassOverride) ? pass : fetched.PassOverride;
            DecryptedNote note;
            try { note = await Task.Run(() => NoteCrypto.Decrypt(fetched.Payload, finalPass), cancellationToken); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception e)
            {
                DwLog.W(Module, "decrypt failed: " + e.Message);
                return new DecryptResult { Status = DecryptStatus.NotFound };
            }
            cancellationToken.ThrowIfCancellationRequested();
            // Keep deletion verification independent of the 60-second local display.
            _ = VerifyRemovalAsync(fetched.Alias);
            return new DecryptResult { Status = DecryptStatus.Ok, Note = new SecretNote { Text = note.Text, Image = note.Image } };
        }

        async Task VerifyRemovalAsync(string alias)
        {
            try
            {
                DwLog.I(Module, "unlink after successful decryption");
                await repository.Unlink(alias, CancellationToken.None);
                var verification = await repository.Fetch(alias, CancellationToken.None);
                if (verification.Status == NoteFetchStatus.NotFound)
                    DwLog.I(Module, "removal verified");
                else
                    DwLog.W(Module, "removal verification status: " + verification.Status);
            }
            catch (Exception e)
            {
                DwLog.E(Module, "removal verification failed", e);
            }
        }

        public async Task<DecryptResult> DecryptLink(string input, string passOverride = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var parsed = await repository.ResolveLink(input, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!parsed.HasValue)
                return new DecryptResult { Status = DecryptStatus.NotFound };

            var pass = string.IsNullOrEmpty(parsed.Value.Pass)
                ? (string.IsNullOrEmpty(passOverride) ? AppInfo.FallbackPassphrase : passOverride)
                : parsed.Value.Pass;
            return await FetchAndDecrypt(parsed.Value.Alias, pass, cancellationToken);
        }

        public Task<ParsedLink?> ResolveLink(string input, CancellationToken cancellationToken = default) => repository.ResolveLink(input, cancellationToken);
    }
}
