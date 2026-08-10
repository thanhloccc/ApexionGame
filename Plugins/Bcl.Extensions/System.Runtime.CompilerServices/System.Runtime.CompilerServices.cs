#pragma warning disable

using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices;

public static class Unsafe
{
    public static ref T Add<T>(ref T source, int elementOffset)
    {
        throw new NotImplementedException();
    }

    public static ref T Add<T>(ref T source, IntPtr elementOffset)
    {
        throw new NotImplementedException();
    }

    /// <safety>source must be a live pointer and elementOffset must remain within its allocation.</safety>
    public unsafe static void* Add<T>(void* source, int elementOffset)
    {
        throw new NotImplementedException();
    }

    public static ref T AddByteOffset<T>(ref T source, IntPtr byteOffset)
    {
        throw new NotImplementedException();
    }

    public static bool AreSame<T>(ref T left, ref T right)
    {
        throw new NotImplementedException();
    }

    public static T As<T>(object? o) where T : class?
    {
        throw new NotImplementedException();
    }

    public static ref TTo As<TFrom, TTo>(ref TFrom source)
    {
        throw new NotImplementedException();
    }

    /// <safety>The returned pointer is valid only while value remains alive.</safety>
    public unsafe static void* AsPointer<T>(ref T value)
    {
        throw new NotImplementedException();
    }

    /// <safety>source must point to a live T for the lifetime of the returned reference.</safety>
    public unsafe static ref T AsRef<T>(void* source)
    {
        throw new NotImplementedException();
    }

    public static ref T AsRef<T>(in T source)
    {
        throw new NotImplementedException();
    }

    public static void InitBlockUnaligned(ref byte startAddress, byte value, uint byteCount)
    {
        throw new NotImplementedException();
    }

    /// <safety>source must point to a live, readable T.</safety>
    public unsafe static T Read<T>(void* source)
    {
        throw new NotImplementedException();
    }

    public static T ReadUnaligned<T>(ref byte source)
    {
        throw new NotImplementedException();
    }

    public static int SizeOf<T>()
    {
        throw new NotImplementedException();
    }

    public static void WriteUnaligned<T>(ref byte destination, T value)
    {
        throw new NotImplementedException();
    }

    public static bool IsAddressLessThan<T>(ref T left, ref T right)
    {
        throw new NotImplementedException();
    }
}
