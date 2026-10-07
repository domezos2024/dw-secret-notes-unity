using System;
using System.Security.Cryptography;
namespace DwSecretNotes.Crypto
{
    public sealed class AesGcm256 : IDisposable
    {
        public const int TagSize = 16, IvSize = 12;
        readonly Aes aes; readonly ICryptoTransform enc; readonly ulong hHi, hLo;
        public AesGcm256(byte[] key)
        {
            if (key == null || key.Length != 32) throw new ArgumentException("key must be 32 bytes");
            aes = Aes.Create(); aes.Mode = CipherMode.ECB; aes.Padding = PaddingMode.None; aes.Key = key;
            enc = aes.CreateEncryptor();
            var h = new byte[16]; enc.TransformBlock(new byte[16], 0, 16, h, 0);
            hHi = ReadU64(h, 0); hLo = ReadU64(h, 8);
        }
        static ulong ReadU64(byte[] b, int o) { ulong v = 0; for (int i = 0; i < 8; i++) v = (v << 8) | b[o + i]; return v; }
        static void WriteU64(byte[] b, int o, ulong v) { for (int i = 7; i >= 0; i--) { b[o + i] = (byte)v; v >>= 8; } }
        void GfMul(ref ulong xHi, ref ulong xLo)
        {
            ulong zHi = 0, zLo = 0, vHi = hHi, vLo = hLo;
            for (int i = 0; i < 128; i++)
            {
                ulong bit = i < 64 ? (xHi >> (63 - i)) & 1 : (xLo >> (127 - i)) & 1;
                if (bit != 0) { zHi ^= vHi; zLo ^= vLo; }
                bool lsb = (vLo & 1) != 0;
                vLo = (vLo >> 1) | (vHi << 63); vHi >>= 1;
                if (lsb) vHi ^= 0xE100000000000000UL;
            }
            xHi = zHi; xLo = zLo;
        }
        byte[] Ghash(byte[] c, int len)
        {
            ulong yHi = 0, yLo = 0; var blk = new byte[16];
            for (int off = 0; off < len; off += 16)
            {
                int n = Math.Min(16, len - off); Array.Clear(blk, 0, 16); Buffer.BlockCopy(c, off, blk, 0, n);
                yHi ^= ReadU64(blk, 0); yLo ^= ReadU64(blk, 8); GfMul(ref yHi, ref yLo);
            }
            yLo ^= (ulong)len * 8; GfMul(ref yHi, ref yLo);
            var o = new byte[16]; WriteU64(o, 0, yHi); WriteU64(o, 8, yLo); return o;
        }
        void Ctr(byte[] iv, byte[] input, int len, byte[] output)
        {
            int blocks = (len + 15) / 16; if (blocks == 0) return;
            var ctr = new byte[blocks * 16]; var ks = new byte[blocks * 16];
            for (int b = 0; b < blocks; b++) { Buffer.BlockCopy(iv, 0, ctr, b * 16, 12); uint n = (uint)(b + 2); ctr[b * 16 + 12] = (byte)(n >> 24); ctr[b * 16 + 13] = (byte)(n >> 16); ctr[b * 16 + 14] = (byte)(n >> 8); ctr[b * 16 + 15] = (byte)n; }
            enc.TransformBlock(ctr, 0, ctr.Length, ks, 0);
            for (int i = 0; i < len; i++) output[i] = (byte)(input[i] ^ ks[i]);
        }
        byte[] TagMask(byte[] iv) { var j0 = new byte[16]; Buffer.BlockCopy(iv, 0, j0, 0, 12); j0[15] = 1; var m = new byte[16]; enc.TransformBlock(j0, 0, 16, m, 0); return m; }
        public byte[] Encrypt(byte[] iv, byte[] plain)
        {
            if (iv.Length != IvSize) throw new ArgumentException("iv must be 12 bytes");
            var outp = new byte[plain.Length + TagSize];
            Ctr(iv, plain, plain.Length, outp);
            var s = Ghash(outp, plain.Length); var m = TagMask(iv);
            for (int i = 0; i < 16; i++) outp[plain.Length + i] = (byte)(s[i] ^ m[i]);
            return outp;
        }
        public byte[] Decrypt(byte[] iv, byte[] cipherWithTag)
        {
            if (iv.Length != IvSize || cipherWithTag.Length < TagSize) throw new CryptographicException("invalid input");
            int len = cipherWithTag.Length - TagSize;
            var s = Ghash(cipherWithTag, len); var m = TagMask(iv); int diff = 0;
            for (int i = 0; i < 16; i++) diff |= (s[i] ^ m[i]) ^ cipherWithTag[len + i];
            if (diff != 0) throw new CryptographicException("authentication tag mismatch");
            var plain = new byte[len]; Ctr(iv, cipherWithTag, len, plain); return plain;
        }
        public void Dispose() { enc.Dispose(); aes.Dispose(); }
    }
}
