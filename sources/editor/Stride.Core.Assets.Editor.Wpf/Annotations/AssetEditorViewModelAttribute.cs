// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Annotations;
using Stride.Core.Assets.Editor.ViewModel;

#nullable enable

namespace Stride.Core.Assets.Editor.Annotations;

public abstract class AssetEditorViewModelAttribute : Attribute
{
    public abstract Type ViewModelType { get; }
}

/// <summary>
/// This attribute is used to register an editor view model class and associate it to an asset view model type.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
[BaseTypeRequired(typeof(AssetEditorViewModel))]
public sealed class AssetEditorViewModelAttribute<T> : AssetEditorViewModelAttribute
    where T : AssetViewModel
{
    /// <summary>
    /// The asset view model type described by this attribute.
    /// </summary>
    public override Type ViewModelType => typeof(T);
}
