using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.Extensions;
using Content.Shared.Inventory;
using Content.Shared.MessengerCartridge;
using Content.Shared.PDA;
using Serilog;

namespace Content.Server.MessengerCartridge;

/// <summary>
///     This system handles crew member messenger program.
/// </summary>
public sealed class MessageServerSystem : EntitySystem
{
    [Dependency] private readonly MessengerCartridgeSystem _messengerCartridgeSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    private ConcurrentDictionary<EntityUid, MessengerContactCacheRecord> _messengerContactCache = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PDAComponent, PdaIdCardChangedEvent>(OnPdaIdCardChanged);
    }

    /// <summary>
    ///     Update <see cref="MessengerCartridgeSystem"/> with owner data.
    /// </summary>
    private void OnPdaIdCardChanged(EntityUid pdaUid, PDAComponent pda, PdaIdCardChangedEvent args)
    {
        if (!GetMessengerComponent(pdaUid, out var messengerCartridge))
            return;

        var station = _stationSystem.GetOwningStation(pdaUid);
        if (!station.HasValue)
            return;

        string? fullName = null;
        if (pda.IdSlot.Item != null && EntityManager.TryGetComponent<IdCardComponent>(pda.IdSlot.Item, out var idCardComponent))
            fullName = idCardComponent.FullName;

        var oldIdCard = messengerCartridge.CurrentOwner?.IdCardUid ?? pda.IdSlot.Item ?? EntityUid.Invalid;

        _messengerCartridgeSystem.UpdateContactInfo(
            station.Value,
            messengerCartridge,
            args.IsIdCardInserted,
            pda.IdSlot.Item,
            fullName);

        UpdateCache(oldIdCard, fullName, station.Value, args.IsIdCardInserted);

        if (messengerCartridge.CurrentOwner != null)
            Log.Debug("Contact is '{Name}'", messengerCartridge.CurrentOwner?.FullName);
        Logger.Debug("IdCard({IdCard}) changed in PDA({PdaUid})", pda.IdSlot.Item, pdaUid);
    }

    /// <summary>
    ///     Update MessengerContact cache.
    /// </summary>
    private void UpdateCache(EntityUid idUid, string? fullName, EntityUid stationUid, bool isOnline)
    {
        if (idUid == EntityUid.Invalid)
            return;

        if (_messengerContactCache.ContainsKey(idUid))
        {
            _messengerContactCache[idUid].Station = stationUid;
            _messengerContactCache[idUid].IsOnline = isOnline;
        }
        else
        {
            _messengerContactCache.TryAdd(
                idUid,
                new MessengerContactCacheRecord(
                    new MessengerContact(idUid, fullName ?? ""))
                    {
                        Station = stationUid,
                        IsOnline = isOnline
                    });
        }
    }

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

        if (!EntityManager.TryGetComponent<CartridgeLoaderComponent>(pdaUid, out var cartridgeLoader))
            return false;

        return EntityManager.TryComp(cartridgeLoader.InstalledPrograms, out messenger);
    }

    /// <summary>
    ///     Get all available chats for <see cref="MessengerCartridgeComponent"/>.
    /// </summary>
    /// <param name="component"><see cref="MessengerCartridgeComponent"/></param>
    public MessengerContact[] GetChats(MessengerCartridgeComponent component)
    {
        var station = _stationSystem.GetOwningStation(component.StationUid);

        if (station == null
            || !TryComp<StationMessageHistoryComponent>(station, out var stationMessageHistory)
            || component.CurrentOwner == null)
            return Array.Empty<MessengerContact>();

        var oldContacts =
            stationMessageHistory.History.GetExistingChats(component.StationUid, component.CurrentOwner.IdCardUid)
                .Select(m => new MessengerContact(m.idCardUid, m.fullName));

        var activeContacts = _messengerContactCache
            .Values
            .Where(m => m.Contact.IdCardUid != component.CurrentOwner.IdCardUid
                        && m.Station == component.StationUid
                        && m.IsOnline)
            .Select(m => m.Contact)
            .ToList();

        return activeContacts.Concat(oldContacts).DistinctBy(m => m.IdCardUid).ToArray();
    }

    /// <summary>
    ///     Record for messenger contact cache.
    /// </summary>
    private class MessengerContactCacheRecord
    {
        public MessengerContact Contact { get; }
        public EntityUid Station { get; set; }
        public bool IsOnline { get; set; }

        public MessengerContactCacheRecord(MessengerContact contact)
        {
            Contact = contact;
        }
    }
}
