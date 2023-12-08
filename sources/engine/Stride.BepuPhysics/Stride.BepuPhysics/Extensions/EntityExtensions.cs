using System.Collections.Generic;
using System.Linq;
using Stride.Engine;

namespace Stride.BepuPhysics.Extensions
{
    /// <summary>
    /// Provides extension methods for working with Entity hierarchies and components.
    /// </summary>
    public static class EntityExtensions
    {
        /// <summary>
        /// Returns all the descendants of the current Entity using a stack and yield return.
        /// </summary>
        /// <param name="entity">The current Entity.</param>
        /// <param name="includeMyself">Include the current Entity in the result.</param>
        /// <returns>An IEnumerable of descendant Entities.</returns>
        public static IEnumerable<Entity> GetDescendants(this Entity entity, bool includeMyself = false)
        {
            if (entity != null)
            {
                if (includeMyself)
                    yield return entity;

                var stack = new Stack<Entity>();
                foreach (var child in entity.GetChildren())
                {
                    stack.Push(child);
                    while (stack.Count > 0)
                    {
                        var descendant = stack.Pop();
                        yield return descendant;
                        foreach (var x in descendant.GetChildren()) stack.Push(x);
                    }
                }
            }
        }

        /// <summary>
        /// Returns all the parents of the current Entity using yield return.
        /// </summary>
        /// <param name="entity">The current Entity.</param>
        /// <param name="includeMyself">Include the current Entity in the result.</param>
        /// <returns>An IEnumerable of parent Entities.</returns>
        public static IEnumerable<Entity> GetParents(this Entity entity, bool includeMyself = false)
        {
            if (entity != null)
            {

                if (includeMyself)
                    yield return entity;

                var parent = entity.GetParent();
                while (parent != null)
                {
                    yield return parent;
                    parent = parent.GetParent();
                }
            }
        }

        /// <summary>
        /// Returns all the components of type 'T' in the descendants of the entity.
        /// </summary>
        /// <typeparam name="T">The type of EntityComponent to search for.</typeparam>
        /// <param name="entity">The current Entity.</param>
        /// <param name="includeMyself">Include the current Entity in the search.</param>
        /// <returns>An IEnumerable of components of type 'T' in the descendants of the entity.</returns>
        public static IEnumerable<T> GetComponentsInDescendants<T>(this Entity entity, bool includeMyself = false) where T : EntityComponent
        {
            if (entity != null)
                foreach (var descendant in entity.GetDescendants(includeMyself))
                {
                    if (descendant.Get<T>() is T component)
                        yield return component;
                }
        }

        /// <summary>
        /// Returns all the components of type 'T' in the parents of the entity.
        /// </summary>
        /// <typeparam name="T">The type of EntityComponent to search for.</typeparam>
        /// <param name="entity">The current Entity.</param>
        /// <param name="includeMyself">Include the current Entity in the search.</param>
        /// <returns>An IEnumerable of components of type 'T' in the parents of the entity.</returns>
        public static IEnumerable<T> GetComponentsInParents<T>(this Entity entity, bool includeMyself = false) where T : EntityComponent
        {
            if (entity != null)
                foreach (var parent in entity.GetParents(includeMyself))
                {
                    if (parent.Get<T>() is T component)
                        yield return component;
                }
        }

        /// <summary>
        /// Returns the first component of type 'T' found in any entity in the scene.
        /// </summary>
        /// <typeparam name="T">The type of EntityComponent to search for.</typeparam>
        /// <param name="entity">The current Entity.</param>
        /// <returns>The first component of type 'T' found in the scene, or null if none is found.</returns>
        public static T? GetFirstComponentInScene<T>(this Entity entity) where T : EntityComponent
        {
            if (entity != null)
                foreach (var childEntity in entity.Scene.Entities)
                {
                    var components = childEntity.GetComponentsInDescendants<T>(true);
                    if (components.FirstOrDefault() is T component)
                        return component;
                }
            return null;
        }

        /// <summary>
        /// Calls GetFirstComponentInScene, providing a Unity-like "wrapper."
        /// </summary>
        /// <typeparam name="T">The type of EntityComponent to search for.</typeparam>
        /// <param name="entity">The current Entity.</param>
        /// <returns>The first component of type 'T' found in the scene, or null if none is found.</returns>
        public static T? FindFirstObjectByType<T>(this Entity entity) where T : EntityComponent
        {
            return entity.GetFirstComponentInScene<T>();
        }

        /// <summary>
        /// Note: These methods may perform deep traversal of the Entity hierarchy, which can be resource-intensive. Consider using them judiciously and avoid frequent use in performance-critical scenarios (e.g., in an Update method called every frame).
        /// </summary>
    }

}
