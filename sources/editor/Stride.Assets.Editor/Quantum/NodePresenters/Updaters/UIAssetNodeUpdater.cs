// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Core.Quantum;
using Stride.Core.Reflection;
using Stride.Core;
using Stride.Core.Assets.Editor.Extensions;
using Stride.Core.Assets.Presentation.Quantum.NodePresenters;
using Stride.Core.Extensions;
using Stride.UI;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Updaters;

internal sealed class UIAssetNodeUpdater : AssetNodePresenterUpdaterBase
{
    private const int AttachedPropertyOrder = 10000;

    /// <inheritdoc/>
    protected override void UpdateNode(IAssetNodePresenter node)
    {
        if (node.Type == typeof(Thickness))
        {
            foreach (var child in node.Children)
            {
                child.IsVisible = false;
            }
        }
        if (node.Type == typeof(StripDefinition))
        {
            node[nameof(StripDefinition.Type)].IsVisible = false;
            node[nameof(StripDefinition.SizeValue)].IsVisible = false;
        }
        if (node.Asset is UIBaseViewModel)
        {
            if (node.Type.HasInterface(typeof(UIElement)) || node.Type.HasInterface(typeof(IEnumerable<UIElement>)))
            {
                // Hide UIElement properties
                node.IsVisible = false;
                if (!node.IsObjectReference(node.Value))
                {
                    UpdateDependencyProperties(node);
                }
            }
        }
    }

    private void UpdateDependencyProperties(IAssetNodePresenter node)
    {
        var element = node.Value as UIElement;
        var parent = element?.VisualParent;
        if (parent == null)
            return;

        // Create a virtual node for each attached dependency property declared by the parent
        var startOrder = AttachedPropertyOrder;
        foreach (var property in GetDeclaredProperties(parent).Where(p => p.Metadatas.Any(m => (m as DependencyPropertyKeyMetadata)?.Flags.HasFlag(DependencyPropertyFlags.Attached) ?? false)))
        {
            GetDisplayData(property, out var displayName, out var categoryName, out var order);
            var propertyNodeparent = node.GetCategory(categoryName) ?? node;
            var propNode = CreateDependencyPropertyNode(propertyNodeparent, node, property, order ?? startOrder++);
            propNode.DisplayName = parent.GetType().Name + "." + displayName;
            propNode.ChangeParent(node.GetCategory(categoryName) ?? node);
        }
    }

    /// <summary>
    /// Gets an enumeration of the dependency properties that are declared by this <paramref name="element"/>.
    /// </summary>
    /// <param name="element"></param>
    /// <returns></returns>
    private static IEnumerable<PropertyKey> GetDeclaredProperties(UIElement element)
    {
        var list = new List<PropertyKey>();
        var type = element.GetType();
        while (type != null)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static).Where(fi => typeof(PropertyKey).IsAssignableFrom(fi.FieldType));
            list.InsertRange(0, fields.Select(fi => fi.GetValue(null) as PropertyKey).NotNull());
            type = type.BaseType;
        }
        return list;
    }

    /// <summary>
    /// Gets the display and category (if any) names of the property.
    /// </summary>
    /// <param name="property"></param>
    /// <param name="displayName"></param>
    /// <param name="categoryName"></param>
    /// <param name="order"></param>
    /// <seealso cref="PropertyKey.Name"/>
    /// <seealso cref="DisplayAttribute.Name"/>
    /// <seealso cref="DisplayAttribute.Category"/>
    private static void GetDisplayData(PropertyKey property, out string? displayName, out string? categoryName, out int? order)
    {
        displayName = null;
        categoryName = null;
        order = null;

        // Try to get the display and category names from the DisplayAttribute
        var ownerType = property.OwnerType;
        var fieldInfo = ownerType.GetField(property.Name, BindingFlags.Public | BindingFlags.Static);
        if (fieldInfo != null)
        {
            var display = TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<DisplayAttribute>(fieldInfo);
            if (!string.IsNullOrEmpty(display?.Name))
            {
                displayName = display.Name;
            }
            if (!string.IsNullOrEmpty(display?.Category))
            {
                categoryName = display.Category;
            }
            order = display?.Order;
        }

        if (string.IsNullOrEmpty(displayName))
        {
            // Otherwise, remove the suffix from the property name
            displayName = property.Name.Replace("PropertyKey", "");
        }
    }

    private static AssetVirtualNodePresenter CreateDependencyPropertyNode(IAssetNodePresenter propertyNodeParent, IAssetNodePresenter node, PropertyKey property, int? order)
    {
        var propertyType = property.PropertyType;
        var propertyIndex = new NodeIndex(property);
        var accessor = node.GetNodeAccessor();

        var propertyContainerNode = ((IObjectNode)accessor.Node)[nameof(UIElement.DependencyProperties)].Target;

        var undoRedoService = propertyNodeParent.Asset.ServiceProvider.Get<IUndoRedoService>();
        var virtualNode = node.Factory.CreateVirtualNodePresenter(propertyNodeParent, property.Name, propertyType, order,
            () => Getter(propertyContainerNode, propertyIndex),
            o => Setter(undoRedoService, propertyContainerNode, propertyIndex, o));

        return virtualNode;
    }

    /// <summary>
    /// Getter for the virtual node's value.
    /// </summary>
    /// <param name="propertyContainerNode">The node containing the property.</param>
    /// <param name="propertyIndex">The index of the property in the node.</param>
    /// <returns></returns>
    private static object Getter(IObjectNode propertyContainerNode, NodeIndex propertyIndex)
    {
        return propertyContainerNode.Retrieve(propertyIndex);
    }

    /// <summary>
    /// Setter for the virtual node's value.
    /// </summary>
    /// <param name="undoRedoService"></param>
    /// <param name="propertyContainerNode">The node containing the property.</param>
    /// <param name="propertyIndex">The index of the property in the node.</param>
    /// <param name="value">The value to set.</param>
    private static void Setter(IUndoRedoService undoRedoService, IObjectNode propertyContainerNode, NodeIndex propertyIndex, object value)
    {
        using (undoRedoService?.CreateTransaction())
        {
            if (!propertyContainerNode.Indices.Contains(propertyIndex))
            {
                // Note: update would probably work, but we want to remove the property when Undo
                propertyContainerNode.Add(value, propertyIndex);
            }
            else
            {
                propertyContainerNode.Update(value, propertyIndex);
            }
        }
    }
}
