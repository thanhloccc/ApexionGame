using EncosyTower.PubSub;

namespace EncosyTower.Samples.Persistence.SimpleUsage;

internal readonly record struct ScreenScope();

internal readonly record struct ShowMainMenuScreenMsg() : IMessage;

internal readonly record struct ShowLobbyScreenMsg() : IMessage;
