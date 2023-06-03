namespace Content.Server.MessengerCartridge;

/// <summary>
///     Station messenger history component for storing message history.
/// </summary>
[Access(typeof(MessageServerSystem))]
[RegisterComponent]
public sealed class StationMessageHistoryComponent : Component
{
    /// <summary>
    ///     Message history.
    /// </summary>
    [ViewVariables]
    public readonly StationsMessageHistory History = new();
}
