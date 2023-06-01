using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Extensions;

public static class EntityManagerExtensions
{
    public static bool TryComp<T>(
        this EntityManager entitySystem,
        List<EntityUid> uids,
        [NotNullWhen(true)] out T? component)
        where T : Component
    {
        foreach (var uid in uids)
        {
            if (entitySystem.TryGetComponent<T>(uid, out var comp))
            {
                component = comp;
                return true;
            }
        }

        component = default;
        return false;
    }

    //public static bool TryComp<T>(
    //    this EntitySystem entitySystem,
    //    List<EntityUid> uids,
    //    [NotNullWhen(true)] out T? component)
    //    where T : Component
    //{
    //    foreach (var uid in uids)
    //    {
    //        if (entitySystem.TryComp<T>(uid, out var comp))
    //        {
    //            component = comp;
    //            return true;
    //        }
    //    }
    //
    //    component = default;
    //    return false;
    //}
}
