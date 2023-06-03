using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Extensions;

/// <summary>
///     Extension methods for <see cref="EntityManager"/>.
/// </summary>
public static class EntityManagerExtensions
{
    /// <summary>
    ///     Try to resolve any components of specific type from a list of <see cref="EntityUid"/>.
    /// </summary>
    /// <param name="entitySystem"><see cref="EntityManager"/></param>
    /// <param name="uids">List of components <see cref="EntityUid"/></param>
    /// <param name="component">Found component</param>
    /// <typeparam name="T">Type to search for</typeparam>
    /// <returns>Have component been found</returns>
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
}
