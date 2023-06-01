using Content.Shared.Access.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges.Messenger;

/// <summary>
///     Messenger program for PDA.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
[Access(typeof(SharedMessengerCartridgeSystem))]
public sealed partial class MessengerCartridgeComponent : Component
{
    /// <summary>
    ///     Is communication works right now (is IdCard inserted).
    /// </summary>
    [DataField("isOnline")]
    [AutoNetworkedField]
    public bool IsOnline { get; set; }

    /// <summary>
    ///     Owner contact info (from IdCard in PDA).
    /// </summary>
    [DataField("contactInfo")]
    [AutoNetworkedField]
    public Contact? ContactInfo { get; set; }

    /// <summary>
    ///     Chat history.
    ///     Chat history is unique per IdCard full name.
    ///     If somebody wants to read chat history, he should insert a correct id card to correct PDA.
    ///     TODO hacking.
    /// </summary>
    [DataField("chatHistory")]
    [AutoNetworkedField]
    public Dictionary<string, Dictionary<(EntityUid Uid, string Name), List<Message>>> ChatHistory { get; set; } = new();
}

/// <summary>
///     Crew member contact data.
/// </summary>
[Serializable, NetSerializable]
public sealed class Contact
{
    /// <summary>
    ///     Identifier of a <see cref="MessengerCartridgeComponent"/>.
    /// </summary>
    public EntityUid? MessengerId { get; }

    /// <summary>
    ///     Full name of a crew member (Name and Surname).
    /// </summary>
    public string? Name { get; }

    /// <summary>
    ///     Address of a DeviceNetworkComponent component.
    /// </summary>
    public string? NetworkAddress { get; }

    public Contact(EntityUid? messengerId, string? name, string? networkAddress)
    {
        MessengerId = messengerId;
        Name = name;
        NetworkAddress = networkAddress;
    }
}

/// <summary>
///     Personal message in messenger.
/// </summary>
[Serializable, NetSerializable]
public sealed class Message
{
    /// <summary>
    ///     Sent time (after a server start)
    /// </summary>
    public TimeSpan SentTime { get; }

    /// <summary>
    ///     Direction of the message.
    ///     false - Owner is a sender.
    ///     true  - Owner is a receiver.
    /// </summary>
    public bool IsIncoming { get; }

    /// <summary>
    ///     Text of the message.
    /// </summary>
    public string Text { get; }

    public Message(TimeSpan sentTime, bool isIncoming, string text)
    {
        SentTime = sentTime;
        IsIncoming = isIncoming;
        Text = text;
    }
}

