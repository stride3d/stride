// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.Core.Reflection;
using Stride.Engine;

namespace Stride.Assets.Presentation;

/// <summary>
/// The entity component metadata both editors rely on: the order of the components (<see cref="ComponentOrderAttribute"/>),
/// for the gizmos and the icons of the entities, and a default display name for the components without one. The default
/// assets plugin of each editor registers it.
/// </summary>
public static class EntityComponentMetadata
{
    /// <summary>
    /// Gets the component types with an order, in reverse registration order (last registered first).
    /// </summary>
    public static IReadOnlyList<(Type type, int order)> ComponentOrders { get; private set; } = [];

    /// <summary>
    /// Reads the order of the components of every asset assembly.
    /// </summary>
    /// <param name="logger">The logger of the warning when two components have the same order.</param>
    public static void RegisterComponentOrders(ILogger logger)
    {
        // TODO: iterate on plugin assembly or register component type in the plugin registry
        var hashSet = new HashSet<int>();
        var componentTypes = AssetRegistry.AssetAssemblies.SelectMany(x => x.GetTypes().Where(y => typeof(EntityComponent).IsAssignableFrom(y)));
        var orders = new List<(Type, int)>();
        foreach (var type in componentTypes)
        {
            // Check with inheritance to ensure they have a reachable display attribute
            var attrib = TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<ComponentOrderAttribute>(type, false);
            if (attrib != null)
            {
                // Disable logging for an order with the same value. It's an order, not a key, so it should not log any warning messages (see this with ben)
                if (!hashSet.Add(attrib.Order))
                {
                    var other = orders.First(t => t.Item2 == attrib.Order);
                    logger.Warning($"Two entity components have explicitly the same order value ({attrib.Order}): [{other.Item1}], [{type}]");
                }
                // tuples are added in the order the component are registered
                orders.Add((type, attrib.Order));
            }
        }
        // Reverse the order so that last registered component appears first.
        orders.Reverse();
        ComponentOrders = orders;
    }

    /// <summary>
    /// Gets the component type which has the highest order (according to <see cref="ComponentOrderAttribute"/>), or
    /// <c>null</c> if none of the given <paramref name="componentTypes"/> were registered.
    /// </summary>
    /// <remarks>If two components (or more) share the same order, the last registered will be returned.</remarks>
    public static Type? GetHighestOrderComponent(IEnumerable<Type> componentTypes)
    {
        return GetComponentsByOrder(componentTypes, false).FirstOrDefault();
    }

    /// <summary>
    /// Gets the component type which has the lowest order (according to <see cref="ComponentOrderAttribute"/>), or
    /// <c>null</c> if none of the given <paramref name="componentTypes"/> were registered.
    /// </summary>
    /// <remarks>If two components (or more) share the same order, the last registered will be returned.</remarks>
    public static Type? GetLowestOrderComponent(IEnumerable<Type> componentTypes)
    {
        return GetComponentsByOrder(componentTypes, true).FirstOrDefault();
    }

    /// <summary>
    /// Returns an enumeration of component types ordered according to their <see cref="ComponentOrderAttribute"/>.
    /// </summary>
    /// <remarks>If two components (or more) share the same order, the last registered will be returned first.</remarks>
    /// <param name="componentTypes">An enumeration of component types</param>
    /// <param name="ascending">True if the order is from the lowest order to the highest, False otherwise.</param>
    public static IEnumerable<Type> GetComponentsByOrder(IEnumerable<Type> componentTypes, bool ascending)
    {
        // Note: ComponentOrders contains the component type in reverse registration order (last registered first).
        // Enumerable.OrderBy and Enumerable.OrderByDescending are stable, so order is preserved.
        var filtered = ComponentOrders.Join(componentTypes, t => t.type, c => c, (t, c) => t, ComponentTypeComparer.Default);
        return (ascending ? filtered.OrderBy(t => t.order) : filtered.OrderByDescending(t => t.order)).Select(t => t.type);
    }

    /// <summary>
    /// Gets the order of the given component, or 0 when it has none.
    /// </summary>
    public static int GetOrder(Type componentType)
    {
        foreach (var (type, order) in ComponentOrders)
        {
            if (type == componentType)
                return order;
        }
        return default;
    }

    /// <summary>
    /// Gives the entity components of the given types without a display attribute one with their type name, expanded once,
    /// so that the property grid shows a decent name for the scripts of the user.
    /// </summary>
    public static void SetDisplayFallback(IEnumerable<Type> types)
    {
        foreach (var type in types)
        {
            if (type.IsAssignableTo(typeof(EntityComponent)) && TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<DisplayAttribute>(type, false) == null)
            {
                TypeDescriptorFactory.Default.AttributeRegistry.Register(type, new DisplayAttribute(type.Name) { Expand = ExpandRule.Once });
            }
        }
    }

    /// <summary>
    /// Matches two component types when they are identical, or when the first one derives from the second one (a
    /// <c>StartupScript</c> is a <c>ScriptComponent</c>).
    /// </summary>
    private sealed class ComponentTypeComparer : EqualityComparer<Type>
    {
        public static new readonly ComponentTypeComparer Default = new();

        public override bool Equals(Type? x, Type? y)
        {
            return ReferenceEquals(x, y) || (x is not null && y is not null && x.IsSubclassOf(y));
        }

        public override int GetHashCode(Type obj)
        {
            return 1; // must all match the same hash so that Equals is called
        }
    }
}
