using EncosyTower.LowLevel.Unsafe;

namespace System.Runtime.CompilerServices.Exposed;

internal static class UnsafeExposed
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static void* AsPointer<T>(ref T value)
    {
        // SAFETY: The exposed API intentionally returns the caller's managed reference as a raw pointer.
        unsafe
        {
            return Unsafe.AsPointer(ref value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int SizeOf<T>()
    {
        return Unsafe.SizeOf<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T As<T>(object? value) where T : class?
    {
        return Unsafe.As<T>(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref TTo As<TFrom, TTo>(ref TFrom source)
    {
        return ref Unsafe.As<TFrom, TTo>(ref source);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T Add<T>(ref T source, int elementOffset)
    {
        return ref Unsafe.Add(ref source, elementOffset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T Add<T>(ref T source, IntPtr elementOffset)
    {
        return ref Unsafe.Add(ref source, elementOffset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static void* Add<T>(void* source, int elementOffset)
    {
        // SAFETY: The caller owns the source pointer and supplies an element offset within its allocation.
        unsafe
        {
            return Unsafe.Add<T>(source, elementOffset);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AreSame<T>(ref T left, ref T right)
    {
        return Unsafe.AreSame(ref left, ref right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAddressLessThan<T>(ref T left, ref T right)
    {
        return Unsafe.IsAddressLessThan(ref left, ref right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InitBlockUnaligned(ref byte startAddress, byte value, uint byteCount)
    {
        Unsafe.InitBlockUnaligned(ref startAddress, value, byteCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T ReadUnaligned<T>(ref byte source)
    {
        return Unsafe.ReadUnaligned<T>(ref source);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUnaligned<T>(ref byte destination, T value)
    {
        Unsafe.WriteUnaligned(ref destination, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T AddByteOffset<T>(ref T source, IntPtr byteOffset)
    {
        return ref Unsafe.AddByteOffset(ref source, byteOffset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static T Read<T>(void* source)
    {
        // SAFETY: The caller supplies a live, correctly aligned pointer to a T.
        unsafe
        {
            return Unsafe.Read<T>(source);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static ref T AsRef<T>(void* source)
    {
        // SAFETY: The caller keeps the source allocation alive for the returned reference.
        unsafe
        {
            return ref Unsafe.AsRef<T>(source);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T AsRef<T>(in T source)
    {
        return ref Unsafe.AsRef(in source);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static ref T NullRef<T>()
    {
        // SAFETY: This intentionally exposes the null-reference sentinel used by the low-level API.
        unsafe
        {
            return ref ILSupport.NullRef<T>();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe static bool IsNullRef<T>(in T source)
    {
        // SAFETY: The low-level API intentionally compares the reference against its null sentinel.
        unsafe
        {
            return ILSupport.IsNullRef(source);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SkipInit<T>(out T value)
    {
        ILSupport.SkipInit(out value);
    }
}
