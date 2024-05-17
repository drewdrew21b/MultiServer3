﻿using System;
using System.Diagnostics;

using Org.BouncyCastle.Crypto.Utilities;
using Org.BouncyCastle.Math.Raw;
using Org.BouncyCastle.Security;

namespace Org.BouncyCastle.Math.EC.Custom.Sec
{
    internal class SecP256R1Field
    {
        // 2^256 - 2^224 + 2^192 + 2^96 - 1
        internal static readonly uint[] P = new uint[]{ 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0x00000000, 0x00000000,
            0x00000000, 0x00000001, 0xFFFFFFFF };
        private static readonly uint[] PExt = new uint[]{ 0x00000001, 0x00000000, 0x00000000, 0xFFFFFFFE, 0xFFFFFFFF,
            0xFFFFFFFF, 0xFFFFFFFE, 0x00000001, 0xFFFFFFFE, 0x00000001, 0xFFFFFFFE, 0x00000001, 0x00000001, 0xFFFFFFFE,
            0x00000002, 0xFFFFFFFE };
        private const uint P7 = 0xFFFFFFFF;
        private const uint PExt15 = 0xFFFFFFFE;

        public static void Add(uint[] x, uint[] y, uint[] z)
        {
            uint c = Nat256.Add(x, y, z);
            if (c != 0 || (z[7] == P7 && Nat256.Gte(z, P)))
            {
                AddPInvTo(z);
            }
        }

        public static void AddExt(uint[] xx, uint[] yy, uint[] zz)
        {
            uint c = Nat.Add(16, xx, yy, zz);
            if (c != 0 || (zz[15] >= PExt15 && Nat.Gte(16, zz, PExt)))
            {
                Nat.SubFrom(16, PExt, zz);
            }
        }

        public static void AddOne(uint[] x, uint[] z)
        {
            uint c = Nat.Inc(8, x, z);
            if (c != 0 || (z[7] == P7 && Nat256.Gte(z, P)))
            {
                AddPInvTo(z);
            }
        }

        public static uint[] FromBigInteger(BigInteger x)
        {
            uint[] z = Nat.FromBigInteger(256, x);
            if (z[7] == P7 && Nat256.Gte(z, P))
            {
                Nat256.SubFrom(P, z, 0);
            }
            return z;
        }

        public static void Half(uint[] x, uint[] z)
        {
            if ((x[0] & 1) == 0)
            {
                Nat.ShiftDownBit(8, x, 0, z);
            }
            else
            {
                uint c = Nat256.Add(x, P, z);
                Nat.ShiftDownBit(8, z, c);
            }
        }

        public static void Inv(uint[] x, uint[] z)
        {
            Mod.CheckedModOddInverse(P, x, z);
        }

        public static int IsZero(uint[] x)
        {
            uint d = 0;
            for (int i = 0; i < 8; ++i)
            {
                d |= x[i];
            }
            d = (d >> 1) | (d & 1);
            return ((int)d - 1) >> 31;
        }

        public static void Multiply(uint[] x, uint[] y, uint[] z)
        {
            uint[] tt = Nat256.CreateExt();
            Nat256.Mul(x, y, tt);
            Reduce(tt, z);
        }

        public static void Multiply(uint[] x, uint[] y, uint[] z, uint[] tt)
        {
            Nat256.Mul(x, y, tt);
            Reduce(tt, z);
        }

        public static void MultiplyAddToExt(uint[] x, uint[] y, uint[] zz)
        {
            uint c = Nat256.MulAddTo(x, y, zz);
            if (c != 0 || (zz[15] >= PExt15 && Nat.Gte(16, zz, PExt)))
            {
                Nat.SubFrom(16, PExt, zz);
            }
        }

        public static void Negate(uint[] x, uint[] z)
        {
            if (0 != IsZero(x))
            {
                Nat256.Sub(P, P, z);
            }
            else
            {
                Nat256.Sub(P, x, z);
            }
        }

        public static void Random(SecureRandom r, uint[] z)
        {
            byte[] bb = new byte[8 * 4];
            do
            {
                r.NextBytes(bb);
                Pack.LE_To_UInt32(bb, 0, z, 0, 8);
            }
            while (0 == Nat.LessThan(8, z, P));
        }

        public static void RandomMult(SecureRandom r, uint[] z)
        {
            do
            {
                Random(r, z);
            }
            while (0 != IsZero(z));
        }

        public static void Reduce(uint[] xx, uint[] z)
        {
            long xx08 = xx[8], xx09 = xx[9], xx10 = xx[10], xx11 = xx[11];
            long xx12 = xx[12], xx13 = xx[13], xx14 = xx[14], xx15 = xx[15];

            const long n = 6;

            xx08 -= n;

            long t0 = xx08 + xx09;
            long t1 = xx09 + xx10;
            long t2 = xx10 + xx11 - xx15;
            long t3 = xx11 + xx12;
            long t4 = xx12 + xx13;
            long t5 = xx13 + xx14;
            long t6 = xx14 + xx15;
            long t7 = t5 - t0;

            long cc = 0;
            cc += (long)xx[0] - t3 - t7;
            z[0] = (uint)cc;
            cc >>= 32;
            cc += (long)xx[1] + t1 - t4 - t6;
            z[1] = (uint)cc;
            cc >>= 32;
            cc += (long)xx[2] + t2 - t5;
            z[2] = (uint)cc;
            cc >>= 32;
            cc += (long)xx[3] + (t3 << 1) + t7 - t6;
            z[3] = (uint)cc;
            cc >>= 32;
            cc += (long)xx[4] + (t4 << 1) + xx14 - t1;
            z[4] = (uint)cc;
            cc >>= 32;
            cc += (long)xx[5] + (t5 << 1) - t2;
            z[5] = (uint)cc;
            cc >>= 32;
            cc += (long)xx[6] + (t6 << 1) + t7;
            z[6] = (uint)cc;
            cc >>= 32;
            cc += (long)xx[7] + (xx15 << 1) + xx08 - t2 - t4;
            z[7] = (uint)cc;
            cc >>= 32;
            cc += n;

            Debug.Assert(cc >= 0);

            Reduce32((uint)cc, z);
        }

        public static void Reduce32(uint x, uint[] z)
        {
            long cc = 0;

            if (x != 0)
            {
                long xx08 = x;

                cc += (long)z[0] + xx08;
                z[0] = (uint)cc;
                cc >>= 32;
                if (cc != 0)
                {
                    cc += (long)z[1];
                    z[1] = (uint)cc;
                    cc >>= 32;
                    cc += (long)z[2];
                    z[2] = (uint)cc;
                    cc >>= 32;
                }
                cc += (long)z[3] - xx08;
                z[3] = (uint)cc;
                cc >>= 32;
                if (cc != 0)
                {
                    cc += (long)z[4];
                    z[4] = (uint)cc;
                    cc >>= 32;
                    cc += (long)z[5];
                    z[5] = (uint)cc;
                    cc >>= 32;
                }
                cc += (long)z[6] - xx08;
                z[6] = (uint)cc;
                cc >>= 32;
                cc += (long)z[7] + xx08;
                z[7] = (uint)cc;
                cc >>= 32;

                Debug.Assert(cc == 0 || cc == 1);
            }

            if (cc != 0 || (z[7] == P7 && Nat256.Gte(z, P)))
            {
                AddPInvTo(z);
            }
        }

        public static void Square(uint[] x, uint[] z)
        {
            uint[] tt = Nat256.CreateExt();
            Nat256.Square(x, tt);
            Reduce(tt, z);
        }

        public static void Square(uint[] x, uint[] z, uint[] tt)
        {
            Nat256.Square(x, tt);
            Reduce(tt, z);
        }

        public static void SquareN(uint[] x, int n, uint[] z)
        {
            Debug.Assert(n > 0);

            uint[] tt = Nat256.CreateExt();
            Nat256.Square(x, tt);
            Reduce(tt, z);

            while (--n > 0)
            {
                Nat256.Square(z, tt);
                Reduce(tt, z);
            }
        }

        public static void SquareN(uint[] x, int n, uint[] z, uint[] tt)
        {
            Debug.Assert(n > 0);

            Nat256.Square(x, tt);
            Reduce(tt, z);

            while (--n > 0)
            {
                Nat256.Square(z, tt);
                Reduce(tt, z);
            }
        }

        public static void Subtract(uint[] x, uint[] y, uint[] z)
        {
            int c = Nat256.Sub(x, y, z);
            if (c != 0)
            {
                SubPInvFrom(z);
            }
        }

        public static void SubtractExt(uint[] xx, uint[] yy, uint[] zz)
        {
            int c = Nat.Sub(16, xx, yy, zz);
            if (c != 0)
            {
                Nat.AddTo(16, PExt, zz);
            }
        }

        public static void Twice(uint[] x, uint[] z)
        {
            uint c = Nat.ShiftUpBit(8, x, 0, z);
            if (c != 0 || (z[7] == P7 && Nat256.Gte(z, P)))
            {
                AddPInvTo(z);
            }
        }

        private static void AddPInvTo(uint[] z)
        {
            long c = (long)z[0] + 1;
            z[0] = (uint)c;
            c >>= 32;
            if (c != 0)
            {
                c += (long)z[1];
                z[1] = (uint)c;
                c >>= 32;
                c += (long)z[2];
                z[2] = (uint)c;
                c >>= 32;
            }
            c += (long)z[3] - 1;
            z[3] = (uint)c;
            c >>= 32;
            if (c != 0)
            {
                c += (long)z[4];
                z[4] = (uint)c;
                c >>= 32;
                c += (long)z[5];
                z[5] = (uint)c;
                c >>= 32;
            }
            c += (long)z[6] - 1;
            z[6] = (uint)c;
            c >>= 32;
            c += (long)z[7] + 1;
            z[7] = (uint)c;
            //c >>= 32;
        }

        private static void SubPInvFrom(uint[] z)
        {
            long c = (long)z[0] - 1;
            z[0] = (uint)c;
            c >>= 32;
            if (c != 0)
            {
                c += (long)z[1];
                z[1] = (uint)c;
                c >>= 32;
                c += (long)z[2];
                z[2] = (uint)c;
                c >>= 32;
            }
            c += (long)z[3] + 1;
            z[3] = (uint)c;
            c >>= 32;
            if (c != 0)
            {
                c += (long)z[4];
                z[4] = (uint)c;
                c >>= 32;
                c += (long)z[5];
                z[5] = (uint)c;
                c >>= 32;
            }
            c += (long)z[6] + 1;
            z[6] = (uint)c;
            c >>= 32;
            c += (long)z[7] - 1;
            z[7] = (uint)c;
            //c >>= 32;
        }
    }
}
