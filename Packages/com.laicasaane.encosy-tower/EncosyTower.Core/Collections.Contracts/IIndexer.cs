namespace EncosyTower.Collections
{
    public interface IReadOnlyIndexer<T>
    {
        T this[int index] { get; }
    }

    public interface IIndexer<T> : IReadOnlyIndexer<T>
    {
        new T this[int index] { get; set; }
    }

    public interface IRefIndexer<T>
    {
        ref T this[int index] { get; }
    }

    public interface IRefReadOnlyIndexer<T>
    {
        ref readonly T this[int index] { get; }
    }
}
