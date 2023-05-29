using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.Verbs;
using Robust.Shared.Input.Binding;
using Robust.Shared.Players;

namespace Content.Shared.LieDown
{
    public class SharedLieDownSystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
        [Dependency] private readonly StandingStateSystem _standing = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<LyingDownComponent, ComponentStartup>(OnComponentInit);
            SubscribeLocalEvent<LyingDownComponent, StandUpActionEvent>(OnStandUpAction);
            SubscribeLocalEvent<LyingDownComponent, LieDownActionEvent>(OnLieDownAction);
            SubscribeLocalEvent<LyingDownComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<LyingDownComponent, GetVerbsEvent<AlternativeVerb>>(AddWakeVerb);
            SubscribeLocalEvent<LyingDownComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<LyingDownComponent, RefreshMovementSpeedModifiersEvent>(OnRefresh);

            SubscribeNetworkEvent<ChangeStandingStateEvent>(OnChangeAction);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.LieDownStandUp, InputCmdHandler.FromDelegate(ChangeLyingState))
                .Register<SharedLieDownSystem>();
        }

        private void ChangeLyingState(ICommonSession? session)
        {
            RaiseNetworkEvent(new ChangeStandingStateEvent());
        }

        private void OnChangeAction(ChangeStandingStateEvent msg, EntitySessionEventArgs args)
        {
            if (!args.SenderSession.AttachedEntity.HasValue)
                return;

            var uid = args.SenderSession.AttachedEntity.Value;
            var component = EnsureComp<LyingDownComponent>(uid);

            if (_standing.IsDown(uid))
            {
                TryStandUp(uid, component);
            }
            else
            {
                TryLieDown(uid, component);
            }
        }

        private void OnRefresh(EntityUid uid, LyingDownComponent component, RefreshMovementSpeedModifiersEvent args)
        {
            args.ModifySpeed(component.MovementSpeedDebuff, component.MovementSpeedDebuff);
        }

        private void OnComponentInit(EntityUid uid, LyingDownComponent component, ComponentStartup args)
        {
            _actions.AddAction(uid, component.LieDownAction, uid);
        }

        private void SwitchActions(EntityUid uid, LyingDownComponent component)
        {
            if (_standing.IsDown(uid))
            {
                _actions.AddAction(uid, component.StandUpAction, null);
                _actions.RemoveAction(uid, component.LieDownAction);
            }
            else
            {
                _actions.AddAction(uid, component.LieDownAction, uid);
                _actions.RemoveAction(uid, component.StandUpAction);
            }
        }

        private void OnStandUpAction(EntityUid uid, LyingDownComponent component, StandUpActionEvent args)
        {
            TryStandUp(uid, component);
        }
        private void OnLieDownAction(EntityUid uid, LyingDownComponent component, LieDownActionEvent args)
        {
            TryLieDown(uid, component);
        }

        /// <summary>
        ///     When interacting with a lying down person, add ability to make him stand up.
        /// </summary>
        private void OnInteractHand(EntityUid uid, LyingDownComponent component, InteractHandEvent args)
        {
            TryStandUp(args.Target, component);
        }

        private void AddWakeVerb(EntityUid uid, LyingDownComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    TryStandUp(uid, component);
                },
                Text = Loc.GetString(component.MakeToStandUpAction!),
                Priority = 2
            };

            args.Verbs.Add(verb);
        }

        /// <summary>
        ///     If somebody examined a lying down person, add description.
        /// </summary>
        private void OnExamined(EntityUid uid, LyingDownComponent component, ExaminedEvent args)
        {
            if (args.IsInDetailsRange)
            {
                args.PushMarkup(Loc.GetString("lying-down-examined", ("target", Identity.Entity(uid, EntityManager))));
            }
        }

        private void TryStandUp(EntityUid uid, LyingDownComponent component)
        {
            if (!_standing.Stand(uid))
                return;

            Logger.Debug("{uid} tried to stand up", uid);

            component.MovementSpeedDebuff = 1f;
            _movement.RefreshMovementSpeedModifiers(uid);

            SwitchActions(uid, component);
        }

        private void TryLieDown(EntityUid uid, LyingDownComponent component)
        {
            if (!_standing.Down(uid, false, false))
                return;

            Logger.Debug("{uid} tried to lie down", uid);

            component.MovementSpeedDebuff = 0.4f;
            _movement.RefreshMovementSpeedModifiers(uid);

            SwitchActions(uid, component);
        }
    }
}
