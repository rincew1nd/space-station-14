using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.CartridgeLoader.Cartridges.Messenger;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.GameTicking;
using Content.Shared.Access.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.CartridgeLoader.Cartridges.Messenger;
using Content.Shared.Extensions;
using Content.Shared.PDA;
using Newtonsoft.Json;
using Robust.Shared.Containers;
using Serilog;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class MessengerCartridgeSystem : SharedMessengerCartridgeSystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly CartridgeLoaderSystem? _cartridgeLoaderSystem = default!;
    [Dependency] private readonly DeviceNetworkSystem? _deviceNetworkSystem = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    public const string MessengerCommand = "messenger_command";
    public const string MessengerOwnerName = "messenger_owner_name";
    public const string MessengerMessage = "messenger_message";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MessengerCartridgeComponent, CartridgeRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<PDAComponent, PdaIdCardChangedEvent>(OnPdaIdCardChanged);

        SubscribeLocalEvent<DeviceNetworkComponent, DeviceNetworkPacketEvent>(OnNetworkPacket);

        SubscribeLocalEvent<MessengerCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<MessengerCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
    }

    /// <summary>
    ///     Set <see cref="MessengerCartridgeComponent"/> to be offline.
    /// </summary>
    /// <param name="uid">The EntityUid of the <see cref="MessengerCartridgeComponent"/></param>
    /// <param name="component"><see cref="MessengerCartridgeComponent"/></param>
    /// <param name="args"><see cref="CartridgeRemovedEvent"/></param>
    private void OnRemoved(EntityUid uid, MessengerCartridgeComponent component, CartridgeRemovedEvent args)
    {
        if (!component.IsOnline)
            return;

        UpdateContactInfo(component, false, null, null, null);
        component.IsOnline = false;
    }

    /// <summary>
    ///     Update the state of <see cref="MessengerCartridgeComponent"/>.
    /// </summary>
    private void UpdateContactInfo(MessengerCartridgeComponent component, bool isOnline, EntityUid? cartridgeLoadedId, string? fullName, string? deviceAddress)
    {
        component.IsOnline = isOnline;
        component.ContactInfo = new Contact(cartridgeLoadedId, fullName, deviceAddress);

        if (component.IsOnline && !string.IsNullOrEmpty(fullName))
            component.ChatHistory.TryAdd(fullName, new Dictionary<(EntityUid, string), List<Message>>());

        Dirty(component);

        if (component.IsOnline && cartridgeLoadedId.HasValue)
            BroadcastCommand(cartridgeLoadedId.Value, MessengerEventType.Online);
    }

    /// <summary>
    ///     Change messenger state when IdCard is changed.
    /// </summary>
    /// <param name="pdaUid">The EntityUid of the <see cref="PDAComponent"/></param>
    /// <param name="pda"><see cref="PDAComponent"/></param>
    /// <param name="args"><see cref="PdaIdCardChangedEvent"/></param>
    private void OnPdaIdCardChanged(EntityUid pdaUid, PDAComponent pda, PdaIdCardChangedEvent args)
    {
        if (!GetMessengerComponent(pdaUid, out var messengerCartridge))
            return;

        if (pda.IdSlot.Item == null || !_entityManager.TryGetComponent<IdCardComponent>(pda.IdSlot.Item, out var idCardComponent))
            return;

        UpdateContactInfo(
            messengerCartridge,
            args.IsIdCardInserted,
            pdaUid,
            idCardComponent.FullName,
            messengerCartridge.ContactInfo?.NetworkAddress!);

        var otherMessengers = EntityQuery<MessengerCartridgeComponent>();
        foreach (var otherMessenger in otherMessengers.Where(m => m.IsOnline))
        {
            // We will use Owner to ease our life
            messengerCartridge.ChatHistory[idCardComponent.FullName!].TryAdd(
                (otherMessenger.Owner, otherMessenger.ContactInfo?.Name ?? ""),
                new List<Message>());
        }

        Log.Debug("Contact is {Name}", messengerCartridge.ContactInfo?.Name);
        Logger.Debug("Pda id card inserted {uid}", pdaUid);
    }

    /// <summary>
    ///     Do things if DeviceNetworkComponent receive a MessengerCommand in payload
    /// </summary>
    /// <param name="pdaUid">The EntityUid of the <see cref="PDAComponent"/></param>
    /// <param name="component"><see cref="DeviceNetworkComponent"/></param>
    /// <param name="args"><see cref="DeviceNetworkPacketEvent"/></param>
    private void OnNetworkPacket(EntityUid pdaUid, DeviceNetworkComponent component, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(MessengerCommand, out MessengerEventType? eventType))
            return;

        switch (eventType)
        {
            case MessengerEventType.Online:
                if (GetMessengerComponent(pdaUid, out var messenger))
                {
                    if (!messenger.IsOnline)
                        break;

                    if (messenger.ChatHistory.ContainsKey(messenger.ContactInfo?.Name ?? ""))
                    {
                        Log.Error("BUG: Messenger '{PdaUid}' doesn't have a chat history of '{Name}'", pdaUid,
                            messenger.ContactInfo?.Name);
                    }

                    args.Data.TryGetValue(MessengerCommand, out string? ownerName);

                    if (messenger.ChatHistory[messenger.ContactInfo?.Name!].Keys.All(k => k.Uid != args.Sender))
                        messenger.ChatHistory[messenger.ContactInfo?.Name!].Add((args.Sender, ownerName ?? "Unknown"), new List<Message>());
                }

                break;
            case MessengerEventType.MessageReceived:
                if (GetMessengerComponent(pdaUid, out messenger))
                {
                    if (!args.Data.TryGetValue(MessengerMessage, out string? message) || string.IsNullOrEmpty(message))
                        break;

                    if (messenger.ChatHistory.ContainsKey(messenger.ContactInfo?.Name ?? ""))
                    {
                        Log.Error("BUG: Messenger '{PdaUid}' doesn't have a chat history of '{Name}'", pdaUid,
                            messenger.ContactInfo?.Name);
                    }

                    args.Data.TryGetValue(MessengerCommand, out string? ownerName);
                    if (messenger.ChatHistory[messenger.ContactInfo?.Name!].Keys.All(k => k.Uid != args.Sender))
                        messenger.ChatHistory[messenger.ContactInfo?.Name!].Add((args.Sender, ownerName ?? "Unknown"), new List<Message>());

                    var currentTime = _gameTicker.RoundDuration();

                    messenger.ChatHistory[messenger.ContactInfo?.Name!][(args.Sender, ownerName ?? "Unknown")]
                        .Add(new Message(currentTime, true, message));
                    Dirty(messenger);
                }

                break;
            default:
                Logger.Debug("Unknown messenger command {0}", eventType.ToString());
                break;
        }
    }

    #region UI Update

    /// <summary>
    ///     Send chants list on open UI of program.
    /// </summary>
    /// <param name="uid">The EntityUid of the MessengerCartridgeComponent component</param>
    /// <param name="component">MessengerCartridgeComponent component of program</param>
    /// <param name="args">CartridgeMessageEvent event of program</param>
    private void OnUiReady(EntityUid uid, MessengerCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        UpdateChatsListUiState(args.Loader, component);
    }

    /// <summary>
    ///     Do things when UI calling:
    ///
    ///     On MessengerSendUiMessageEvent if To has DeviceNetworkComponent queue a packet with message
    ///     On MessengerChatsListUiMessageEvent send state with chants list
    ///     On MessengerOpenHistoryUiMessageEvent send state with chant history
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
                    [MessengerCommand] = MessengerEventType.MessageReceived,
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
        var chats = component.ChatHistory[component.ContactInfo?.Name!];

        var lastMessages = new Dictionary<EntityUid, MessengerMessage?>();
        foreach (var (sender, messages) in chats)
        {
            var lastMessage = messages.MaxBy(m => m.SentTime);
            if (lastMessage != null)
            {
                var messageVm = new MessengerMessage(lastMessage.SentTime, lastMessage.IsIncoming, lastMessage.Text);

                if (GetMessengerComponent(sender.Uid, out var receiverMessenger))
                {
                    messageVm.Name = receiverMessenger.ContactInfo?.Name ?? "Unknown";
                }

                lastMessages.Add(sender.Uid, messageVm);
            }
            else
            {
                lastMessages.Add(sender.Uid, null);
            }
        }

        var state = new MessengerMainMenuUiState(lastMessages);
        _cartridgeLoaderSystem?.UpdateCartridgeUiState(loaderUid, state);
    }

    /// <summary>
    ///     Call UpdateCartridgeUiState with MessengerHistoryUiState,
    ///     when new message coming or when loader ask a history
    /// </summary>
    /// <param name="loaderUid">The EntityUid of the program loader</param>
    /// <param name="receiver">The EntityUid of the program which messages were sent, if there are messages</param>
    /// <param name="component">MessengerCartridgeComponent component of program loader</param>
    private void UpdateChatHistoryUiState(EntityUid loaderUid, EntityUid receiver, MessengerCartridgeComponent component)
    {
        var ownerChatHistory = component
            .ChatHistory[component.ContactInfo?.Name ?? ""]
            .FirstOrDefault(k => k.Key.Uid == receiver);

        var chatHistoryVm = ownerChatHistory.Value
            .Select(chatHistory =>
                new MessengerHistoryMessage(
                    ownerChatHistory.Key.Name,
                    chatHistory.SentTime,
                    chatHistory.IsIncoming,
                    chatHistory.Text))
            .ToList();

        var state = new MessengerHistoryUiState(receiver, chatHistoryVm);
        _cartridgeLoaderSystem?.UpdateCartridgeUiState(loaderUid, state);
    }

    #endregion

    #region Helper methodds

    /// <summary>
    ///     Get a <see cref="MessengerCartridgeComponent"/> from hierarchy:
    ///     <see cref="PDAComponent"/>
    ///         |- <see cref="CartridgeLoaderComponent"/>
    ///             |- <see cref="MessengerCartridgeComponent"/>
    /// </summary>
    /// <param name="pdaUid">The EntityUid of the <see cref="PDAComponent"/></param>
    /// <param name="messenger"><see cref="MessengerCartridgeComponent"/></param>
    /// <returns>Returns true when the component was successfully received.</returns>
    private bool GetMessengerComponent(EntityUid pdaUid, [NotNullWhen(true)] out MessengerCartridgeComponent? messenger)
    {
        messenger = null;

        if (!_entityManager.TryGetComponent<CartridgeLoaderComponent>(pdaUid, out var cartridgeLoader))
            return false;

        return EntityManager.TryComp(cartridgeLoader.InstalledPrograms, out messenger);
    }

    /// <summary>
    ///     Broadcast a command to all loaders with same network.
    /// </summary>
    /// <param name="loaderUid">The EntityUid of the program loader</param>
    /// <param name="eventType">Messenger event type</param>
    private void BroadcastCommand(EntityUid loaderUid, MessengerEventType eventType)
    {
        _deviceNetworkSystem?.QueuePacket(
            uid: loaderUid,
            address: null,
            data: new NetworkPayload
            {
                [MessengerCommand] = eventType,
            });
    }

    #endregion
}
