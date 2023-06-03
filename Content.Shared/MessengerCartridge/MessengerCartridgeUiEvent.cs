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
    public readonly object Args;

    /// <summary>
    ///     .ctor
    /// </summary>
    public MessengerCartridgeUiEvent(MessengerCartridgeUiEventType type, object args)
    {
        Type = type;
        Args = args;
    }
}

/// <summary>
///     Message cartridge UI event types.
/// </summary>
[Serializable, NetSerializable]
public enum MessengerCartridgeUiEventType
{
    GetChatContacts,
    GetChatHistory,
    SendMessage,
    ChangeOnlineState
}
