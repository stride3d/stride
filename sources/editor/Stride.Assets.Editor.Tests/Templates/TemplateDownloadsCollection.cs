// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Templates;

namespace Stride.Assets.Editor.Tests.Templates;

/// <summary>
/// The tests that follow or start the template downloads of <see cref="TemplateDownloads"/>, which every view model of the
/// process shares: they run one after the other, so that a download one test starts (and stops before it ends) doesn't show
/// in the status of another.
/// </summary>
[CollectionDefinition(Name)]
public sealed class TemplateDownloadsCollection
{
    public const string Name = "Template downloads";
}
