using Content.Shared.CartridgeLoader;
using Robust.Shared.Serialization;

namespace Content.Shared.MessengerCartridge;

/// <summary>
///     Message cartridge UI events.
/// </summary>
[Serializable, NetSerializable]
public sealed class MessengerCartridgeUiEvent : CartridgeMessageEvent
{
    /// <summary>
    ///     Event type.
    /// </summary>
    public readonly MessengerCartridgeUiEventType Type;

    /// <summary>
    ///     Event args.
    /// </summary>
    public readonly object? Args;

    /// <summary>
    ///     .ctor
    /// </summary>
    public MessengerCartridgeUiEvent(MessengerCartridgeUiEventType type, object? args = null)
    {
        Type = type;
        Args = args;
    }
}

/// <summary>
///     Message cartridge UI events.
/// </summary>
[Serializable, NetSerializable]
public sealed class MessengerCartridgeUiEventNewMessage
{
    /// <summary>
    ///     Receiver Uid.
    /// </summary>
    public readonly EntityUid IdUid;

    /// <summary>
    ///     Message text.
    /// </summary>
    public readonly string Text;

    /// <summary>
    ///     .ctor
    /// </summary>
    public MessengerCartridgeUiEventNewMessage(EntityUid idUid, string text)
    {
        IdUid = idUid;
        Text = text;
    }
}

/// <summary>
///     Message cartridge UI event types.
/// </summary>
[Serializable, NetSerializable]
public enum MessengerCartridgeUiEventType : byte
{
    Unknown = 0,

    /// <summary>
    ///     Client => Server messages.
    /// </summary>
    GetChatContacts = 1,
    GetChatHistory = 2,
    SendMessage = 3,
    ChangeOnlineState = 5,

    /// <summary>
    ///     Server => Client messages.
    /// </summary>
    NewMessage = 11,
    PopupMessage = 12,
}
