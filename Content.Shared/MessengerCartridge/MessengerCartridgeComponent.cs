using Robust.Shared.Serialization;

namespace Content.Shared.MessengerCartridge;

/// <summary>
///     PDA Messenger program.
/// </summary>
[RegisterComponent]
public sealed class MessengerCartridgeComponent : Component
{
    /// <summary>
    ///     Current station uid of a component.
    /// </summary>
    [ViewVariables]
    public EntityUid StationUid { get; set; }

    /// <summary>
    ///     Current contact that depends on inserted IdCard in PDA IdSlot.
    /// </summary>
    [DataField("currentOwner", serverOnly: true)]
    public MessengerContact? CurrentOwner { get; set; }

    /// <summary>
    ///     Is IdCard PDA messenger program online.
    ///     PDA owner can turn it on/off through program interface.
    /// </summary>
    [DataField("isOnline")]
    public bool IsOnline { get; set; }
}

/// <summary>
///     Messenger contact that depends on inserted IdCard in PDA IdSlot.
/// </summary>
[Serializable, NetSerializable]
public sealed class MessengerContact
{
    /// <summary>
    ///     IdCard uid.
    /// </summary>
    public EntityUid IdCardUid { get; set; }

    /// <summary>
    ///     Full name (Name + Surname).
    /// </summary>
    public string FullName { get; set; }

    /// <summary>
    ///     .ctor
    /// </summary>
    public MessengerContact(EntityUid idCardUid, string fullName)
    {
        IdCardUid = idCardUid;
        FullName = fullName;
    }
}

[Serializable, NetSerializable]
public sealed class MessengerContactMessage
{
    /// <summary>
    ///     Hashcode for a message history.
    /// </summary>
    private readonly int _hashCode;

    /// <summary>
    ///     Sent time from start of the round.
    /// </summary>
    public TimeSpan SentTime { get; }

    /// <summary>
    ///     Sender IdCard uid.
    /// </summary>
    public EntityUid From { get; }

    /// <summary>
    ///     Receiver IdCard uid.
    /// </summary>
    public EntityUid To { get; }

    /// <summary>
    ///     Message text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    ///     .ctor
    /// </summary>
    public MessengerContactMessage(TimeSpan sentTime, EntityUid from, EntityUid to, string text)
    {
        SentTime = sentTime;
        From = from;
        To = to;
        Text = text;

        _hashCode = (Math.Min(from.GetHashCode(), to.GetHashCode()), Math.Max(from.GetHashCode(), to.GetHashCode()))
            .GetHashCode();
    }

    /// <summary>
    ///     Get a HashCode of MessengerContactMessage from Sender and Receiver EntityUid.
    /// </summary>
    public override int GetHashCode()
    {
        return _hashCode;
    }
}
