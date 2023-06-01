using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges.Messenger;

[Serializable, NetSerializable]
public sealed class MessengerSendUiMessageEvent : CartridgeMessageEvent
{
    public readonly EntityUid To;
    public readonly string Message;

    public MessengerSendUiMessageEvent(EntityUid to, string message)
    {
        To = to;
        Message = message;
    }
}

[Serializable, NetSerializable]
public sealed class MessengerOpenHistoryUiMessageEvent : CartridgeMessageEvent
{
    public readonly EntityUid From;

    public MessengerOpenHistoryUiMessageEvent(EntityUid from)
    {
        From = from;
    }
}

[Serializable, NetSerializable]
public sealed class MessengerChatsListUiMessageEvent : CartridgeMessageEvent { }
