// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Events;

namespace Stride.AssetCompiler.Tasks
{
    public static class PackAssetsHelper
    {
        /// <param name="hostAssemblies">Package-relative paths (lib/tfm/name.dll) of the assemblies the hosts load.</param>
        /// <param name="companionPackages">Companion declarations as <c>Kind:Name:Version[:Replaces[:Toolkit]]</c>, Replaces being ';'-separated.</param>
        /// <param name="packageKind">What the package carries (StridePackageKind); null or empty for a runtime package.</param>
        /// <param name="packageToolkit">The UI toolkit of an Editor package's views (StrideEditorToolkit); null or empty for a neutral one.</param>
        public static bool Run(Core.Diagnostics.Logger logger, string projectFile, string intermediatePackagePath, List<(string SourcePath, string PackagePath)> generatedItems, IReadOnlyList<string> hostAssemblies = null, string assetNamespace = null, IReadOnlyList<string> companionPackages = null, string packageKind = null, string packageToolkit = null)
        {
            var package = Package.Load(logger, projectFile, new PackageLoadParameters()
            {
                AutoCompileProjects = false,
                LoadAssemblyReferences = false,
                AutoLoadTemporaryAssets = false,
            });

            var outputPath = new UDirectory(new FileInfo(intermediatePackagePath).FullName);
            var newPackage = new Package
            {
                Meta = package.Meta,
                FullPath = UPath.Combine(outputPath, (UFile)package.FullPath.GetFileName()),
            };

            var resourceOutputPath = UPath.Combine(outputPath, (UDirectory)"Resources");
            var resourcesTargetToSource = new Dictionary<UFile, UFile>();
            var resourcesSourceToTarget = new Dictionary<UFile, UFile>();

            void RegisterItem(UFile targetFilePath)
            {
                generatedItems.Add((targetFilePath.ToOSPath(), UPath.Combine("stride", targetFilePath.MakeRelative(outputPath)).ToOSPath()));
            }

            void TryCopyDirectory(UDirectory sourceDirectory, UDirectory targetDirectory, string exclude = null)
            {
                var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
                matcher.AddInclude("**/*.*");
                if (exclude != null)
                {
                    foreach (var excludeEntry in exclude.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                        matcher.AddExclude(excludeEntry);
                }

                //var resourceFiles = Directory.EnumerateFiles(sourceDirectory, "*.*", SearchOption.AllDirectories);
                foreach (var resourceFile in matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(sourceDirectory))).Files)
                {
                    var resourceFilePath = UPath.Combine(sourceDirectory, (UFile)resourceFile.Path);
                    var targetFilePath = UPath.Combine(targetDirectory, (UFile)resourceFile.Path);

                    TryCopyResource(resourceFilePath, targetFilePath);
                }
            }

            void TryCopyResource(UFile resourceFilePath, UFile targetFilePath)
            {
                resourcesSourceToTarget.Add(resourceFilePath, targetFilePath);

                if (resourcesTargetToSource.TryGetValue(targetFilePath, out var otherResourceFilePath))
                {
                    logger.Error($"Could not copy resource file [{targetFilePath.MakeRelative(resourceOutputPath)}] because it exists in multiple locations: [{resourceFilePath.ToOSPath()}] and [{otherResourceFilePath.ToOSPath()}]");
                }
                else
                {
                    resourcesTargetToSource.Add(targetFilePath, resourceFilePath);

                    try
                    {
                        Directory.CreateDirectory(targetFilePath.GetFullDirectory());
                        File.Copy(resourceFilePath, targetFilePath, true);

                        RegisterItem(targetFilePath);
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Could not copy resource file from [{resourceFilePath.ToOSPath()}] to [{targetFilePath.MakeRelative(resourceOutputPath)}]", e);
                    }
                }
            }

            foreach (var resourceFolder in package.ResourceFolders)
            {
                if (!Directory.Exists(resourceFolder))
                    continue;

                TryCopyDirectory(resourceFolder, resourceOutputPath);
            }

            var assetOutputPath = UPath.Combine(outputPath, (UDirectory)"Assets");
            var assets = Package.ListAssetFiles(package, true, true);
            if (assets.Count > 0)
            {
                newPackage.AssetFolders.Add(new AssetFolder(assetOutputPath));

                foreach (var asset in assets)
                {
                    // Ignore source files
                    if (asset.FilePath.GetFileExtension() == ".cs")
                        continue;

                    var assetRelativePath = asset.FilePath.MakeRelative(asset.SourceFolder);
                    var outputFile = UPath.Combine(assetOutputPath, assetRelativePath);

                    try
                    {
                        var assetDirectory = asset.FilePath.GetFullDirectory();
                        Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

                        var parsingEvents = new List<ParsingEvent>();

                        using (var assetStream = File.OpenRead(asset.FilePath))
                        using (var streamReader = new StreamReader(assetStream))
                        {
                            var yamlEventReader = new EventReader(new Parser(streamReader));
                            yamlEventReader.ReadCurrent(parsingEvents);

                            var hasChanges = false;
                            foreach (var parsingEvent in parsingEvents)
                            {
                                if (parsingEvent is Scalar scalar)
                                {
                                    if (scalar.Tag == "!file")
                                    {
                                        // Transform to absolute path
                                        var sourceResourcePath = UPath.Combine(asset.FilePath.GetFullDirectory(), (UFile)scalar.Value);
                                        // Check if file was copied in resource
                                        if (!resourcesSourceToTarget.TryGetValue(sourceResourcePath, out var targetResourcePath))
                                        {
                                            // This file was not stored in resource, copy it manually
                                            targetResourcePath = UPath.Combine(resourceOutputPath, (UFile)sourceResourcePath.GetFileName());
                                            TryCopyResource(sourceResourcePath, targetResourcePath);
                                        }
                                        var newValue = targetResourcePath.MakeRelative(outputFile.GetFullDirectory());
                                        if (scalar.Value != newValue)
                                        {
                                            hasChanges = true;
                                            scalar.Value = newValue;
                                        }
                                    }
                                }
                            }

                            if (!hasChanges)
                            {
                                // We do this because pure text files could be parsed as YAML events even though they are not
                                File.Copy(asset.FilePath, outputFile, true);
                            }
                            else
                            {
                                using (var output = File.CreateText(outputFile))
                                {
                                    var emitter = new Emitter(output, AssetYamlSerializer.Default.GetSerializerSettings().PreferredIndent);
                                    foreach (var parsingEvent in parsingEvents)
                                    {
                                        emitter.Emit(parsingEvent);
                                    }
                                }
                            }

                            RegisterItem(outputFile);
                        }
                    }
                    catch (YamlException)
                    {
                        // Not a Yaml asset? Process it as binary (copy)
                        File.Copy(asset.FilePath, outputFile, true);
                        RegisterItem(outputFile);
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Could not process asset [{asset.FilePath}]", e);
                    }
                }
            }

            // If any resource was copied, add resource folder
            if (resourcesTargetToSource.Count > 0)
                newPackage.ResourceFolders.Add(resourceOutputPath);

            // Process templates
            if (package.TemplateFolders.Count > 0)
            {
                var templateOutputPath = UPath.Combine(outputPath, (UDirectory)"Templates");

                var targetFolder = new TemplateFolder(templateOutputPath);

                foreach (var templateFolder in package.TemplateFolders)
                {
                    UDirectory target = templateOutputPath;
                    if (templateFolder.Group != null)
                    {
                        target = UPath.Combine(target, templateFolder.Group);
                    }

                    TryCopyDirectory(templateFolder.Path, target, templateFolder.Exclude);

                    // Add template files
                    foreach (var templateFile in templateFolder.Files)
                    {
                        var newTemplateFile = templateFile.MakeRelative(templateFolder.Path);
                        if (templateFolder.Group != null)
                        {
                            newTemplateFile = UPath.Combine(templateFolder.Group, newTemplateFile);
                        }

                        newTemplateFile = UPath.Combine(targetFolder.Path, newTemplateFile);
                        targetFolder.Files.Add(newTemplateFile);
                    }
                }

                newPackage.TemplateFolders.Add(targetFolder);
            }

            foreach (var rootAsset in package.RootAssets)
                newPackage.RootAssets.Add(rootAsset);

            // Packed sdpkg stores the resolved namespace name (default = the authored package name),
            // never sentinels: the packed name is authoritative for consumers.
            var assetNamespaceDeclaration = !string.IsNullOrEmpty(assetNamespace) ? assetNamespace : package.AssetNamespace;
            newPackage.AssetNamespace = PackageContainer.ResolveAssetNamespace(assetNamespaceDeclaration, package.Meta.Name);

            // Packed sdpkg states what the package carries (a build property, absent from the authored sdpkg)
            if (!string.IsNullOrWhiteSpace(packageKind))
            {
                if (Enum.TryParse<PackageKind>(packageKind.Trim(), ignoreCase: true, out var ownKind))
                    newPackage.Kind = ownKind;
                else
                    logger.Error($"Package kind [{packageKind}] is not one of {string.Join(", ", Enum.GetNames<PackageKind>())}.");
            }
            if (!string.IsNullOrWhiteSpace(packageToolkit))
            {
                if (newPackage.Kind == PackageKind.Editor)
                    newPackage.Toolkit = packageToolkit.Trim();
                else
                    logger.Error($"Package toolkit [{packageToolkit}] needs the Editor kind; this package is {newPackage.Kind}.");
            }

            // Packed sdpkg stores the companion package names, versions, kinds and toolkits, read from the companion projects at pack time.
            if (companionPackages != null)
            {
                foreach (var declaration in companionPackages)
                {
                    var parts = declaration.Split(':');
                    if (parts.Length is < 3 or > 5 || !Enum.TryParse<PackageKind>(parts[0].Trim(), ignoreCase: true, out var kind) || kind == PackageKind.Runtime || string.IsNullOrWhiteSpace(parts[1]))
                    {
                        logger.Error($"Companion package declaration [{declaration}] is not of the form Kind:Name:Version[:Replaces[:Toolkit]] with Kind Assets or Editor.");
                        continue;
                    }
                    var companion = new CompanionPackage
                    {
                        Kind = kind,
                        Name = parts[1].Trim(),
                        Version = !string.IsNullOrWhiteSpace(parts[2]) ? new PackageVersion(parts[2].Trim()) : null,
                    };
                    if (parts.Length >= 4)
                    {
                        foreach (var replaced in parts[3].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                            companion.Replaces.Add(replaced);
                    }
                    if (parts.Length == 5 && !string.IsNullOrWhiteSpace(parts[4]))
                    {
                        if (kind != PackageKind.Editor)
                        {
                            logger.Error($"Companion package declaration [{declaration}] has a toolkit; only an Editor companion can.");
                            continue;
                        }
                        companion.Toolkit = parts[4].Trim();
                    }
                    newPackage.CompanionPackages.Add(companion);
                }
            }

            // Host-loadable assemblies, stored relative to the packed sdpkg (at stride/X.sdpkg).
            // Each path is lib/<tfm>/<name>.dll (built by the pack target); tag the entry with its TFM
            // so a multi-targeted package lets the consumer load the build matching its compiler runtime.
            if (hostAssemblies != null)
            {
                // The TFM is the path segment right after "lib".
                static string TargetFrameworkFromPath(string libRelativePath)
                {
                    var parts = libRelativePath.Split('/');
                    for (var i = 0; i < parts.Length - 1; i++)
                        if (string.Equals(parts[i], "lib", StringComparison.OrdinalIgnoreCase))
                            return parts[i + 1];
                    return null;
                }

                foreach (var hostAssembly in hostAssemblies)
                {
                    var normalized = hostAssembly.Replace('\\', '/');
                    newPackage.HostAssemblies.Add(new AssetAssembly(TargetFrameworkFromPath(normalized), (UFile)("../" + normalized)));
                }
            }

            // Save package if there are resources, assets, or declarations
            if (generatedItems.Count > 0 || newPackage.HostAssemblies.Count > 0 || newPackage.CompanionPackages.Count > 0 || newPackage.Kind != PackageKind.Runtime || newPackage.Toolkit is not null)
            {
                // Make sure we have a standalone package
                var standalonePackage = new StandalonePackage(newPackage);
                standalonePackage.Save(logger);
                RegisterItem(newPackage.FullPath);
            }

            return !logger.HasErrors;
        }
    }
}
