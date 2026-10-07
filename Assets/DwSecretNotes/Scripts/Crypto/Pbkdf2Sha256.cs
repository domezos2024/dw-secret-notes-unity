using System;
namespace DwSecretNotes.Crypto
{
    public static class Pbkdf2Sha256
    {
        static readonly uint[] K =
        {
            0x428a2f98,0x71374491,0xb5c0fbcf,0xe9b5dba5,0x3956c25b,0x59f111f1,0x923f82a4,0xab1c5ed5,0xd807aa98,0x12835b01,0x243185be,0x550c7dc3,0x72be5d74,0x80deb1fe,0x9bdc06a7,0xc19bf174,
            0xe49b69c1,0xefbe4786,0x0fc19dc6,0x240ca1cc,0x2de92c6f,0x4a7484aa,0x5cb0a9dc,0x76f988da,0x983e5152,0xa831c66d,0xb00327c8,0xbf597fc7,0xc6e00bf3,0xd5a79147,0x06ca6351,0x14292967,
            0x27b70a85,0x2e1b2138,0x4d2c6dfc,0x53380d13,0x650a7354,0x766a0abb,0x81c2c92e,0x92722c85,0xa2bfe8a1,0xa81a664b,0xc24b8b70,0xc76c51a3,0xd192e819,0xd6990624,0xf40e3585,0x106aa070,
            0x19a4c116,0x1e376c08,0x2748774c,0x34b0bcb5,0x391c0cb3,0x4ed8aa4a,0x5b9cca4f,0x682e6ff3,0x748f82ee,0x78a5636f,0x84c87814,0x8cc70208,0x90befffa,0xa4506ceb,0xbef9a3f7,0xc67178f2
        };
        static readonly uint[] H0 = { 0x6a09e667, 0xbb67ae85, 0x3c6ef372, 0xa54ff53a, 0x510e527f, 0x9b05688c, 0x1f83d9ab, 0x5be0cd19 };
        static uint R(uint x, int n) => (x >> n) | (x << (32 - n));
        static void Compress(uint[] st, uint[] w)
        {
            for (int i = 16; i < 64; i++) { uint s0 = R(w[i - 15], 7) ^ R(w[i - 15], 18) ^ (w[i - 15] >> 3), s1 = R(w[i - 2], 17) ^ R(w[i - 2], 19) ^ (w[i - 2] >> 10); w[i] = w[i - 16] + s0 + w[i - 7] + s1; }
            uint a = st[0], b = st[1], c = st[2], d = st[3], e = st[4], f = st[5], g = st[6], h = st[7];
            for (int i = 0; i < 64; i++)
            {
                uint t1 = h + (R(e, 6) ^ R(e, 11) ^ R(e, 25)) + ((e & f) ^ (~e & g)) + K[i] + w[i], t2 = (R(a, 2) ^ R(a, 13) ^ R(a, 22)) + ((a & b) ^ (a & c) ^ (b & c));
                h = g; g = f; f = e; e = d + t1; d = c; c = b; b = a; a = t1 + t2;
            }
            st[0] += a; st[1] += b; st[2] += c; st[3] += d; st[4] += e; st[5] += f; st[6] += g; st[7] += h;
        }
        static void LoadBlock(byte[] src, int off, uint[] w) { for (int i = 0; i < 16; i++) w[i] = (uint)(src[off + i * 4] << 24 | src[off + i * 4 + 1] << 16 | src[off + i * 4 + 2] << 8 | src[off + i * 4 + 3]); }
        public static byte[] Sha256(byte[] data)
        {
            var st = (uint[])H0.Clone(); var w = new uint[64];
            long bitLen = (long)data.Length * 8; int padLen = ((data.Length + 9 + 63) / 64) * 64;
            var buf = new byte[padLen]; Buffer.BlockCopy(data, 0, buf, 0, data.Length); buf[data.Length] = 0x80;
            for (int i = 0; i < 8; i++) buf[padLen - 1 - i] = (byte)(bitLen >> (8 * i));
            for (int off = 0; off < padLen; off += 64) { LoadBlock(buf, off, w); Compress(st, w); }
            var o = new byte[32]; for (int i = 0; i < 8; i++) { o[i * 4] = (byte)(st[i] >> 24); o[i * 4 + 1] = (byte)(st[i] >> 16); o[i * 4 + 2] = (byte)(st[i] >> 8); o[i * 4 + 3] = (byte)st[i]; }
            return o;
        }
        static uint[] PadState(byte[] key, byte pad)
        {
            var st = (uint[])H0.Clone(); var w = new uint[64]; var blk = new byte[64];
            for (int i = 0; i < 64; i++) blk[i] = (byte)((i < key.Length ? key[i] : 0) ^ pad);
            LoadBlock(blk, 0, w); Compress(st, w); return st;
        }
        static void HashDigest(uint[] padState, uint[] msg8, uint[] outSt, uint[] w)
        {
            Array.Copy(padState, outSt, 8);
            for (int i = 0; i < 8; i++) w[i] = msg8[i];
            w[8] = 0x80000000; for (int i = 9; i < 15; i++) w[i] = 0; w[15] = (64 + 32) * 8;
            Compress(outSt, w);
        }
        public static byte[] Derive(byte[] password, byte[] salt, int iterations, int dkLen)
        {
            if (password.Length > 64) password = Sha256(password);
            uint[] ipad = PadState(password, 0x36), opad = PadState(password, 0x5c), w = new uint[64], inner = new uint[8], u = new uint[8], acc = new uint[8];
            var dk = new byte[dkLen]; int blocks = (dkLen + 31) / 32;
            for (int bi = 1; bi <= blocks; bi++)
            {
                var first = new byte[salt.Length + 4]; Buffer.BlockCopy(salt, 0, first, 0, salt.Length);
                first[salt.Length] = (byte)(bi >> 24); first[salt.Length + 1] = (byte)(bi >> 16); first[salt.Length + 2] = (byte)(bi >> 8); first[salt.Length + 3] = (byte)bi;
                var st = (uint[])ipad.Clone(); long total = 64 + first.Length; int padLen = ((first.Length + 9 + 63) / 64) * 64;
                var buf = new byte[padLen]; Buffer.BlockCopy(first, 0, buf, 0, first.Length); buf[first.Length] = 0x80;
                for (int i = 0; i < 8; i++) buf[padLen - 1 - i] = (byte)((total * 8) >> (8 * i));
                for (int off = 0; off < padLen; off += 64) { LoadBlock(buf, off, w); Compress(st, w); }
                HashDigest(opad, st, u, w);
                Array.Copy(u, acc, 8);
                for (int it = 1; it < iterations; it++)
                {
                    HashDigest(ipad, u, inner, w);
                    HashDigest(opad, inner, u, w);
                    for (int i = 0; i < 8; i++) acc[i] ^= u[i];
                }
                for (int i = 0; i < 32 && (bi - 1) * 32 + i < dkLen; i++) dk[(bi - 1) * 32 + i] = (byte)(acc[i / 4] >> (24 - 8 * (i % 4)));
            }
            return dk;
        }
    }
}
