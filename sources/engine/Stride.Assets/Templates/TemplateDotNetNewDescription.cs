// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Templates;

namespace Stride.Assets.Templates;

/// <summary>
/// <see cref="TemplateDescription"/> backed by a Microsoft.TemplateEngine dotnet new template.
/// Constructed in-process from <c>template.json</c> metadata; never written to disk (no
/// <c>.sdtpl</c> file ever exists for these). The associated <see
/// cref="DotNetNewTemplateGenerator"/> consumes the <see cref="TemplateIdentity"/> to dispatch
/// instantiation through the shared <see cref="DotNetNewTemplateRegistry"/>.
/// </summary>
public class TemplateDotNetNewDescription : TemplateDescription
{
    /// <summary>
    /// The dotnet new template identity (the <c>.sdtpl</c> Id GUID emitted by the
    /// preprocessor). Stable across editor sessions / template package reinstalls; used to
    /// resolve back to the <c>ITemplateInfo</c> at instantiation time.
    /// </summary>
    public string TemplateIdentity { get; set; } = string.Empty;

    /// <summary>
    /// The dotnet new short name (e.g. <c>stride-game</c>, <c>stride-fps</c>). Used by tests
    /// and tools that need to look up a template by a stable human-readable name.
    /// </summary>
    public string TemplateShortName { get; set; } = string.Empty;
}
