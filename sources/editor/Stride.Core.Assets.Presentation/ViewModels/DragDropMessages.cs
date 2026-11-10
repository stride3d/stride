// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using Stride.Core.Translation;

namespace Stride.Core.Assets.Presentation.ViewModels;

/// <summary>
/// The messages that the view models give back to the drag and drop of the views (<see cref="Stride.Core.Assets.Presentation.ViewModels.IAddChildViewModel"/>).
/// </summary>
public static class DragDropMessages
{
    /// <summary>
    /// Can't drop here.
    /// </summary>
    public static readonly string InvalidDropArea = Tr._p("Message", "Can't drop here");
}
