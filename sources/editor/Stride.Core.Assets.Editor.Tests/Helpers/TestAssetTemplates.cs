// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Templates;
using Stride.Core.IO;

namespace Stride.Core.Assets.Editor.Tests.Helpers;

/// <summary>
/// The generator of the asset templates of <see cref="TestTemplatePackage"/> and the importers of their source files, registered
/// once for the test process. The generator creates a <see cref="TestEditorAsset"/> (or a <see cref="TestOtherEditorAsset"/>)
/// named after the template, or one per source file.
/// </summary>
public sealed class TestAssetTemplates : TemplateGeneratorBase<AssetTemplateGeneratorParameters>
{
    private static readonly Lazy<TestAssetTemplates> LazyInstance = new(Register);

    private TestAssetTemplates()
    {
    }

    /// <summary>
    /// The generator, registered with the template manager (and the importers with the asset registry).
    /// </summary>
    public static TestAssetTemplates Generator => LazyInstance.Value;

    /// <summary>
    /// The parameters of the last run.
    /// </summary>
    public AssetTemplateGeneratorParameters? LastParameters { get; private set; }

    /// <summary>
    /// Makes the next run log an error and create nothing.
    /// </summary>
    public bool FailNextRun { get; set; }

    /// <summary>
    /// Makes the next run stop in its preparation, as a cancelled prompt does.
    /// </summary>
    public bool CancelNextRun { get; set; }

    /// <inheritdoc/>
    public override bool IsSupportingTemplate(TemplateDescription templateDescription)
    {
        return templateDescription.Id == TestTemplatePackage.AssetFactoryId
            || templateDescription.Id == TestTemplatePackage.ImportId
            || templateDescription.Id == TestTemplatePackage.OtherImportId;
    }

    /// <inheritdoc/>
    public override Task<bool> PrepareForRun(AssetTemplateGeneratorParameters parameters)
    {
        LastParameters = parameters;
        if (CancelNextRun)
        {
            CancelNextRun = false;
            return Task.FromResult(false);
        }
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public override bool Run(AssetTemplateGeneratorParameters parameters)
    {
        if (FailNextRun)
        {
            FailNextRun = false;
            parameters.Logger.Error("The test generator failed.");
            return false;
        }

        var description = (TemplateAssetDescription)parameters.Description;
        if (description.ImportSource)
        {
            foreach (var file in parameters.SourceFiles)
            {
                var location = UFile.Combine(parameters.TargetLocation, file.GetFileNameWithoutExtension()!);
                parameters.Package.Assets.Add(new AssetItem(location, CreateAsset(description, file.FullPath)));
            }
        }
        else
        {
            var location = UFile.Combine(parameters.TargetLocation, parameters.Name);
            parameters.Package.Assets.Add(new AssetItem(location, CreateAsset(description, "generated")));
        }
        return true;
    }

    private static Asset CreateAsset(TemplateAssetDescription description, string value)
    {
        return description.GetAssetType() == typeof(TestOtherEditorAsset)
            ? new TestOtherEditorAsset { Value = value }
            : new TestEditorAsset { Value = value };
    }

    private static TestAssetTemplates Register()
    {
        _ = TestTemplatePackage.Package;
        AssetRegistry.RegisterImporter(new TestAssetImporter(new Guid("5E6F7A8B-9CAD-4EBF-A0B1-C2D3E4F5A6B7"), typeof(TestEditorAsset), TestTemplatePackage.ImportExtension));
        AssetRegistry.RegisterImporter(new TestAssetImporter(new Guid("6F7A8B9C-ADBE-4FC0-B1C2-D3E4F5A6B7C8"), typeof(TestOtherEditorAsset), TestTemplatePackage.ImportExtension, TestTemplatePackage.OtherImportExtension));
        var generator = new TestAssetTemplates();
        TemplateManager.Register(generator);
        return generator;
    }

    /// <summary>
    /// An importer of the test files, which only tells the extensions a test asset type is imported from.
    /// </summary>
    private sealed class TestAssetImporter : IAssetImporter
    {
        private readonly Type assetType;
        private readonly string[] extensions;

        public TestAssetImporter(Guid id, Type assetType, params string[] extensions)
        {
            Id = id;
            this.assetType = assetType;
            this.extensions = extensions;
        }

        public Guid Id { get; }

        public string Name => "Test importer of " + assetType.Name;

        public string Description => Name;

        public int Order => 0;

        public string SupportedFileExtensions => string.Join(";", extensions.Select(x => "*" + x));

        public IEnumerable<Type> RootAssetTypes => [assetType];

        public IEnumerable<Type> AdditionalAssetTypes => [];

        public bool IsSupportingFile(string filePath) => extensions.Contains(Path.GetExtension(filePath), StringComparer.OrdinalIgnoreCase);

        public AssetImporterParameters GetDefaultParameters(bool isForReImport) => new(RootAssetTypes);

        public IEnumerable<AssetItem> Import(UFile rawAssetPath, AssetImporterParameters importParameters)
        {
            throw new NotSupportedException("The test generator creates the assets itself.");
        }
    }
}
