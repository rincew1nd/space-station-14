using System.Linq;
using Content.Server.CartridgeLoader;
using Content.Shared.CartridgeLoader;
using Content.Shared.MessengerCartridge;

namespace Content.Server.MessengerCartridge;

/// <summary>
///     This system handles messenger cartridge program that is installed in PDA.
/// </summary>
public sealed class MessengerCartridgeSystem : SharedMessengerCartridgeSystem
{
    [Dependency] private readonly CartridgeLoaderSystem? _cartridgeLoaderSystem = default!;
    [Dependency] private readonly MessageServerSystem _messageServerSystem = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Logger
        _sawmill = Logger.GetSawmill(typeof(MessengerCartridgeSystem).ToString());

        // UI events
        SubscribeLocalEvent<MessengerCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<MessengerCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
    }

    #region UI events

    /// <summary>
    ///     Gets sent to program / cartridge entities when the ui is ready to be updated by the cartridge.
    /// </summary>
    private void OnUiReady(EntityUid uid, MessengerCartridgeComponent? component, CartridgeUiReadyEvent args)
    {
        UpdateUiState(uid, args.Loader, component, MessengerCartridgeUiEventType.GetChatContacts);
    }

    /// <summary>
    ///     Received a message from UI to process.
    /// </summary>
    private void OnUiMessage(EntityUid uid, MessengerCartridgeComponent? component, CartridgeMessageEvent args)
    {
        if (args is not MessengerCartridgeUiEvent message)
            return;

        UpdateUiState(uid, args.LoaderUid, component, message.Type, message.Args);
    }

    /// <summary>
    ///     Update UI state according to received event.
    /// </summary>
    private void UpdateUiState(
        EntityUid uid,
        EntityUid loaderUid,
        MessengerCartridgeComponent? component,
        MessengerCartridgeUiEventType messageType,
        object? messageArgs = null)
    {
        if (!Resolve(uid, ref component))
            return;

        MessengerCartridgeUiState? state = null;
        try
        {
            state = messageType switch
            {
                MessengerCartridgeUiEventType.GetChatContacts => GetChatContacts(component),
                MessengerCartridgeUiEventType.GetChatHistory => GetChatHistory(component, messageArgs),
                MessengerCartridgeUiEventType.SendMessage => SendMessage(component, messageArgs),
                MessengerCartridgeUiEventType.ChangeOnlineState => SetOnlineStatus(component),
                _ => throw new ArgumentException("Unexpected message type")
            };
        }
        catch (Exception e)
        {
            _sawmill.Error(
                "Error during processing of the MessengerCartridge({uid}) event({messageType}): {Message}",
                uid,
                messageType,
                e.Message);
        }

        if (state == null)
            return;

        _cartridgeLoaderSystem?.UpdateCartridgeUiState(loaderUid, state);
    }

    #endregion

    #region Methods for constuction of view models for UI

    /// <summary>
    ///     Get available chats on network and chats from history.
    /// </summary>
    private MessengerCartridgeUiState GetChatContacts(MessengerCartridgeComponent component)
    {
        var chats = _messageServerSystem.GetChats(component);
        return new MessengerCartridgeUiState()
        {
            UpdateEventType = MessengerCartridgeUiEventType.GetChatContacts,
            Chats = chats.ToList(),
            IsOnline = component.IsOnline
        };
    }

    /// <summary>
    ///     Get a chat history for a messenger.
    /// </summary>
    private MessengerCartridgeUiState GetChatHistory(MessengerCartridgeComponent component, object? messageArgs)
    {
        if (messageArgs is not EntityUid idUid)
            return new MessengerCartridgeUiState();

        var history = _messageServerSystem.GetChatHistory(component, idUid);
        return new MessengerCartridgeUiState()
        {
            UpdateEventType = MessengerCartridgeUiEventType.GetChatHistory,
            CurrentOpenChat = history.Item1,
            Messages = history.Item2.ToList()
        };
    }

    /// <summary>
    ///     Send a message to a chat.
    /// </summary>
    private MessengerCartridgeUiState? SendMessage(MessengerCartridgeComponent component, object? messageArgs)
    {
        if (messageArgs is not MessengerCartridgeUiEventNewMessage message)
            return new MessengerCartridgeUiState() { UpdateEventType = MessengerCartridgeUiEventType.Unknown };

        var isSuccessful = _messageServerSystem.SendMessage(component, message.IdUid, message.Text);
        return isSuccessful
            ? null
            : new MessengerCartridgeUiState()
            {
                UpdateEventType = MessengerCartridgeUiEventType.PopupMessage,
                PopupMessageText = "messenger-program-failed-delivery"
            };
    }

    /// <summary>
    ///     Turn on/off messenger cartridge.
    /// </summary>
    private MessengerCartridgeUiState SetOnlineStatus(MessengerCartridgeComponent component)
    {
        component.IsOnline = !component.IsOnline;
        return new MessengerCartridgeUiState
        {
            UpdateEventType = MessengerCartridgeUiEventType.ChangeOnlineState,
            IsOnline = !component.IsOnline
        };
    }

    #endregion

    #region External methods and processors

    /// <summary>
    ///     Update the state of <see cref="MessengerCartridgeComponent"/>.
    /// </summary>
    public void UpdateContactInfo(
        EntityUid station,
        MessengerCartridgeComponent component,
        bool isOnline,
        EntityUid? idCard,
        EntityUid? cartridgeLoaderUid,
        string? fullName)
    {
        component.StationUid = station;
        if (isOnline && idCard.HasValue && cartridgeLoaderUid.HasValue)
        {
            component.IsOnline = true;
            component.CurrentOwner = new MessengerContact(idCard.Value, cartridgeLoaderUid.Value, fullName!);
        }
        else
        {
            component.IsOnline = false;
            component.CurrentOwner = null;
        }

        Dirty(component);
    }

    public void NewMessageAlert(MessengerCartridgeComponent component, string senderFullName)
    {
        if (component.CurrentOwner == null)
            return;

        _cartridgeLoaderSystem?.UpdateCartridgeUiState(
            component.CurrentOwner.CartridgeLoaderUid,
            new MessengerCartridgeUiState()
            {
                UpdateEventType = MessengerCartridgeUiEventType.PopupMessage,
                PopupMessageText = $"New message from {senderFullName}"
            });
    }

    #endregion
}
