// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Editor.Annotations;
using Stride.Editor.Avalonia.Preview.Views;

namespace Stride.Audio.Editor;

/// <summary>
/// The view of a sound preview: the play and pause buttons, the volume and the position of the sound (Themes/Generic.axaml).
/// </summary>
[AssetPreviewView<SoundPreview>]
public class SoundPreviewView : StridePreviewView
{
    /// <inheritdoc />
    protected override Type StyleKeyOverride => typeof(SoundPreviewView);
}
