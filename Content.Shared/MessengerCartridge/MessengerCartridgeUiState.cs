using Robust.Shared.Serialization;

namespace Content.Shared.MessengerCartridge;

/// <summary>
///     UI State for <see cref="MessengerCartridgeComponent"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed class MessengerCartridgeUiState : BoundUserInterfaceState
{
    /// <summary>
    ///     Currently available contacts.
    /// </summary>
    public List<MessengerContact> Chats { get; set; } = new();

    /// <summary>
    ///     Currently opened chat.
    /// </summary>
    public MessengerContact? CurrentOpenChat { get; set; }

    /// <summary>
    ///     Messages from currently opened chat.
    /// </summary>
    public List<MessengerContactMessage> Messages { get; set; } = new();

    /// <summary>
    ///     Online status.
    /// </summary>
    public bool IsOnline { get; set; } = false;
}
