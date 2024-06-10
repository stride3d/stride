// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Annotations;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;

#nullable enable

namespace Stride.Core.Assets.Editor.Annotations;

public abstract class AssetEditorViewAttribute : Attribute
{
    public abstract Type EditorViewModelType { get; }
}

/// <summary>
/// This attribute is used to register an editor view class and associate it to an editor view model type.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
[BaseTypeRequired(typeof(IEditorView))]
public sealed class AssetEditorViewAttribute<T> : AssetEditorViewAttribute
    where T : AssetEditorViewModel
{
    /// <summary>
    /// The editor view model type described by this attribute.
    /// </summary>
    public override Type EditorViewModelType => typeof(T);
}
