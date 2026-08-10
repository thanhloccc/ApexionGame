namespace EncosyTower.Collections
{
    public interface IHasCapacity
    {
        int Capacity { get; }
    }

    public interface IIncreaseCapacity : IHasCapacity
    {
        int IncreaseCapacityBy(int amount);

        int IncreaseCapacityTo(int newCapacity);
    }
}
