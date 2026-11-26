// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Assets.Templates;

/// <summary>The type of template, based on its yaml tag.</summary>
public enum TemplateType
{
    /// <summary>A standard template that uses the default tag.</summary>
    Standard,
    /// <summary>A sample template that shows up in the create or open project dialogue in Game Studio.</summary>
    Sample,
    /// <summary>An asset pack template that users can select when creating a new Stride project to include additional assets.</summary>
    AssetPack
}
