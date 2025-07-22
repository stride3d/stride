// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.Status;

namespace Stride.Core.Assets.Editor.ViewModels;

public interface IMainViewModel
{
    StatusViewModel Status { get; }
}
