using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges.Messenger;

public sealed class MessengerUi : UIFragment
{
    public const int ChatsList = 0;
    public const int ChatHistory = 1;

    private int _currentView;

    private MessengerUiFragment? _fragment;
    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _currentView = ChatsList;

        _fragment = new MessengerUiFragment();
        _fragment.OnHistoryViewPressed += from =>
        {
            var message = new MessengerOpenHistoryUiMessageEvent(from);
            var ms = new CartridgeUiMessage(message);
            userInterface.SendMessage(ms);

            _currentView = ChatHistory;
        };

        _fragment.OnMessageSendButtonPressed += (to, text) =>
        {
            var message = new MessengerSendUiMessageEvent(to, text);
            var ms = new CartridgeUiMessage(message);
            userInterface.SendMessage(ms);
        };

        _fragment.OnBackButtonPressed += _ =>
        {
            if (_currentView != ChatsList)
            {
                _currentView--;
            }

            switch (_currentView)
            {
                case ChatsList:
                    var message = new MessengerChatsListUiMessageEvent();
                    var ms = new CartridgeUiMessage(message);
                    userInterface.SendMessage(ms);
                break;
            }
        };


    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        switch (state)
        {
            case MessengerMainMenuUiState messengerMainMenuUiState:
                if (_currentView == ChatsList)
                    _fragment?.UpdateChatsState(messengerMainMenuUiState.LastMessage);
                break;
            case MessengerHistoryUiState historyUiState:
                if (_currentView == ChatHistory)
                    _fragment?.UpdateChatHistoryState(historyUiState.Receiver, historyUiState.History);
                break;
        }
    }
}
