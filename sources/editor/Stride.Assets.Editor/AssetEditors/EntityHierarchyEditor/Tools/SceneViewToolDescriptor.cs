// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Assets.Editor.Services;

namespace Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Tools;

/// <summary>
/// A tool of the views of the scene and prefab editors, as a plugin registers it (<see cref="ScenePluginContextExtensions.SceneViewTools"/>)
/// or declares it (<see cref="EditorViewToolAttribute"/>): each open view creates one instance of the type, and the views
/// add and remove it as the plugin joins and leaves.
/// </summary>
public sealed class SceneViewToolDescriptor
{
    /// <summary>
    /// The order of the tools of the plugins: after the tools of the editor.
    /// </summary>
    public const int DefaultOrder = EditorViewToolAttribute.DefaultOrder;

    /// <summary>
    /// Gets the type of the tool: a class that implements <see cref="IEditorViewTool"/>, with a public parameterless
    /// constructor.
    /// </summary>
    public required Type ToolType { get; init; }

    /// <summary>
    /// Gets the place of the tool in the toolbar (ties by type name).
    /// </summary>
    public int Order { get; init; } = DefaultOrder;

    /// <summary>
    /// Describes a tool type, with the order its <see cref="EditorViewToolAttribute"/> gives, if any.
    /// </summary>
    public static SceneViewToolDescriptor FromType(Type toolType)
    {
        ArgumentNullException.ThrowIfNull(toolType);
        return new SceneViewToolDescriptor
        {
            ToolType = toolType,
            Order = toolType.GetCustomAttribute<EditorViewToolAttribute>()?.Order ?? DefaultOrder,
        };
    }

    /// <summary>
    /// Describes the tool <typeparamref name="TTool"/>.
    /// </summary>
    /// <param name="order">The place of the tool in the toolbar.</param>
    public static SceneViewToolDescriptor For<TTool>(int order = DefaultOrder)
        where TTool : class, IEditorViewTool, new()
    {
        return new SceneViewToolDescriptor { ToolType = typeof(TTool), Order = order };
    }

    /// <summary>
    /// Why a tool cannot be used by the views, or null when it can.
    /// </summary>
    public static string? GetRefusal(SceneViewToolDescriptor? descriptor)
    {
        var type = descriptor?.ToolType;
        if (type is null)
            return "it has no tool type";
        if (!type.IsAssignableTo(typeof(IEditorViewTool)))
            return $"it is not an {nameof(IEditorViewTool)}";
        if (!type.IsClass || type.IsAbstract || type.IsGenericTypeDefinition || type.GetConstructor(Type.EmptyTypes) is null)
            return "it must be a class with a public parameterless constructor, since each view creates its own instance";
        return null;
    }

    /// <summary>
    /// The message of a refused tool.
    /// </summary>
    public static string DescribeRefusal(SceneViewToolDescriptor? descriptor, AssetsPlugin? plugin, string refusal)
    {
        var tool = descriptor?.ToolType is { } type ? $"[{type.FullName}] ({type.Assembly.GetName().Name})" : "[null]";
        var owner = plugin is not null ? $" of the plugin [{plugin.GetType().FullName}]" : string.Empty;
        return $"The scene view tool {tool}{owner} is left out: {refusal}.";
    }

    /// <summary>
    /// Refuses a tool that the views cannot use, as the registry of the editor does (<see cref="ContributionRegistry{T}"/>).
    /// </summary>
    /// <exception cref="ArgumentException">The tool cannot be used by the views.</exception>
    public static void Validate(SceneViewToolDescriptor descriptor)
    {
        if (GetRefusal(descriptor) is { } refusal)
            throw new ArgumentException(DescribeRefusal(descriptor, null, refusal), nameof(descriptor));
    }

    /// <inheritdoc />
    public override string ToString() => $"{ToolType?.FullName} (order {Order})";
}
