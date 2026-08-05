// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;

namespace Stride.Assets.Templates;

/// <summary>UI hook for collecting dotnet new template parameter values; null result = cancel.</summary>
public interface IDotNetNewParameterPrompt
{
    Task<DotNetNewPromptResult?> PromptAsync(ITemplateInfo template, TemplateDotNetNewDescription description);
}

/// <summary>
/// What the parameter dialog collected: template parameter values, plus the identities of the
/// asset-pack item templates the user selected (empty when the template offers none or the user
/// selected none).
/// </summary>
public sealed record DotNetNewPromptResult(
    IReadOnlyDictionary<string, string> Parameters,
    IReadOnlyList<string> AssetPackIdentities);
