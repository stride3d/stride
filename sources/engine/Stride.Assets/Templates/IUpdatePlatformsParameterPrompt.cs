// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core;

namespace Stride.Assets.Templates;

/// <summary>UI hook for Update Platforms; null result = cancel.</summary>
public interface IUpdatePlatformsParameterPrompt
{
    Task<UpdatePlatformsResult?> PromptAsync(ISet<PlatformType> installed);
}

public sealed record UpdatePlatformsResult(IReadOnlyList<PlatformType> SelectedPlatforms, bool ForceRegeneration);
