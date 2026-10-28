// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core.Assets.Templates;
using Stride.Core.IO;

namespace Stride.Core.Assets.Editor.Tests.Helpers;

/// <summary>
/// A package of template descriptions registered with the <see cref="TemplateManager"/> once for the test process: a game
/// template in a group, a code library and a plugin (the ids the new project window lists), and an asset template.
/// </summary>
/// <remarks>
/// The package has no generator: its templates are listed by the template windows, never run. The template manager has no way
/// to unregister a package, so the names are specific to the tests and the tests assert on their presence, not on the whole list.
/// </remarks>
public static class TestTemplatePackage
{
    public static readonly Guid GameId = new("5E1D3F6C-2B7A-4C9E-8D1F-3A5B7C9D1E2F");

    /// <summary>
    /// The id of the code library template (samples/Library/Library/Library.sdtpl), which only adds a project to a session.
    /// </summary>
    public static readonly Guid LibraryId = new("7B79F1B7-3A55-4C84-AED9-3F4F3EE4B6E5");

    /// <summary>
    /// The id of the plugin template (samples/Plugin/Plugin/Plugin.sdtpl).
    /// </summary>
    public static readonly Guid PluginId = new("2D5E7F1A-8C3B-4E6A-9F0D-1B2C3D4E5F60");

    public static readonly Guid AssetId = new("9A8B7C6D-5E4F-4A3B-9C2D-1E0F9A8B7C6D");

    public const string GameName = "Test game";

    public const string GameDefaultOutputName = "MyTestGame";

    public const string LibraryName = "Test code library";

    public const string PluginName = "Test plugin";

    public const string AssetTemplateName = "Test asset template";

    private static readonly Lazy<Package> LazyPackage = new(Register);

    /// <summary>
    /// The package, registered with the template manager.
    /// </summary>
    public static Package Package => LazyPackage.Value;

    public static TemplateDescription Game => Template(GameId);

    public static TemplateDescription Library => Template(LibraryId);

    public static TemplateDescription Plugin => Template(PluginId);

    private static TemplateDescription Template(Guid id) => Package.Templates.Single(x => x.Id == id);

    private static Package Register()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Stride.Core.Assets.Editor.Tests", "Templates", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var package = new Package
        {
            FullPath = new UFile(Path.Combine(directory, "TestTemplates" + Package.PackageFileExtension)),
            Meta = { Name = "TestTemplates", Version = new PackageVersion("1.0.0") },
        };
        package.Templates.Add(new TemplateDescription
        {
            Id = GameId,
            Name = GameName,
            Scope = TemplateScope.Session,
            Group = "Test games/Starters",
            Order = 10,
            DefaultOutputName = GameDefaultOutputName,
            Description = "A game for the tests",
            FullPath = new UFile(Path.Combine(directory, "TestGame" + TemplateDescription.FileExtension)),
        });
        package.Templates.Add(new TemplateDescription
        {
            Id = LibraryId,
            Name = LibraryName,
            Scope = TemplateScope.Session,
            Order = 20,
            DefaultOutputName = "MyTestLibrary",
            Description = "A code library for the tests",
            FullPath = new UFile(Path.Combine(directory, "TestLibrary" + TemplateDescription.FileExtension)),
        });
        package.Templates.Add(new TemplateDescription
        {
            Id = PluginId,
            Name = PluginName,
            Scope = TemplateScope.Session,
            Order = 30,
            DefaultOutputName = "MyTestPlugin",
            Description = "A plugin for the tests",
            FullPath = new UFile(Path.Combine(directory, "TestPlugin" + TemplateDescription.FileExtension)),
        });
        package.Templates.Add(new TemplateDescription
        {
            Id = AssetId,
            Name = AssetTemplateName,
            Scope = TemplateScope.Asset,
            DefaultOutputName = "MyTestAsset",
            FullPath = new UFile(Path.Combine(directory, "TestAsset" + TemplateDescription.FileExtension)),
        });
        TemplateManager.RegisterPackage(package);
        return package;
    }
}
