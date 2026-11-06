// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Annotations;
using Stride.UI.Editor.ViewModels;

namespace Stride.UI.Editor.Views;

/// <summary>
/// The view of the UI library editor, as the WPF one: the UI editor's view, on a library and its several roots.
/// </summary>
[AssetEditorView<UILibraryEditorViewModel>]
public sealed class UILibraryEditorView : UIEditorView;
