using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DwSecretNotes.Crypto;

var report = new List<string>();
void Check(string name, bool passed)
{
    var line = $"{(passed ? "PASS" : "FAIL")} {name}";
    report.Add(line);
    Console.WriteLine(line);
    if (!passed) throw new InvalidOperationException(name);
}

var apiKey = Environment.GetEnvironmentVariable("DW_SECRET_NOTES_API_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine("Set DW_SECRET_NOTES_API_KEY for the live backend test.");
    return 2;
}

var endpoint = new Uri("https://domezos-ware.com/api/msg_store.php");
using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(35) };
client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

static byte[] DeriveStandard(string pass) => Rfc2898DeriveBytes.Pbkdf2(
    Encoding.UTF8.GetBytes(pass), Encoding.UTF8.GetBytes("salt"), 100000, HashAlgorithmName.SHA256, 32);
static byte[] DeriveLegacy(string pass) => Pbkdf2Sha256.Derive(
    Encoding.UTF8.GetBytes(pass), Encoding.UTF8.GetBytes("salt"), 100000, 32);
static int[] ToInts(byte[] data) => data.Select(x => (int)x).ToArray();
static byte[] ToBytes(int[] data) => data.Select(x => checked((byte)x)).ToArray();
static byte[] EncryptStandard(byte[] key, byte[] iv, byte[] plain)
{
    var cipher = new byte[plain.Length];
    var tag = new byte[16];
    using var aes = new AesGcm(key, 16);
    aes.Encrypt(iv, plain, cipher, tag);
    return [.. cipher, .. tag];
}
static byte[] DecryptStandard(byte[] key, byte[] iv, byte[] joined)
{
    if (joined.Length < 16) throw new CryptographicException("Missing tag");
    var plain = new byte[joined.Length - 16];
    using var aes = new AesGcm(key, 16);
    aes.Decrypt(iv, joined.AsSpan(0, plain.Length), joined.AsSpan(plain.Length, 16), plain);
    return plain;
}
static string Alias() => DateTime.Now.ToString("dd.MM.yyyy_HH-mm-ss-fff", System.Globalization.CultureInfo.InvariantCulture);
static string Password() => string.Concat(RandomNumberGenerator.GetBytes(16).Select(x => x.ToString(System.Globalization.CultureInfo.InvariantCulture)));
static Payload Encrypt(string pass, string text, byte[] image, bool standard)
{
    var key = standard ? DeriveStandard(pass) : DeriveLegacy(pass);
    var iv = RandomNumberGenerator.GetBytes(12);
    var imageIv = RandomNumberGenerator.GetBytes(12);
    byte[] Run(byte[] nonce, byte[] plain)
    {
        if (standard) return EncryptStandard(key, nonce, plain);
        using var aes = new AesGcm256(key);
        return aes.Encrypt(nonce, plain);
    }
    return new Payload(ToInts(iv), ToInts(Run(iv, Encoding.UTF8.GetBytes(text))),
        ToInts(imageIv), ToInts(Run(imageIv, image)));
}
static (string Text, byte[] Image) Decrypt(Payload payload, string pass, bool standard)
{
    var key = standard ? DeriveStandard(pass) : DeriveLegacy(pass);
    byte[] Run(int[] nonce, int[] joined)
    {
        if (standard) return DecryptStandard(key, ToBytes(nonce), ToBytes(joined));
        using var aes = new AesGcm256(key);
        return aes.Decrypt(ToBytes(nonce), ToBytes(joined));
    }
    return (Encoding.UTF8.GetString(Run(payload.Iv, payload.Data)), Run(payload.ImageIv, payload.ImageData));
}

async Task CheckBackend(bool standardWrites)
{
    var alias = Alias();
    var password = Password();
    var text = "crypto compatibility " + Guid.NewGuid().ToString("N");
    var image = Encoding.UTF8.GetBytes("image compatibility " + Guid.NewGuid().ToString("N"));
    var payload = Encrypt(password, text, image, standardWrites);
    var action = standardWrites ? "standard-to-legacy" : "legacy-to-standard";
    try
    {
        var saveUrl = new Uri(endpoint + "?action=save&ts=" + Uri.EscapeDataString(alias));
        using var saveResponse = await client.PostAsJsonAsync(saveUrl, payload);
        Check(action + " backend save", saveResponse.IsSuccessStatusCode);

        var getUrl = new Uri(endpoint + "?action=get&com=" + Uri.EscapeDataString(alias));
        using var getResponse = await client.GetAsync(getUrl);
        Check(action + " backend fetch", getResponse.IsSuccessStatusCode);
        using var document = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        var root = document.RootElement;
        Check(action + " backend status", root.GetProperty("status").GetString() != "not_found");
        var fetched = root.GetProperty("payload").Deserialize<Payload>();
        Check(action + " payload unchanged", fetched != null && payload.SameAs(fetched));
        var overridePass = root.TryGetProperty("pass_override", out var overrideElement) && overrideElement.ValueKind == JsonValueKind.String
            ? overrideElement.GetString() : null;
        var decrypted = Decrypt(fetched!, string.IsNullOrEmpty(overridePass) ? password : overridePass, !standardWrites);
        Check(action + " decrypt text and image", decrypted.Text == text && decrypted.Image.SequenceEqual(image));
    }
    finally
    {
        var unlinkUrl = new Uri(endpoint + "?action=unlink&com=" + Uri.EscapeDataString(alias));
        using var unlinkResponse = await client.GetAsync(unlinkUrl);
        Check(action + " cleanup request", unlinkResponse.IsSuccessStatusCode);
        using var verifyResponse = await client.GetAsync(new Uri(endpoint + "?action=get&com=" + Uri.EscapeDataString(alias)));
        using var verifyDoc = JsonDocument.Parse(await verifyResponse.Content.ReadAsStringAsync());
        Check(action + " cleanup verified", verifyResponse.IsSuccessStatusCode &&
            verifyDoc.RootElement.GetProperty("status").GetString() == "not_found");
    }
}

try
{
    var password = "1671330142412131832322139820315246110200182";
    var standardKey = DeriveStandard(password);
    var legacyKey = DeriveLegacy(password);
    Check("PBKDF2 legacy salt and parameters", standardKey.SequenceEqual(legacyKey));
    var iv = Enumerable.Range(0, 12).Select(x => (byte)x).ToArray();
    var plain = Encoding.UTF8.GetBytes("Grüße 秘密 ✓ test");
    byte[] oldCipher;
    using (var aes = new AesGcm256(legacyKey)) oldCipher = aes.Encrypt(iv, plain);
    var newCipher = EncryptStandard(standardKey, iv, plain);
    Check("AES-GCM ciphertext and tag", oldCipher.SequenceEqual(newCipher));
    Check("AES-GCM decrypt existing vector", DecryptStandard(standardKey, iv, oldCipher).SequenceEqual(plain));
    await CheckBackend(true);
    await Task.Delay(10);
    await CheckBackend(false);
    report.Add("RESULT PASS");
    Console.WriteLine("RESULT PASS");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine("RESULT FAIL " + error.GetType().Name + ": " + error.Message);
    report.Add("RESULT FAIL " + error.GetType().Name + ": " + error.Message);
    return 1;
}
finally
{
    var outputPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../Logs/crypto-backend-dotnet.txt"));
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    File.WriteAllLines(outputPath, report);
}

internal sealed record Payload(
    [property: JsonPropertyName("iv")] int[] Iv,
    [property: JsonPropertyName("data")] int[] Data,
    [property: JsonPropertyName("imgIv")] int[] ImageIv,
    [property: JsonPropertyName("imgData")] int[] ImageData)
{
    public bool SameAs(Payload other) => Iv.SequenceEqual(other.Iv) && Data.SequenceEqual(other.Data) &&
        ImageIv.SequenceEqual(other.ImageIv) && ImageData.SequenceEqual(other.ImageData);
}
