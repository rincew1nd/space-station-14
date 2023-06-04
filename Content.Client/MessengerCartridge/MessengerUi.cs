using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Content.Shared.MessengerCartridge;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.MessengerCartridge;

public sealed class MessengerUi : UIFragment
{
    private MessengerCartridgeUiFragment? _fragment;
    private BoundUserInterface? _userInterface;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new MessengerCartridgeUiFragment();
        _userInterface = userInterface;

        _fragment.ChangeOnlineStatusButtonPressed += () =>
            SendMessengerUpdateEvent(MessengerCartridgeUiEventType.ChangeOnlineState);
        _fragment.OnBackButtonPressed += () =>
            SendMessengerUpdateEvent(MessengerCartridgeUiEventType.GetChatContacts);
        _fragment.OnHistoryViewPressed += from =>
            SendMessengerUpdateEvent(MessengerCartridgeUiEventType.GetChatHistory, from);
        _fragment.OnMessageSendButtonPressed += (to, text) =>
            SendMessengerUpdateEvent(
                MessengerCartridgeUiEventType.SendMessage,
                new MessengerCartridgeUiEventNewMessage(to, text));
    }

    private void SendMessengerUpdateEvent(MessengerCartridgeUiEventType type, object? args = null)
    {
        if (_userInterface == null)
            return;

        var ev = new MessengerCartridgeUiEvent(type, args);
        var message = new CartridgeUiMessage(ev);
        _userInterface.SendMessage(message);
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not MessengerCartridgeUiState messengerCartridgeUiState)
            return;

        _fragment?.UpdateState(messengerCartridgeUiState);
    }
}
