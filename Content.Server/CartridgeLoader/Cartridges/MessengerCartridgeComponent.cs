namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed class MessengerCartridgeComponent : Component
{
    public bool IsInstalled = false;
    public Contact? ContactInfo = default;
    public Dictionary<EntityUid, List<Message>> ReceivedMessages = new();
}


public sealed class Contact
{
    public EntityUid Loader;
    public string? Name;
    public string NetAddress;

    public Contact(EntityUid loader)
    {
        Loader = loader;
        Name = "";
        NetAddress = "";
    }
}

public sealed class Message
{
    public TimeSpan Time;
    public string Text;

    public Message(TimeSpan time, string text)
    {
        Time = time;
        Text = text;
    }
}

