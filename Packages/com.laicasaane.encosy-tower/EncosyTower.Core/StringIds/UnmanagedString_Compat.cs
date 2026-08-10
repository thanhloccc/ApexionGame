#if !UNITY_COLLECTIONS

#pragma warning disable IDE1006 // Naming Styles

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using EncosyTower.Common;
using UnityEngine;

namespace EncosyTower.StringIds
{
    partial struct UnmanagedString
    {
        public struct FixedString512Bytes : IEquatable<FixedString512Bytes>, IComparable<FixedString512Bytes>
        {
            private ushort utf8LengthInBytes;
            private FixedBytes510 bytes;

            public FixedString512Bytes(string other) : this()
            {
                // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
                unsafe
                {
                    Initialize(ref this, other.AsSpan());
                }
            }

            public static int UTF8MaxLengthInBytes => 509;

            public int Length
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                readonly get
                {
                    return utf8LengthInBytes;
                }

                set
                {
                    utf8LengthInBytes = (ushort)value;

                    // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
                    unsafe
                    {
                        GetUnsafePtr()[utf8LengthInBytes] = 0;
                    }
                }
            }

            public readonly int Capacity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => UTF8MaxLengthInBytes;
            }

            public readonly bool IsEmpty
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => utf8LengthInBytes == 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator FixedString512Bytes(string other)
                => new(other);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(in FixedString512Bytes a, in FixedString512Bytes b)
                => a.Equals(b);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(in FixedString512Bytes a, in FixedString512Bytes b)
                => !a.Equals(b);

            private static void Initialize(ref FixedString512Bytes fs, ReadOnlySpan<char> chars)
            {
                // SAFETY: The stack buffer is local to this call and the fixed-string span is bounded by Capacity.
                unsafe
                {
                    int worstCaseCapacity = chars.Length * 4;
                    Span<byte> buffer = stackalloc byte[worstCaseCapacity];
                    var length = Encoding.UTF8.GetBytes(chars, buffer);
                    length = Mathf.Min(length, fs.Capacity);

                    fs.Length = length;
                    buffer[..length].CopyTo(fs.AsSpan());
                }
            }

            /// <safety>The returned pointer is valid only for this fixed-string value's lifetime.</safety>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly unsafe byte* GetUnsafePtr()
            {
                // SAFETY: bytes is an inline value field and the returned pointer is valid only for this value's lifetime.
                unsafe
                {
                    fixed (void* b = &bytes)
                    {
                        return (byte*)b;
                    }
                }
            }

            /// <safety>The returned span borrows this fixed-string value and must not outlive it.</safety>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe Span<byte> AsSpan()
            {
                // SAFETY: The span borrows this fixed-string value and is bounded by Length.
                unsafe
                {
                    return new(GetUnsafePtr(), Length);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly ReadOnlySpan<byte> AsReadOnlySpan()
            {
                // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
                unsafe
                {
                    return new(GetUnsafePtr(), Length);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool Equals(FixedString512Bytes other)
            {
                return AsReadOnlySpan().SequenceEqual(other.AsReadOnlySpan());
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly override bool Equals(object obj)
            {
                return obj switch {
                    string other => Equals(other),
                    FixedString512Bytes other => Equals(other),
                    _ => false
                };
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly override int GetHashCode()
            {
                return HashValue.FNV1a(AsReadOnlySpan());
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly override string ToString()
            {
                return Encoding.UTF8.GetString(AsReadOnlySpan());
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly int CompareTo(FixedString512Bytes other)
            {
                return AsReadOnlySpan().SequenceCompareTo(other.AsReadOnlySpan());
            }

            [StructLayout(LayoutKind.Explicit, Size = 16)]
            private struct FixedBytes16
            {
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(0)] public byte byte0000;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(1)] public byte byte0001;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(2)] public byte byte0002;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(3)] public byte byte0003;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(4)] public byte byte0004;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(5)] public byte byte0005;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(6)] public byte byte0006;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(7)] public byte byte0007;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(8)] public byte byte0008;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(9)] public byte byte0009;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(10)] public byte byte0010;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(11)] public byte byte0011;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(12)] public byte byte0012;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(13)] public byte byte0013;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(14)] public byte byte0014;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(15)] public byte byte0015;
            }

            [StructLayout(LayoutKind.Explicit, Size = 510)]
            private struct FixedBytes510
            {
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(0)] public FixedBytes16 offset0000;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(16)] public FixedBytes16 offset0016;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(32)] public FixedBytes16 offset0032;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(48)] public FixedBytes16 offset0048;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(64)] public FixedBytes16 offset0064;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(80)] public FixedBytes16 offset0080;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(96)] public FixedBytes16 offset0096;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(112)] public FixedBytes16 offset0112;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(128)] public FixedBytes16 offset0128;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(144)] public FixedBytes16 offset0144;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(160)] public FixedBytes16 offset0160;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(176)] public FixedBytes16 offset0176;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(192)] public FixedBytes16 offset0192;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(208)] public FixedBytes16 offset0208;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(224)] public FixedBytes16 offset0224;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(240)] public FixedBytes16 offset0240;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(256)] public FixedBytes16 offset0256;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(272)] public FixedBytes16 offset0272;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(288)] public FixedBytes16 offset0288;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(304)] public FixedBytes16 offset0304;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(320)] public FixedBytes16 offset0320;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(336)] public FixedBytes16 offset0336;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(352)] public FixedBytes16 offset0352;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(368)] public FixedBytes16 offset0368;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(384)] public FixedBytes16 offset0384;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(400)] public FixedBytes16 offset0400;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(416)] public FixedBytes16 offset0416;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(432)] public FixedBytes16 offset0432;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(448)] public FixedBytes16 offset0448;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(464)] public FixedBytes16 offset0464;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(480)] public FixedBytes16 offset0480;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(496)] public byte byte0496;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(497)] public byte byte0497;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(498)] public byte byte0498;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(499)] public byte byte0499;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(500)] public byte byte0500;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(501)] public byte byte0501;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(502)] public byte byte0502;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(503)] public byte byte0503;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(504)] public byte byte0504;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(505)] public byte byte0505;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(506)] public byte byte0506;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(507)] public byte byte0507;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(508)] public byte byte0508;
                // TODO(unsafe-evolution): mark this overlapping field safe/unsafe when the new syntax is available.
                [FieldOffset(509)] public byte byte0509;
            }
        }
    }

    internal static class UnmanagedStringFixedStringExtensions
    {
        public static void CopyFromTruncated(
              this ref UnmanagedString.FixedString512Bytes fs
            , ReadOnlySpan<byte> utf8Chars
        )
        {
            // SAFETY: The surrounding validation or ownership contract makes this native-memory operation sound.
            unsafe
            {
                var length = Mathf.Min(utf8Chars.Length, fs.Capacity);
                fs.Length = length;
                utf8Chars[..length].CopyTo(fs.AsSpan());
            }
        }
    }
}

#endif
