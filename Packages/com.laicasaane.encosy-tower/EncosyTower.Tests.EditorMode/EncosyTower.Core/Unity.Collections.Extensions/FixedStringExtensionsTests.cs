#if UNITY_COLLECTIONS

using System;
using System.Text;
using EncosyTower.Collections;
using EncosyTower.Common;
using NUnit.Framework;
using Unity.Collections;

namespace EncosyTower.Tests.Core.Collections
{
    public class FixedStringExtensionsTests
    {
        [Test]
        public void ToFixedString_AllValueOverloadsReturnExpectedText()
        {
            Assert.AreEqual("True", EncosyFixedStringExtensions.ToFixedString(true).ToString());
            Assert.AreEqual("False", EncosyFixedStringExtensions.ToFixedString(false).ToString());
            Assert.AreEqual("12", EncosyFixedStringExtensions.ToFixedString((byte)12).ToString());
            Assert.AreEqual(
                  "-12"
                , EncosyFixedStringExtensions.ToFixedString((sbyte)-12).ToString()
            );
            Assert.AreEqual(
                  "-123"
                , EncosyFixedStringExtensions.ToFixedString((short)-123).ToString()
            );
            Assert.AreEqual(
                  "123"
                , EncosyFixedStringExtensions.ToFixedString((ushort)123).ToString()
            );
            Assert.AreEqual("-456", EncosyFixedStringExtensions.ToFixedString(-456).ToString());
            Assert.AreEqual("456", EncosyFixedStringExtensions.ToFixedString(456u).ToString());
            Assert.AreEqual("-789", EncosyFixedStringExtensions.ToFixedString(-789L).ToString());
            Assert.AreEqual("789", EncosyFixedStringExtensions.ToFixedString(789UL).ToString());
            Assert.AreEqual("1.5", EncosyFixedStringExtensions.ToFixedString(1.5f).ToString());
            Assert.AreEqual(
                  "2"
                , EncosyFixedStringExtensions.ToFixedString(new Index(2)).ToString()
            );

            var range = new Range(new Index(1), new Index(3));

            Assert.AreEqual("[1, 3]", EncosyFixedStringExtensions.ToFixedString(range).ToString());
        }

        [Test]
        public void CastMembers_AllSourceOverloadsCopyText()
        {
            FixedString32Bytes source32 = "abc";
            FixedString64Bytes source64 = "abc";
            FixedString128Bytes source128 = "abc";
            FixedString512Bytes source512 = "abc";
            FixedString4096Bytes source4096 = "abc";

            var from32 = EncosyFixedStringExtensions.CastTo<FixedString128Bytes>(in source32);
            var from64 = EncosyFixedStringExtensions.CastTo<FixedString128Bytes>(in source64);
            var from128 = EncosyFixedStringExtensions.CastTo<FixedString512Bytes>(in source128);
            var from512 = EncosyFixedStringExtensions.CastTo<FixedString4096Bytes>(in source512);
            var from4096 = EncosyFixedStringExtensions.CastTo<FixedString4096Bytes>(in source4096);
            var generic = EncosyFixedStringExtensions
                .Cast<FixedString32Bytes, FixedString128Bytes>(source32);
            var marker = EncosyFixedStringExtensions.CastTo(
                  source32
                , GenericT.T<FixedString128Bytes>()
            );

            Assert.AreEqual("abc", from32.ToString());
            Assert.AreEqual("abc", from64.ToString());
            Assert.AreEqual("abc", from128.ToString());
            Assert.AreEqual("abc", from512.ToString());
            Assert.AreEqual("abc", from4096.ToString());
            Assert.AreEqual("abc", generic.ToString());
            Assert.AreEqual("abc", marker.ToString());
        }

        [Test]
        public void CopyAndAppendMembers_HandleEmptyExactAndTruncatedInput()
        {
            FixedString32Bytes utf16 = "old";
            ReadOnlySpan<char> emptyChars = ReadOnlySpan<char>.Empty;
            ReadOnlySpan<char> abcChars = "abc".AsSpan();

            Assert.AreEqual(
                  CopyError.None
                , EncosyFixedStringExtensions.CopyFrom(ref utf16, emptyChars)
            );
            Assert.AreEqual(string.Empty, utf16.ToString());
            Assert.AreEqual(
                  CopyError.None
                , EncosyFixedStringExtensions.CopyFrom(ref utf16, abcChars)
            );
            Assert.AreEqual("abc", utf16.ToString());

            var exactText = new string('x', utf16.Capacity);
            ReadOnlySpan<char> exactChars = exactText.AsSpan();

            Assert.AreEqual(
                  CopyError.None
                , EncosyFixedStringExtensions.CopyFrom(ref utf16, exactChars)
            );
            Assert.AreEqual(exactText, utf16.ToString());

            ReadOnlySpan<char> tooManyChars = new string('y', utf16.Capacity + 1).AsSpan();

            Assert.AreEqual(
                  CopyError.Truncation
                , EncosyFixedStringExtensions.CopyFrom(ref utf16, tooManyChars)
            );

            FixedString32Bytes truncatedChars = default;
            var truncatedCharResult = EncosyFixedStringExtensions.CopyFromTruncated(
                  ref truncatedChars
                , tooManyChars
            );

            Assert.AreEqual(CopyError.Truncation, truncatedCharResult);
            Assert.AreEqual(truncatedChars.Capacity, truncatedChars.Length);

            FixedString32Bytes appendedChars = "a";

            Assert.AreEqual(
                  FormatError.None
                , EncosyFixedStringExtensions.Append(ref appendedChars, "bc".AsSpan())
            );
            Assert.AreEqual("abc", appendedChars.ToString());

            var fullBeforeAppend = new string('q', appendedChars.Capacity);
            appendedChars = fullBeforeAppend;

            Assert.AreEqual(
                  FormatError.Overflow
                , EncosyFixedStringExtensions.Append(ref appendedChars, "z".AsSpan())
            );
            Assert.AreEqual(fullBeforeAppend, appendedChars.ToString());

            var abcBytes = Encoding.UTF8.GetBytes("abc");
            ReadOnlySpan<byte> emptyBytes = ReadOnlySpan<byte>.Empty;
            ReadOnlySpan<byte> utf8Bytes = abcBytes;
            FixedString32Bytes utf8 = "old";

            Assert.AreEqual(
                  CopyError.None
                , EncosyFixedStringExtensions.CopyFrom(ref utf8, emptyBytes)
            );
            Assert.AreEqual(string.Empty, utf8.ToString());
            Assert.AreEqual(
                  CopyError.None
                , EncosyFixedStringExtensions.CopyFrom(ref utf8, utf8Bytes)
            );
            Assert.AreEqual("abc", utf8.ToString());

            var exactBytesArray = Encoding.UTF8.GetBytes(new string('p', utf8.Capacity));
            ReadOnlySpan<byte> exactBytes = exactBytesArray;

            Assert.AreEqual(
                  CopyError.None
                , EncosyFixedStringExtensions.CopyFrom(ref utf8, exactBytes)
            );
            Assert.AreEqual(new string('p', utf8.Capacity), utf8.ToString());

            var tooManyBytesArray = Encoding.UTF8.GetBytes(new string('r', utf8.Capacity + 1));
            ReadOnlySpan<byte> tooManyBytes = tooManyBytesArray;

            Assert.AreEqual(
                  CopyError.Truncation
                , EncosyFixedStringExtensions.CopyFrom(ref utf8, tooManyBytes)
            );

            FixedString32Bytes truncatedBytes = default;
            var truncatedByteResult = EncosyFixedStringExtensions.CopyFromTruncated(
                  ref truncatedBytes
                , tooManyBytes
            );

            Assert.AreEqual(CopyError.Truncation, truncatedByteResult);
            Assert.AreEqual(truncatedBytes.Capacity, truncatedBytes.Length);

            FixedString32Bytes appendedBytes = "a";
            ReadOnlySpan<byte> suffixBytes = Encoding.UTF8.GetBytes("bc");

            Assert.AreEqual(
                  FormatError.None
                , EncosyFixedStringExtensions.Append(ref appendedBytes, suffixBytes)
            );
            Assert.AreEqual("abc", appendedBytes.ToString());

            var fullBeforeByteAppend = new string('s', appendedBytes.Capacity);
            appendedBytes = fullBeforeByteAppend;
            ReadOnlySpan<byte> finalByte = new byte[] { (byte)'z' };

            Assert.AreEqual(
                  FormatError.Overflow
                , EncosyFixedStringExtensions.Append(ref appendedBytes, finalByte)
            );
            Assert.AreEqual(fullBeforeByteAppend, appendedBytes.ToString());
        }

        [Test]
        public void OutputViewsNativeTextAndFormatting_ReturnExpectedContent()
        {
            FixedString64Bytes value = "abc";
            var destination = new char[3];
            var shortDestination = new char[2];

            Assert.AreEqual(
                  CopyError.None
                , EncosyFixedStringExtensions.CopyTo(value, destination, out var copiedLength)
            );
            Assert.AreEqual(3, copiedLength);
            CollectionAssert.AreEqual(new[] { 'a', 'b', 'c' }, destination);
            Assert.AreEqual(
                  CopyError.Truncation
                , EncosyFixedStringExtensions.CopyTo(value, shortDestination, out _)
            );

            var builder = new StringBuilder("start:");
            EncosyFixedStringExtensions.AppendTo(value, builder, out var appendedLength);

            Assert.AreEqual(3, appendedLength);
            Assert.AreEqual("start:abc", builder.ToString());

            Span<byte> writable = EncosyFixedStringExtensions.AsSpan(ref value);
            writable[0] = (byte)'z';
            ReadOnlySpan<byte> readOnly = EncosyFixedStringExtensions.AsReadOnlySpan(value);

            Assert.AreEqual("zbc", value.ToString());
            CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("zbc"), readOnly.ToArray());

            var formatted = new char[3];
            Assert.IsTrue(
                EncosyFixedStringExtensions.TryFormat(value, formatted, out var charsWritten)
            );
            Assert.AreEqual(3, charsWritten);
            CollectionAssert.AreEqual(new[] { 'z', 'b', 'c' }, formatted);

            var formattedWithArguments = new char[3];
            Assert.IsTrue(
                EncosyFixedStringExtensions.TryFormat(
                      value
                    , formattedWithArguments
                    , out var argumentCharsWritten
                    , "ignored".AsSpan()
                    , null
                )
            );
            Assert.AreEqual(3, argumentCharsWritten);
            CollectionAssert.AreEqual(new[] { 'z', 'b', 'c' }, formattedWithArguments);

            var tooShort = new char[2];
            Assert.IsFalse(
                EncosyFixedStringExtensions.TryFormat(value, tooShort, out var failedCharsWritten)
            );
            Assert.AreEqual(0, failedCharsWritten);

            NativeText allocatorText = default;
            NativeText handleText = default;

            try
            {
                allocatorText = EncosyFixedStringExtensions.ToNativeText(
                      value
                    , Allocator.Persistent
                );
                handleText = EncosyFixedStringExtensions.ToNativeText(
                      value
                    , AllocatorManager.Persistent
                );

                Assert.AreEqual("zbc", allocatorText.ToString());
                Assert.AreEqual("zbc", handleText.ToString());
            }
            finally
            {
                if (allocatorText.IsCreated)
                {
                    allocatorText.Dispose();
                }

                if (handleText.IsCreated)
                {
                    handleText.Dispose();
                }
            }
        }
    }
}

#endif
