using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class MessengerMainMenuUiState : BoundUserInterfaceState
{
    public Dictionary<EntityUid, MessengerMessage> LastMessage;

    public MessengerMainMenuUiState(Dictionary<EntityUid, MessengerMessage> lastMessage)
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
    public string? Name;
    public readonly TimeSpan Time;
    public readonly string Text;

    public MessengerMessage(TimeSpan time, string text)
    {
        Name = "unknown";
        Time = time;
        Text = text;
    }
}

[Serializable, NetSerializable, DataRecord]
public sealed class MessengerHistoryMessage
{
    public readonly EntityUid From;
    public readonly EntityUid To;
    public readonly string? ToName;
    public readonly TimeSpan Time;
    public readonly string Text;

    public MessengerHistoryMessage(EntityUid from, EntityUid to, string? toName, TimeSpan time, string text)
    {
        From = from;
        To = to;
        ToName = toName;
        if (string.IsNullOrEmpty(ToName))
        {
            ToName = "unknown";
        }
        Time = time;
        Text = text;
    }
}
