// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Editor.Services;

public interface IDebugService
{
    Task<bool> StartDebugAsync(ISessionViewModel session, ProjectViewModel project, LoggerResult logger);
}
