using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.LieDown;

/// <summary>
///     Makes the target to lie down.
/// </summary>
[Access(typeof(SharedLieDownSystem))]
[NetworkedComponent, RegisterComponent]
public sealed class LyingDownComponent : Component
{
    /// <summary>
    ///     The action to lie down or stand up.
    /// </summary>
    [DataField("stand-up-action", required: true)]
    public InstantAction StandUpAction { get; set; } = new();

    /// <summary>
    ///     The action to lie down or stand up.
    /// </summary>
    ///
    [DataField("lie-down-action", required: true)]
    public InstantAction LieDownAction { get; set; } = new();

    /// <summary>
    ///     The action to lie down or stand up.
    /// </summary>
    [DataField("make-to-stand-up-action", customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>))]
    public string? MakeToStandUpAction = "action-name-make-standup";

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("movementSpeedDebuff")]
    public float MovementSpeedDebuff { get; set; } = 1f;
}

public sealed class LieDownActionEvent : InstantActionEvent {}
public sealed class StandUpActionEvent : InstantActionEvent {}

[Serializable, NetSerializable]
public sealed class ChangeStandingStateEvent : EntityEventArgs {}
