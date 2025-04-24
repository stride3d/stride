// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Presentation.Avalonia.Views;

/// <summary>
/// A default implementation of the <see cref="ITemplateProvider"/> interface that matches any object.
/// </summary>
public sealed class DefaultTemplateProvider : TemplateProviderBase
{
    /// <inheritdoc/>
    public override string Name => "Default";

    /// <inheritdoc/>
    public override bool Match(object? obj)
    {
        return true;
    }
}
