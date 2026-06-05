// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;

namespace Stride.Assets.Templates;

/// <summary>UI hook for collecting dotnet new template parameter values; null result = cancel.</summary>
public interface IDotNetNewParameterPrompt
{
    Task<IReadOnlyDictionary<string, string>?> PromptAsync(ITemplateInfo template);
}
