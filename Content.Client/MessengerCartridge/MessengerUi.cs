using Content.Client.UserInterface.Fragments;
using Content.Shared.MessengerCartridge;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.MessengerCartridge;

public sealed class MessengerUi : UIFragment
{
    private MessengerCartridgeUiFragment? _fragment;

    public const int ChatsList = 0;
    public const int ChatHistory = 1;

    private int _currentView;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new MessengerCartridgeUiFragment();

        _fragment.OnHistoryViewPressed += from =>
        {
            _currentView = ChatHistory;
        };

        _fragment.OnMessageSendButtonPressed += (to, text) =>
        {

        };

        _fragment.OnBackButtonPressed += _ =>
        {
            if (_currentView != ChatsList)
            {
                _currentView--;
            }
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not MessengerCartridgeUiState messengerCartridgeUiState)
            return;

        _fragment?.UpdateState(messengerCartridgeUiState);
    }
}
