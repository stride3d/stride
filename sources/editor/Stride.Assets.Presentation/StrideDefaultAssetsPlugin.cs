// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Reflection;
using Stride.Core.Serialization;
using Stride.Engine;

namespace Stride.Assets.Presentation;

/// <summary>
/// The assets plugin of the Stride engine assets: asset view models, primitive types and the entity component metadata that the property grids rely on.
/// </summary>
/// <remarks>
/// The editor-level registrations (node presenter commands and updaters, previews, gizmos, entity factories...) are in <c>Stride.Assets.Editor</c>.
/// </remarks>
public sealed class StrideDefaultAssetsPlugin : AssetsPlugin
{
    /// <summary>
    /// Comparer for component types.
    /// </summary>
    private sealed class ComponentTypeComparer : EqualityComparer<Type>
    {
        public static new readonly ComponentTypeComparer Default = new();

        /// <summary>
        /// Compares two component types and returns <c>true</c> if the types match, i.e.:
        /// <list type="bullet">
        /// <item>both types are identical</item>
        /// <item>first type is a subclass of the second type (e.g. StartupScript is a subclass of ScriptComponent)</item>
        /// </list>
        /// </summary>
        public override bool Equals(Type? x, Type? y)
        {
            return ReferenceEquals(x, y) || (x is not null && y is not null && x.IsSubclassOf(y));
        }

        public override int GetHashCode(Type obj)
        {
            return 1; // must all match the same hash so that Equals is called
        }
    }

    private static bool initialized;

    public static IReadOnlyList<(Type type, int order)> ComponentOrders { get; private set; } = [];

    /// <inheritdoc />
    public override void InitializePlugin(ILogger logger)
    {
        if (initialized)
            return;
        initialized = true;

        AssemblyRegistry.AssemblyRegistered += (_, e) =>
        {
            SetTypeExpandRuleFallback(e.Assembly.GetTypes());
        };

        SetTypeExpandRuleFallback(typeof(EntityComponent).GetInheritedInstantiableTypes().ToArray());
        RegisterComponentOrders(logger);
    }

    /// <inheritdoc />
    public override void RegisterPrimitiveTypes(ICollection<Type> primitiveTypes)
    {
        primitiveTypes.Add(typeof(AssetReference));
        primitiveTypes.Add(typeof(UrlReferenceBase));
    }

    /// <summary>
    /// Get the component type which has the highest order (according to <see cref="ComponentOrderAttribute"/>)
    /// or <see langword="null"/> if none of the given <paramref name="componentTypes"/> were registered.
    /// </summary>
    /// <remarks>If two components (or more) share the same order, the last registered will be returned.</remarks>
    public static Type? GetHighestOrderComponent(IEnumerable<Type> componentTypes)
    {
        return GetComponentsByOrder(componentTypes, false).FirstOrDefault();
    }

    /// <summary>
    /// Get the component type which has the lowest order (according to <see cref="ComponentOrderAttribute"/>)
    /// or <see langword="null"/> if none of the given <paramref name="componentTypes"/> were registered.
    /// </summary>
    /// <remarks>If two components (or more) share the same order, the last registered will be returned.</remarks>
    public static Type? GetLowestOrderComponent(IEnumerable<Type> componentTypes)
    {
        return GetComponentsByOrder(componentTypes, true).FirstOrDefault();
    }

    /// <summary>
    /// Returns an enumeration of component types ordered according to their <see cref="DisplayAttribute.Order"/>.
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
    /// Update display name of scripts to have a decent default value if user didn't set one.
    /// </summary>
    private static void SetTypeExpandRuleFallback(Type[] types)
    {
        foreach (var type in types)
        {
            if (type.IsAssignableTo(typeof(EntityComponent)) && TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<DisplayAttribute>(type, false) == null)
            {
                TypeDescriptorFactory.Default.AttributeRegistry.Register(type, new DisplayAttribute(type.Name) { Expand = ExpandRule.Once });
            }
        }
    }

    private static void RegisterComponentOrders(ILogger logger)
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
}
