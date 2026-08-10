namespace EncosyTower.Buffers
{
    public interface IBufferProvider<TBuffer, T>
        where TBuffer : IBuffer<T>
    {
        ref TBuffer Buffer { get; }

        ref int Count { get; }

        ref int Version { get; }
    }
}
