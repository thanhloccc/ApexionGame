namespace EncosyTower.Samples.Persistence.Shared;

public readonly record struct Changed<T>(T New, T Previous);
