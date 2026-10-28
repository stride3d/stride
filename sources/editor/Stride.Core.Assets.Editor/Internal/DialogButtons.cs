// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Windows;

namespace Stride.Core.Assets.Editor.Internal;

/// <summary>
/// Builds the buttons of a message box from their captions, as the WPF <c>DialogHelper.CreateButtons</c> does.
/// </summary>
internal static class DialogButtons
{
    /// <summary>
    /// Creates one button per caption. The result of a button is its 1-based index.
    /// </summary>
    /// <param name="captions">The captions of the buttons.</param>
    /// <param name="defaultIndex">The 1-based index of the default button, if any.</param>
    /// <param name="cancelIndex">The 1-based index of the cancel button, if any.</param>
    public static IReadOnlyList<DialogButtonInfo> Create(IEnumerable<string> captions, int? defaultIndex = null, int? cancelIndex = null)
    {
        var i = 1;
        return captions.Select(caption => new DialogButtonInfo
        {
            Content = caption,
            Result = i,
            IsDefault = defaultIndex == i,
            IsCancel = cancelIndex == i++,
        }).ToList();
    }
}
