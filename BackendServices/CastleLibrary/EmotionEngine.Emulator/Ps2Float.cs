﻿using System;

namespace EmotionEngine.Emulator
{
    // Adapted from: https://github.com/gregorygaines/ps2-floating-point-rs
    public class Ps2Float : IComparable<Ps2Float>
    {
        public bool Sign { get; private set; }
        public byte Exponent { get; private set; }
        public uint Mantissa { get; private set; }

        public const byte BIAS = 127;
        public const uint SIGNMASK = 0x80000000;
        public const uint MAX_FLOATING_POINT_VALUE = 0x7FFFFFFF;
        public const uint MIN_FLOATING_POINT_VALUE = 0xFFFFFFFF;
        public const uint POSITIVE_INFINITY_VALUE = 0x7F800000;
        public const uint NEGATIVE_INFINITY_VALUE = 0xFF800000;
        public const uint ONE = 0x3F800000;
        public const uint MIN_ONE = 0xBF800000;
        public const int IMPLICIT_LEADING_BIT_POS = 23;

        private static readonly sbyte[] msb = new sbyte[256]
        {
            -1, 0, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
            5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
            6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
            6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7
        };

        private static readonly int[] debruijn32 = new int[64]
        {
            32, 8,  17, -1, -1, 14, -1, -1, -1, 20, -1, -1, -1, 28, -1, 18,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0,  26, 25, 24,
            4,  11, 23, 31, 3,  7,  10, 16, 22, 30, -1, -1, 2,  6,  13, 9,
            -1, 15, -1, 21, -1, 29, 19, -1, -1, -1, -1, -1, 1,  27, 5,  12
        };

        private static readonly int[] normalizeAmounts = new int[]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 8, 8, 8, 8, 8, 8, 8, 8, 16, 16, 16, 16, 16, 16, 16, 16, 24, 24, 24, 24, 24, 24, 24
        };

        public Ps2Float(uint value)
        {
            Sign = ((value >> 31) & 1) != 0;
            Exponent = (byte)((value >> 23) & 0xFF);
            Mantissa = value & 0x7FFFFF;
        }

        public Ps2Float(bool sign, byte exponent, uint mantissa)
        {
            Sign = sign;
            Exponent = exponent;
            Mantissa = mantissa;
        }

        public static Ps2Float Max()
        {
            return new Ps2Float(MAX_FLOATING_POINT_VALUE);
        }

        public static Ps2Float Min()
        {
            return new Ps2Float(MIN_FLOATING_POINT_VALUE);
        }

        public static Ps2Float One()
        {
            return new Ps2Float(ONE);
        }

        public static Ps2Float MinOne()
        {
            return new Ps2Float(MIN_ONE);
        }

        public uint AsUInt32()
        {
            uint result = 0;
            result |= (Sign ? 1u : 0u) << 31;
            result |= (uint)(Exponent << 23);
            result |= Mantissa;
            return result;
        }

        public Ps2Float Add(Ps2Float addend)
        {
            if (IsDenormalized() || addend.IsDenormalized())
                return SolveAddSubDenormalizedOperation(this, addend, true);

            if (IsAbnormal() && addend.IsAbnormal())
                return SolveAbnormalAdditionOrSubtractionOperation(this, addend, true);

            uint a = AsUInt32();
            uint b = addend.AsUInt32();
            int temp = 0;

            // Exponent difference
            int expDiff = ((int)(a >> 23) & 0xFF) - ((int)(b >> 23) & 0xFF);

            // diff = 25 .. 255 , expt < expd
            if (expDiff >= 25)
            {
                b = b & SIGNMASK;
            }
            // diff = 1 .. 24, expt < expd
            else if (expDiff > 0)
            {
                expDiff = expDiff - 1;
                temp = unchecked((int)MIN_FLOATING_POINT_VALUE) << expDiff;
                b = (uint)(temp & b);
            }
            // diff = -255 .. -25, expd < expt
            else if (expDiff <= -25)
            {
                a = a & SIGNMASK;
            }
            // diff = -24 .. -1 , expd < expt
            else if (expDiff < 0)
            {
                expDiff = -expDiff;
                expDiff = expDiff - 1;
                temp = unchecked((int)MIN_FLOATING_POINT_VALUE) << expDiff;
                a = (uint)(a & temp);
            }

            return new Ps2Float(a).DoAdd(new Ps2Float(b));
        }

        public Ps2Float Sub(Ps2Float subtrahend)
        {
            if (IsDenormalized() || subtrahend.IsDenormalized())
                return SolveAddSubDenormalizedOperation(this, subtrahend, false);

            if (IsAbnormal() && subtrahend.IsAbnormal())
                return SolveAbnormalAdditionOrSubtractionOperation(this, subtrahend, false);

            uint a = AsUInt32();
            uint b = subtrahend.AsUInt32();
            int temp = 0;

            // Exponent difference
            int expDiff = ((int)(a >> 23) & 0xFF) - ((int)(b >> 23) & 0xFF);

            // diff = 25 .. 255 , expt < expd
            if (expDiff >= 25)
            {
                b = b & SIGNMASK;
            }
            // diff = 1 .. 24, expt < expd
            else if (expDiff > 0)
            {
                expDiff = expDiff - 1;
                temp = unchecked((int)MIN_FLOATING_POINT_VALUE) << expDiff;
                b = (uint)(temp & b);
            }
            // diff = -255 .. -25, expd < expt
            else if (expDiff <= -25)
            {
                a = a & SIGNMASK;
            }
            // diff = -24 .. -1 , expd < expt
            else if (expDiff < 0)
            {
                expDiff = -expDiff;
                expDiff = expDiff - 1;
                temp = unchecked((int)MIN_FLOATING_POINT_VALUE) << expDiff;
                a = (uint)(a & temp);
            }

            return new Ps2Float(a).DoAdd(Neg(new Ps2Float(b)));
        }

        public Ps2Float Mul(Ps2Float mulend)
        {
            if (IsDenormalized() || mulend.IsDenormalized())
                return SolveMultiplicationDenormalizedOperation(this, mulend);

            if (IsAbnormal() && mulend.IsAbnormal())
                return SolveAbnormalMultiplicationOrDivisionOperation(this, mulend, true);

            if (IsZero() || mulend.IsZero())
            {
                return new Ps2Float(0)
                {
                    Sign = DetermineMultiplicationDivisionOperationSign(this, mulend)
                };
            }

            return DoMul(mulend);
        }

        public Ps2Float Div(Ps2Float divend)
        {
            if (IsDenormalized() || divend.IsDenormalized())
                return SolveDivisionDenormalizedOperation(this, divend);

            if (IsAbnormal() && divend.IsAbnormal())
                return SolveAbnormalMultiplicationOrDivisionOperation(this, divend, false);

            if (IsZero())
            {
                return new Ps2Float(0)
                {
                    Sign = DetermineMultiplicationDivisionOperationSign(this, divend)
                };
            }
            else if (divend.IsZero())
                return DetermineMultiplicationDivisionOperationSign(this, divend) ? Min() : Max();

            return DoDiv(divend);
        }

        private Ps2Float DoAdd(Ps2Float other)
        {
            const byte roundingMultiplier = 6;

            byte selfExponent = Exponent;
            int resExponent = selfExponent - other.Exponent;

            if (resExponent < 0)
                return other.DoAdd(this);
            else if (resExponent >= 25)
                return this;

            // http://graphics.stanford.edu/~seander/bithacks.html#ConditionalNegate
            uint sign1 = (uint)((int)AsUInt32() >> 31);
            int selfMantissa = (int)(((Mantissa | 0x800000) ^ sign1) - sign1);
            uint sign2 = (uint)((int)other.AsUInt32() >> 31);
            int otherMantissa = (int)(((other.Mantissa | 0x800000) ^ sign2) - sign2);

            int man = (selfMantissa << roundingMultiplier) + ((otherMantissa << roundingMultiplier) >> resExponent);
            int absMan = Math.Abs(man);
            if (absMan == 0)
                return new Ps2Float(0);

            int rawExp = selfExponent - roundingMultiplier;

            int amount = normalizeAmounts[clz(absMan)];
            rawExp -= amount;
            absMan <<= amount;

            int msbIndex = BitScanReverse8(absMan >> 23);
            rawExp += msbIndex;
            absMan >>= msbIndex;

            if (rawExp > 255)
                return man < 0 ? Min() : Max();
            else if (rawExp <= 0)
                return new Ps2Float(man < 0, 0, 0);

            return new Ps2Float((uint)man & SIGNMASK | (uint)rawExp << 23 | ((uint)absMan & 0x7FFFFF));
        }

        private Ps2Float DoMul(Ps2Float other)
        {
            byte selfExponent = Exponent;
            byte otherExponent = other.Exponent;
            uint selfMantissa = Mantissa | 0x800000;
            uint otherMantissa = other.Mantissa | 0x800000;
            uint sign = (AsUInt32() ^ other.AsUInt32()) & SIGNMASK;

            int resExponent = selfExponent + otherExponent - 127;
            uint resMantissa = (uint)(BoothMultiplier.MulMantissa(selfMantissa, otherMantissa) >> 23);

            if (resMantissa > 0xFFFFFF)
            {
                resMantissa >>= 1;
                resExponent++;
            }

            if (resExponent > 255)
                return new Ps2Float(sign | MAX_FLOATING_POINT_VALUE);
            else if (resExponent <= 0)
                return new Ps2Float(sign);

            return new Ps2Float(sign | (uint)(resExponent << 23) | (resMantissa & 0x7FFFFF));
        }

        // Rounding can be slightly off: (PS2: 0x3F800000 / 0x3F800001 = 0x3F7FFFFF | SoftFloat/IEEE754: 0x3F800000 / 0x3F800001 = 0x3F7FFFFE).
        private Ps2Float DoDiv(Ps2Float other)
        {
            ulong selfMantissa64;
            uint selfMantissa = Mantissa | 0x800000;
            uint otherMantissa = other.Mantissa | 0x800000;
            int resExponent = Exponent - other.Exponent + BIAS;

            Ps2Float result = new Ps2Float(0) { Sign = DetermineMultiplicationDivisionOperationSign(this, other) };

            if (resExponent > 255)
                return result.Sign ? Min() : Max();
            else if (resExponent <= 0)
                return new Ps2Float(result.Sign, 0, 0);

            if (selfMantissa < otherMantissa)
            {
                --resExponent;
                if (resExponent == 0)
                    return new Ps2Float(result.Sign, 0, 0);
                selfMantissa64 = (ulong)selfMantissa << 31;
            }
            else
                selfMantissa64 = (ulong)selfMantissa << 30;

            uint resMantissa = (uint)(selfMantissa64 / otherMantissa);
            if ((resMantissa & 0x3F) == 0)
                resMantissa |= ((ulong)otherMantissa * resMantissa != selfMantissa64) ? 1U : 0;

            result.Exponent = (byte)resExponent;
            result.Mantissa = (resMantissa + 0x40U) >> 7;

            if (result.Mantissa > 0)
            {
                int leadingBitPosition = GetMostSignificantBitPosition(result.Mantissa);

                while (leadingBitPosition != IMPLICIT_LEADING_BIT_POS)
                {
                    if (leadingBitPosition > IMPLICIT_LEADING_BIT_POS)
                    {
                        result.Mantissa >>= 1;

                        int increasedExponent = result.Exponent + 1;

                        if (increasedExponent > 255)
                            return result.Sign ? Min() : Max();

                        result.Exponent = (byte)increasedExponent;

                        leadingBitPosition--;
                    }
                    else if (leadingBitPosition < IMPLICIT_LEADING_BIT_POS)
                    {
                        result.Mantissa <<= 1;

                        int decreasedExponent = result.Exponent - 1;

                        if (decreasedExponent <= 0)
                            return new Ps2Float(result.Sign, 0, 0);

                        result.Exponent = (byte)decreasedExponent;

                        leadingBitPosition++;
                    }
                }
            }

            result.Mantissa &= 0x7FFFFF;
            return result.RoundTowardsZero();
        }

        // Rounding can be slightly off: (PS2: rsqrt(0x7FFFFFF0) -> 0x5FB504ED | SoftFloat/IEEE754 rsqrt(0x7FFFFFF0) -> 0x5FB504EE).
        /// <summary>
        /// Returns the square root of x
        /// </summary>
        public Ps2Float Sqrt()
        {
            int t;
            int s = 0;
            int q = 0;
            uint r = 0x01000000; /* r = moving bit from right to left */

            if (IsDenormalized())
                return new Ps2Float(0);

            // PS2 only takes positive numbers for SQRT, and convert if necessary.
            int ix = (int)new Ps2Float(false, Exponent, Mantissa).AsUInt32();

            /* extract mantissa and unbias exponent */
            int m = (ix >> 23) - BIAS;

            ix = (ix & 0x007FFFFF) | 0x00800000;
            if ((m & 1) == 1)
            {
                /* odd m, double x to make it even */
                ix += ix;
            }

            m >>= 1; /* m = [m/2] */

            /* generate sqrt(x) bit by bit */
            ix += ix;

            while (r != 0)
            {
                t = s + (int)r;
                if (t <= ix)
                {
                    s = t + (int)r;
                    ix -= t;
                    q += (int)r;
                }

                ix += ix;
                r >>= 1;
            }

            /* use floating add to find out rounding direction */
            if (ix != 0)
            {
                q += q & 1;
            }

            ix = (q >> 1) + 0x3F000000;
            ix += m << 23;

            return new Ps2Float((uint)ix);
        }

        public Ps2Float Rsqrt(Ps2Float other)
        {
            return Div(other.Sqrt());
        }

        public bool IsDenormalized()
        {
            return Exponent == 0;
        }

        public bool IsAbnormal()
        {
            uint val = AsUInt32();
            return val == MAX_FLOATING_POINT_VALUE || val == MIN_FLOATING_POINT_VALUE ||
                   val == POSITIVE_INFINITY_VALUE || val == NEGATIVE_INFINITY_VALUE;
        }

        public bool IsZero()
        {
            return (Abs()) == 0;
        }

        public Ps2Float RoundTowardsZero()
        {
            return new Ps2Float((uint)Math.Truncate((double)AsUInt32()));
        }

        public int CompareTo(Ps2Float other)
        {
            int selfTwoComplementVal = (int)(Abs());
            if (Sign) selfTwoComplementVal = -selfTwoComplementVal;

            int otherTwoComplementVal = (int)(other.Abs());
            if (other.Sign) otherTwoComplementVal = -otherTwoComplementVal;

            return selfTwoComplementVal.CompareTo(otherTwoComplementVal);
        }

        public int CompareOperand(Ps2Float other)
        {
            int selfTwoComplementVal = (int)(Abs());
            int otherTwoComplementVal = (int)(other.Abs());

            return selfTwoComplementVal.CompareTo(otherTwoComplementVal);
        }

        public uint Abs()
        {
            return AsUInt32() & MAX_FLOATING_POINT_VALUE;
        }

        private static Ps2Float SolveAbnormalAdditionOrSubtractionOperation(Ps2Float a, Ps2Float b, bool add)
        {
            uint aval = a.AsUInt32();
            uint bval = b.AsUInt32();

            if (aval == MAX_FLOATING_POINT_VALUE && bval == MAX_FLOATING_POINT_VALUE)
                return add ? Max() : new Ps2Float(0);

            if (aval == MIN_FLOATING_POINT_VALUE && bval == MIN_FLOATING_POINT_VALUE)
                return add ? Min() : new Ps2Float(0);

            if (aval == MIN_FLOATING_POINT_VALUE && bval == MAX_FLOATING_POINT_VALUE)
                return add ? new Ps2Float(0) : Min();

            if (aval == MAX_FLOATING_POINT_VALUE && bval == MIN_FLOATING_POINT_VALUE)
                return add ? new Ps2Float(0) : Max();

            if (aval == POSITIVE_INFINITY_VALUE && bval == POSITIVE_INFINITY_VALUE)
                return add ? Max() : new Ps2Float(0);

            if (aval == NEGATIVE_INFINITY_VALUE && bval == POSITIVE_INFINITY_VALUE)
                return add ? new Ps2Float(0) : Min();

            if (aval == POSITIVE_INFINITY_VALUE && bval == NEGATIVE_INFINITY_VALUE)
                return add ? new Ps2Float(0) : Max();

            if (aval == NEGATIVE_INFINITY_VALUE && bval == NEGATIVE_INFINITY_VALUE)
                return add ? Min() : new Ps2Float(0);

            if (aval == MAX_FLOATING_POINT_VALUE && bval == POSITIVE_INFINITY_VALUE)
                return add ? Max() : new Ps2Float(0x7F7FFFFE);

            if (aval == MAX_FLOATING_POINT_VALUE && bval == NEGATIVE_INFINITY_VALUE)
                return add ? new Ps2Float(0x7F7FFFFE) : Max();

            if (aval == MIN_FLOATING_POINT_VALUE && bval == POSITIVE_INFINITY_VALUE)
                return add ? new Ps2Float(0xFF7FFFFE) : Min();

            if (aval == MIN_FLOATING_POINT_VALUE && bval == NEGATIVE_INFINITY_VALUE)
                return add ? Min() : new Ps2Float(0xFF7FFFFE);

            if (aval == POSITIVE_INFINITY_VALUE && bval == MAX_FLOATING_POINT_VALUE)
                return add ? Max() : new Ps2Float(0xFF7FFFFE);

            if (aval == POSITIVE_INFINITY_VALUE && bval == MIN_FLOATING_POINT_VALUE)
                return add ? new Ps2Float(0xFF7FFFFE) : Max();

            if (aval == NEGATIVE_INFINITY_VALUE && bval == MAX_FLOATING_POINT_VALUE)
                return add ? new Ps2Float(0x7F7FFFFE) : Min();

            if (aval == NEGATIVE_INFINITY_VALUE && bval == MIN_FLOATING_POINT_VALUE)
                return add ? Min() : new Ps2Float(0x7F7FFFFE);

            throw new InvalidOperationException("Unhandled abnormal add/sub floating point operation");
        }

        private static Ps2Float SolveAbnormalMultiplicationOrDivisionOperation(Ps2Float a, Ps2Float b, bool mul)
        {
            uint aval = a.AsUInt32();
            uint bval = b.AsUInt32();

            if (mul)
            {
                if ((aval == MAX_FLOATING_POINT_VALUE && bval == MAX_FLOATING_POINT_VALUE) ||
                    (aval == MIN_FLOATING_POINT_VALUE && bval == MIN_FLOATING_POINT_VALUE))
                    return Max();

                if ((aval == MAX_FLOATING_POINT_VALUE && bval == MIN_FLOATING_POINT_VALUE) ||
                    (aval == MIN_FLOATING_POINT_VALUE && bval == MAX_FLOATING_POINT_VALUE))
                    return Min();

                if (aval == POSITIVE_INFINITY_VALUE && bval == POSITIVE_INFINITY_VALUE)
                    return Max();

                if (aval == NEGATIVE_INFINITY_VALUE && bval == POSITIVE_INFINITY_VALUE)
                    return Min();

                if (aval == POSITIVE_INFINITY_VALUE && bval == NEGATIVE_INFINITY_VALUE)
                    return Min();

                if (aval == NEGATIVE_INFINITY_VALUE && bval == NEGATIVE_INFINITY_VALUE)
                    return Max();

                if (aval == MAX_FLOATING_POINT_VALUE && bval == POSITIVE_INFINITY_VALUE)
                    return Max();

                if (aval == MAX_FLOATING_POINT_VALUE && bval == NEGATIVE_INFINITY_VALUE)
                    return Min();

                if (aval == MIN_FLOATING_POINT_VALUE && bval == POSITIVE_INFINITY_VALUE)
                    return Min();

                if (aval == MIN_FLOATING_POINT_VALUE && bval == NEGATIVE_INFINITY_VALUE)
                    return Max();

                if (aval == POSITIVE_INFINITY_VALUE && bval == MAX_FLOATING_POINT_VALUE)
                    return Max();

                if (aval == POSITIVE_INFINITY_VALUE && bval == MIN_FLOATING_POINT_VALUE)
                    return Min();

                if (aval == NEGATIVE_INFINITY_VALUE && bval == MAX_FLOATING_POINT_VALUE)
                    return Min();

                if (aval == NEGATIVE_INFINITY_VALUE && bval == MIN_FLOATING_POINT_VALUE)
                    return Max();
            }
            else
            {
                if ((aval == MAX_FLOATING_POINT_VALUE && bval == MAX_FLOATING_POINT_VALUE) ||
                    (aval == MIN_FLOATING_POINT_VALUE && bval == MIN_FLOATING_POINT_VALUE))
                    return One();

                if ((aval == MAX_FLOATING_POINT_VALUE && bval == MIN_FLOATING_POINT_VALUE) ||
                    (aval == MIN_FLOATING_POINT_VALUE && bval == MAX_FLOATING_POINT_VALUE))
                    return MinOne();

                if (aval == POSITIVE_INFINITY_VALUE && bval == POSITIVE_INFINITY_VALUE)
                    return One();

                if (aval == NEGATIVE_INFINITY_VALUE && bval == POSITIVE_INFINITY_VALUE)
                    return MinOne();

                if (aval == POSITIVE_INFINITY_VALUE && bval == NEGATIVE_INFINITY_VALUE)
                    return MinOne();

                if (aval == NEGATIVE_INFINITY_VALUE && bval == NEGATIVE_INFINITY_VALUE)
                    return One();

                if (aval == MAX_FLOATING_POINT_VALUE && bval == POSITIVE_INFINITY_VALUE)
                    return new Ps2Float(0x3FFFFFFF);

                if (aval == MAX_FLOATING_POINT_VALUE && bval == NEGATIVE_INFINITY_VALUE)
                    return new Ps2Float(0xBFFFFFFF);

                if (aval == MIN_FLOATING_POINT_VALUE && bval == POSITIVE_INFINITY_VALUE)
                    return new Ps2Float(0xBFFFFFFF);

                if (aval == MIN_FLOATING_POINT_VALUE && bval == NEGATIVE_INFINITY_VALUE)
                    return new Ps2Float(0x3FFFFFFF);

                if (aval == POSITIVE_INFINITY_VALUE && bval == MAX_FLOATING_POINT_VALUE)
                    return new Ps2Float(0x3F000001);

                if (aval == POSITIVE_INFINITY_VALUE && bval == MIN_FLOATING_POINT_VALUE)
                    return new Ps2Float(0xBF000001);

                if (aval == NEGATIVE_INFINITY_VALUE && bval == MAX_FLOATING_POINT_VALUE)
                    return new Ps2Float(0xBF000001);

                if (aval == NEGATIVE_INFINITY_VALUE && bval == MIN_FLOATING_POINT_VALUE)
                    return new Ps2Float(0x3F000001);
            }

            throw new InvalidOperationException("Unhandled abnormal mul/div floating point operation");
        }

        private static Ps2Float Neg(Ps2Float self)
        {
            return new Ps2Float(self.AsUInt32() ^ SIGNMASK);
        }

        private static Ps2Float SolveAddSubDenormalizedOperation(Ps2Float a, Ps2Float b, bool add)
        {
            Ps2Float result;

            if (a.IsDenormalized() && !b.IsDenormalized())
                result = b;
            else if (!a.IsDenormalized() && b.IsDenormalized())
                result = a;
            else if (a.IsDenormalized() && b.IsDenormalized())
                result = new Ps2Float(0);
            else
                throw new InvalidOperationException("Both numbers are not denormalized");

            result.Sign = add ? DetermineAdditionOperationSign(a, b) : DetermineSubtractionOperationSign(a, b);
            return result;
        }

        private static Ps2Float SolveMultiplicationDenormalizedOperation(Ps2Float a, Ps2Float b)
        {
            return new Ps2Float(0)
            {
                Sign = DetermineMultiplicationDivisionOperationSign(a, b)
            };
        }

        private static Ps2Float SolveDivisionDenormalizedOperation(Ps2Float a, Ps2Float b)
        {
            bool sign = DetermineMultiplicationDivisionOperationSign(a, b);
            Ps2Float result;

            if (a.IsDenormalized() && !b.IsDenormalized())
                result = new Ps2Float(0);
            else if (!a.IsDenormalized() && b.IsDenormalized())
                return sign ? Min() : Max();
            else if (a.IsDenormalized() && b.IsDenormalized())
                return sign ? Min() : Max();
            else
                throw new InvalidOperationException("Both numbers are not denormalized");

            result.Sign = sign;
            return result;
        }

        private static bool DetermineMultiplicationDivisionOperationSign(Ps2Float a, Ps2Float b)
        {
            return a.Sign ^ b.Sign;
        }

        private static bool DetermineAdditionOperationSign(Ps2Float a, Ps2Float b)
        {
            if (a.IsZero() && b.IsZero())
            {
                if (!a.Sign || !b.Sign)
                    return false;
                else if (a.Sign && b.Sign)
                    return true;
                else
                    throw new InvalidOperationException("Unhandled addition operation flags");
            }

            return a.CompareOperand(b) >= 0 ? a.Sign : b.Sign;
        }

        private static bool DetermineSubtractionOperationSign(Ps2Float a, Ps2Float b)
        {
            if (a.IsZero() && b.IsZero())
            {
                if (!a.Sign || b.Sign)
                    return false;
                else if (a.Sign && !b.Sign)
                    return true;
                else
                    throw new InvalidOperationException("Unhandled subtraction operation flags");
            }

            return a.CompareOperand(b) >= 0 ? a.Sign : !b.Sign;
        }

        /// <summary>
        /// Returns the leading zero count of the given 32-bit integer
        /// </summary>
        private static int clz(int x)
        {
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;

            return debruijn32[(uint)x * 0x8c0b2891u >> 26];
        }

        private static int BitScanReverse8(int b)
        {
            return msb[b];
        }

        private static int GetMostSignificantBitPosition(uint value)
        {
            for (int i = 31; i >= 0; i--)
            {
                if (((value >> i) & 1) != 0)
                    return i;
            }
            return -1;
        }

        public double ToDouble()
        {
            double res = (Mantissa / Math.Pow(2, 23) + 1.0) * Math.Pow(2, Exponent - 127.0);
            if (Sign)
                res *= -1.0;

            return res;
        }

        public override string ToString()
        {
            double res = ToDouble();

            uint value = AsUInt32();
            if (IsDenormalized())
                return $"Denormalized({res:F6})";
            else if (value == MAX_FLOATING_POINT_VALUE)
                return $"Fmax({res:F6})";
            else if (value == MIN_FLOATING_POINT_VALUE)
                return $"-Fmax({res:F6})";
            else if (value == POSITIVE_INFINITY_VALUE)
                return $"Inf({res:F6})";
            else if (value == NEGATIVE_INFINITY_VALUE)
                return $"-Inf({res:F6})";

            return $"Ps2Float({res:F6})";
        }
    }
}