using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.GameTicking;
using Content.Shared.Access.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.PDA;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class MessengerCartridgeSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly CartridgeLoaderSystem? _cartridgeLoaderSystem = default!;
    [Dependency] private readonly DeviceNetworkSystem? _deviceNetworkSystem = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    public const string MessengerCommand = "messenger_command";
    public const string MessengerCommandInstalled = "messenger_command_installed";
    public const string MessengerCommandRemoved = "messenger_command_removed";
    public const string MessengerCommandActivated = "messenger_command_activated";
    public const string MessengerCommandDeactivated = "messenger_command_deactivated";
    public const string MessengerCommandOnline = "messenger_command_online";
    public const string MessengerCommandReceiveMessage = "messenger_command_receive_message";

    public const string MessengerMessage = "messenger_message";


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MessengerCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<MessengerCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);

        SubscribeLocalEvent<MessengerCartridgeComponent, CartridgeAddedEvent>(OnInstall);
        SubscribeLocalEvent<MessengerCartridgeComponent, CartridgeRemovedEvent>(OnRemoved);

        SubscribeLocalEvent<DeviceNetworkComponent, DeviceNetworkPacketEvent>(OnNetworkPacket);
    }

    /// <summary>
    /// Do things if DeviceNetworkComponent receive a MessengerCommand in payload
    /// For now in most cases require a MessengerComponent
    /// On MessengerCommandInstalled add to entity with MessengerComponent new receiver
    ///     and call back to sender MessengerCommandOnline command
    /// On MessengerCommandOnline add a new receiver to MessengerComponent and update chats list
    /// On MessengerCommandReceiveMessage add new (not empty) message to received messages
    ///     and update chants history for receiver and sender
    /// On MessengerCommandRemoved todo
    /// </summary>
    /// <param name="loaderUid">The EntityUid of the DeviceNetworkComponent component</param>
    /// <param name="component">DeviceNetworkComponent entity mas be a part of network</param>
    /// <param name="args">DeviceNetworkPacketEvent</param>
    private void OnNetworkPacket(EntityUid loaderUid, DeviceNetworkComponent component, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(MessengerCommand, out string? msg))
            return;

        switch (msg)
        {
            case MessengerCommandInstalled:
                if (GetMessengerComponent(loaderUid, out var messenger))
                {
                    if (!messenger.ReceivedMessages.ContainsKey(args.Sender))
                        messenger.ReceivedMessages.Add(args.Sender, new List<Message>());
                }

                _deviceNetworkSystem?.QueuePacket(loaderUid, args.SenderAddress, new NetworkPayload
                {
                    [MessengerCommand] = MessengerCommandOnline,
                });
                break;
            case MessengerCommandOnline:
                if (GetMessengerComponent(loaderUid, out messenger))
                {
                    if (!messenger.ReceivedMessages.ContainsKey(args.Sender))
                        messenger.ReceivedMessages.Add(args.Sender,   new List<Message>());

                    UpdateChatsListUiState(loaderUid, messenger);
                }
                break;
            case MessengerCommandReceiveMessage:
                if (GetMessengerComponent(loaderUid, out messenger))
                {
                    if (!args.Data.TryGetValue(MessengerMessage, out string? message))
                        break;

                    if (message == "")
                        break;

                    if (!messenger.ReceivedMessages.ContainsKey(args.Sender))
                        messenger.ReceivedMessages.Add(args.Sender, new List<Message>());

                    var currentTime = _gameTicker.RoundDuration();

                    messenger.ReceivedMessages[args.Sender].Add(new Message(currentTime, message));

                    if (messenger.ContactInfo != null)
                    {
                        UpdateChatHistoryUiState(messenger.ContactInfo.Loader, args.Sender, messenger);
                        if (GetMessengerComponent(args.Sender, out var senderMessenger))
                            UpdateChatHistoryUiState(args.Sender, messenger.ContactInfo.Loader, senderMessenger);
                    }
                }
                break;
            case MessengerCommandRemoved:
                break;
        }
    }

    /// <summary>
    /// When program installed add contact info and broadcast MessengerCommandInstalled command
    /// If it has DeviceNetworkComponent add NetAddress
    /// if it has PDAComponent add Name
    /// </summary>
    /// <param name="uid">The EntityUid of the MessengerCartridgeComponent component</param>
    /// <param name="component">MessengerCartridgeComponent component of program</param>
    /// <param name="args">CartridgeRemovedEvent</param>
    private void OnInstall(EntityUid uid, MessengerCartridgeComponent component, CartridgeAddedEvent args)
    {
        if (component.IsInstalled)
            return;

        component.IsInstalled = true;

        var contactInfo = new Contact(args.Loader);

        if (_entityManager.TryGetComponent<DeviceNetworkComponent>(args.Loader, out var sender))
            contactInfo.NetAddress = sender.Address;

        if (_entityManager.TryGetComponent<PDAComponent>(args.Loader, out var pda))
        {
            if (pda.ContainedID != null)
                contactInfo.Name = pda.ContainedID.FullName + " " + pda.ContainedID.JobTitle;
        }

        component.ContactInfo = contactInfo;

        BroadcastCommand(args.Loader, MessengerCommandInstalled);
    }

    /// <summary>
    /// Broadcast a command that the program has been removed
    /// </summary>
    /// <param name="uid">The EntityUid of the MessengerCartridgeComponent component</param>
    /// <param name="component">MessengerCartridgeComponent component of program</param>
    /// <param name="args">CartridgeRemovedEvent</param>
    private void OnRemoved(EntityUid uid, MessengerCartridgeComponent component, CartridgeRemovedEvent args)
    {
        if (!component.IsInstalled)
            return;

        component.IsInstalled = false;

        BroadcastCommand(args.Loader, MessengerCommandRemoved);
    }

    /// <summary>
    /// Broadcast a command to all loaders with same network
    /// </summary>
    /// <param name="loaderUid">The EntityUid of the program loader</param>
    /// <param name="command">Messenger command</param>
    private void BroadcastCommand(EntityUid loaderUid, string command)
    {
        _deviceNetworkSystem?.QueuePacket(loaderUid, null, new NetworkPayload
        {
            [MessengerCommand] = command,
        });
    }

    /// <summary>
    /// Send chants list on open UI of program
    /// </summary>
    /// <param name="uid">The EntityUid of the MessengerCartridgeComponent component</param>
    /// <param name="component">MessengerCartridgeComponent component of program</param>
    /// <param name="args">CartridgeMessageEvent event of program</param>
    private void OnUiReady(EntityUid uid, MessengerCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        UpdateChatsListUiState(args.Loader, component);
    }

    /// <summary>
    /// Do things when UI calling
    /// On MessengerSendUiMessageEvent if To has DeviceNetworkComponent queue a packet with message
    /// On MessengerChatsListUiMessageEvent send state with chants list
    /// On MessengerOpenHistoryUiMessageEvent send state with chant history
    /// </summary>
    /// <param name="uid">The EntityUid of the MessengerCartridgeComponent component</param>
    /// <param name="component">MessengerCartridgeComponent component of program</param>
    /// <param name="args">CartridgeMessageEvent event of program</param>
    private void OnUiMessage(EntityUid uid, MessengerCartridgeComponent component, CartridgeMessageEvent args)
    {
        switch (args)
        {
            case MessengerSendUiMessageEvent messengerSendUiMessageEvent:
                if (messengerSendUiMessageEvent.Message == "")
                    return;

                if (!_entityManager.TryGetComponent<DeviceNetworkComponent>(messengerSendUiMessageEvent.To,
                        out var receiver))
                    return;

                _deviceNetworkSystem?.QueuePacket(args.LoaderUid, receiver.Address, new NetworkPayload
                {
                    [MessengerCommand] = MessengerCommandReceiveMessage,
                    [MessengerMessage] = messengerSendUiMessageEvent.Message,
                });

                break;
            case MessengerChatsListUiMessageEvent messengerChatsListUiMessageEvent:
                UpdateChatsListUiState(args.LoaderUid, component);
                break;
            case MessengerOpenHistoryUiMessageEvent messengerOpenHistoryUiMessageEvent:
                UpdateChatHistoryUiState(args.LoaderUid, messengerOpenHistoryUiMessageEvent.From, component);
                break;

        }
    }

    /// <summary>
    /// Call UpdateCartridgeUiState with MessengerMainMenuUiState,
    /// when new message coming or when loader ask a chats list
    /// </summary>
    /// <param name="loaderUid">The EntityUid of the program loader</param>
    /// <param name="component">MessengerCartridgeComponent component of program loader</param>
    private void UpdateChatsListUiState(EntityUid loaderUid, MessengerCartridgeComponent component)
    {
        var lastMessages = new Dictionary<EntityUid, MessengerMessage>();
        foreach (var (from, messages) in component.ReceivedMessages)
        {
            var m = messages.LastOrDefault( new Message(TimeSpan.Zero, ""));
            var mm = new MessengerMessage( m.Time, m.Text);

            if (GetMessengerComponent(from, out var receiverMessenger))
            {
                if (receiverMessenger.ContactInfo != null)
                {
                    if (receiverMessenger.ContactInfo.Name !=  "")
                        mm.Name = receiverMessenger.ContactInfo.Name;
                }
            }

            lastMessages.Add(from, mm);
        }

        var state = new MessengerMainMenuUiState(lastMessages);
        _cartridgeLoaderSystem?.UpdateCartridgeUiState(loaderUid, state);
    }

    /// <summary>
    /// Call UpdateCartridgeUiState with MessengerHistoryUiState,
    /// when new message coming or when loader ask a history
    /// </summary>
    /// <param name="loaderUid">The EntityUid of the program loader</param>
    /// <param name="receiver">The EntityUid of the program which messages were sent, if there are messages</param>
    /// <param name="component">MessengerCartridgeComponent component of program loader</param>
    private void UpdateChatHistoryUiState(EntityUid loaderUid, EntityUid receiver, MessengerCartridgeComponent component)
    {
        var chatHistory = new List<MessengerHistoryMessage>();

        // get messenger component for requested messages receiver if exist
        if (GetMessengerComponent(receiver, out var receiverMessenger))
        {
            // select messages loaderUid history, messages sent by loaderUid to receiver
            var loaderMessages = receiverMessenger.ReceivedMessages[loaderUid];
            chatHistory.AddRange(loaderMessages.Select(msg => new MessengerHistoryMessage(loaderUid, receiver, receiverMessenger.ContactInfo?.Name, msg.Time, msg.Text)));
        }

        // select messages receiver history, messages sent by receiver to loaderUid
        var receiverMessages = component.ReceivedMessages[receiver];
        chatHistory.AddRange(receiverMessages.Select(msg => new MessengerHistoryMessage(receiver, loaderUid, component.ContactInfo?.Name, msg.Time, msg.Text)));

        // sort by time
        chatHistory.Sort((x, y) => x.Time.CompareTo(y.Time));

        var state = new MessengerHistoryUiState(receiver, chatHistory);
        _cartridgeLoaderSystem?.UpdateCartridgeUiState(loaderUid, state);
    }

    /// <summary>
    /// Get Messenger component for current entity, which should contain
    /// CartridgeLoaderComponent
    /// and MessengerCartridgeComponent installed in CartridgeLoaderComponent
    /// </summary>
    /// <param name="uid">The EntityUid of the program loader</param>
    /// <param name="messenger">out MessengerCartridgeComponent</param>
    /// <returns>Returns true when the component was successfully received.</returns>
    private bool GetMessengerComponent(EntityUid uid, [NotNullWhen(true)] out MessengerCartridgeComponent? messenger)
    {
        messenger = null;

        if (!_entityManager.TryGetComponent<CartridgeLoaderComponent>(uid, out var cartridge))
            return false;

        if (_cartridgeLoaderSystem == null)
            return false;

        var programs = _cartridgeLoaderSystem.GetAvailablePrograms(uid, cartridge);

        foreach (var programsEntityUid in programs)
        {
            if (!_entityManager.TryGetComponent<MessengerCartridgeComponent>(programsEntityUid, out messenger))
                continue;

            return true;
        }

        return false;
    }
}
