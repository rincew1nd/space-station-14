using System.Collections.Concurrent;
using Content.Shared.MessengerCartridge;

namespace Content.Server.MessengerCartridge;

/// <summary>
///     Record set for private messages between crew members.
/// </summary>
public sealed class StationsMessageHistory
{
    /// <summary>
    ///     Message history for each station.
    /// </summary>
    private readonly ConcurrentDictionary<EntityUid, ConcurrentDictionary<int, List<MessengerContactMessage>>> _stationMessages = new();

    /// <summary>
    ///     Existing dialogs for each station.
    /// </summary>
    private readonly ConcurrentDictionary<EntityUid, ConcurrentDictionary<EntityUid, List<(EntityUid, string)>>> _stationExistingChats = new();

    /// <summary>
    ///     Add a new station for a message history.
    /// </summary>
    /// <param name="stationUid">Station that we're adding the record for.</param>
    public bool AddStation(EntityUid stationUid)
    {
        if (TryAddEmpty(_stationMessages, stationUid))
        {
            return false; // Highly unluckily
        }
        if (TryAddEmpty(_stationExistingChats, stationUid))
        {
            return false; // Highly unluckily
        }
        return true;
    }

    /// <summary>
    ///     Add a new message record to the station message history.
    /// </summary>
    /// <param name="stationUid">Station identifier</param>
    /// <param name="message">Message</param>
    public bool AddRecord(EntityUid stationUid, MessengerContactMessage message, string from, string to)
    {
        if (!_stationMessages.ContainsKey(stationUid))
        {
            if (AddStation(stationUid))
            {
                return false;
            }
        }

        if (!TryAddEmpty(_stationMessages[stationUid], message.GetHashCode()))
        {
            return false; // Highly unluckily
        }

        if (!TryAddEmpty(_stationExistingChats[stationUid], message.From))
        {
            return false; // Highly unluckily
        }
        _stationExistingChats[stationUid][message.From].Add((message.To, to));

        if (!TryAddEmpty(_stationExistingChats[stationUid], message.To))
        {
            return false; // Highly unluckily
        }
        _stationExistingChats[stationUid][message.To].Add((message.From, from));

        _stationMessages[stationUid][message.GetHashCode()].Add(message);
        return true;
    }

    /// <summary>
    ///     Get a message history between two contacts from station.
    /// </summary>
    /// <param name="stationUid">Station identifier</param>
    /// <param name="messengerUid"><see cref="MessengerCartridgeComponent"/> uid</param>
    public IEnumerable<(EntityUid idCardUid, string fullName)> GetExistingChats(EntityUid stationUid, EntityUid messengerUid)
    {
        if (_stationExistingChats.ContainsKey(stationUid))
        {
            if (_stationExistingChats[stationUid].ContainsKey(messengerUid))
            {
                return _stationExistingChats[stationUid][messengerUid];
            }
        }
        return Array.Empty<(EntityUid, string)>();
    }

    /// <summary>
    ///     Get a message history between two contacts from station.
    /// </summary>
    /// <param name="stationUid">Station identifier</param>
    /// <param name="historyKey">History identifier</param>
    public IEnumerable<MessengerContactMessage> GetRecords(EntityUid stationUid, int historyKey)
    {
        if (_stationMessages.ContainsKey(stationUid))
        {
            if (_stationMessages[stationUid].ContainsKey(historyKey))
            {
                return _stationMessages[stationUid][historyKey];
            }
        }
        return Array.Empty<MessengerContactMessage>();
    }

    private bool TryAddEmpty<T1, T2>(ConcurrentDictionary<T1, T2> collection, T1 key)
        where T1 : notnull
    {
        var val = default(T2);
        return TryAdd(collection!, key, val);
    }

    private bool TryAdd<T1, T2>(ConcurrentDictionary<T1, T2> collection, T1 key, T2 obj) where T1 : notnull
    {
        if (obj == null)
            return false;

        if (!collection.TryAdd(key, obj))
        {
            if (!collection.ContainsKey(key))
            {
                return false; // Highly unluckily
            }
        }
        return obj.Equals(collection[key]);
    }
}
