using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges.Messenger;

[Serializable, NetSerializable]
public sealed class MessengerMainMenuUiState : BoundUserInterfaceState
{
    public Dictionary<EntityUid, MessengerMessage?> LastMessage;

    public MessengerMainMenuUiState(Dictionary<EntityUid, MessengerMessage?> lastMessage)
    {
        LastMessage = lastMessage;
    }
}

[Serializable, NetSerializable]
public sealed class MessengerHistoryUiState : BoundUserInterfaceState
{
    public EntityUid Receiver;
    public List<MessengerHistoryMessage> History;

    public MessengerHistoryUiState(EntityUid receiver, List<MessengerHistoryMessage> history)
    {
        Receiver = receiver;
        History = history;
    }
}

[Serializable, NetSerializable, DataRecord]
public sealed class MessengerMessage
{
    public string Name = "Unknown";
    public readonly TimeSpan? SentTime;
    public readonly bool? IsIncoming;
    public readonly string? Text;

    public MessengerMessage(TimeSpan? sentTime, bool? isIncoming, string? text)
    {
        SentTime = sentTime;
        IsIncoming = isIncoming;
        Text = text;
    }
}

[Serializable, NetSerializable, DataRecord]
public sealed class MessengerHistoryMessage
{
    public readonly string? ToName;
    public readonly TimeSpan SentTime;
    public readonly bool IsIncoming;
    public readonly string Text;

    public MessengerHistoryMessage(string? toName, TimeSpan sentTime, bool isIncoming, string text)
    {
        ToName = toName ?? "Unknown";
        SentTime = sentTime;
        IsIncoming = isIncoming;
        Text = text;
    }
}
