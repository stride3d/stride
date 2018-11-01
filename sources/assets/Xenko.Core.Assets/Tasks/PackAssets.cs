// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using Xenko.Core.Diagnostics;
using Xenko.Core.IO;
using Xenko.Core.Yaml;
using Xenko.Core.Yaml.Events;

namespace Xenko.Core.Assets.Tasks
{
    public class PackAssets : Task
    {
        [Required]
        public ITaskItem ProjectFile { get; set; }

        [Required]
        public ITaskItem IntermediatePackagePath { get; set; }

        [Output]
        public ITaskItem[] GeneratedItems { get; private set; }

        public override bool Execute()
        {
            var result = new RedirectLog(Log);
            var package = Package.Load(result, ProjectFile.ItemSpec, new PackageLoadParameters()
            {
                AutoCompileProjects = false,
                LoadAssemblyReferences = false,
                AutoLoadTemporaryAssets = false,
            });

            var generatedItems = new List<ITaskItem>();
            var outputPath = new UDirectory(new FileInfo(IntermediatePackagePath.ItemSpec).FullName);
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
                var generatedItem = new TaskItem(targetFilePath.ToWindowsPath());
                generatedItem.SetMetadata("Pack", "true");
                generatedItem.SetMetadata("PackagePath", UPath.Combine("xenko", targetFilePath.MakeRelative(outputPath)).ToWindowsPath());
                generatedItems.Add(generatedItem);
            }

            void TryCopyDirectory(UDirectory sourceDirectory, UDirectory targetDirectory)
            {
                var resourceFiles = Directory.EnumerateFiles(sourceDirectory, "*.*", SearchOption.AllDirectories);
                foreach (var resourceFile in resourceFiles)
                {
                    var resourceFilePath = (UFile)resourceFile;
                    var targetFilePath = UPath.Combine(targetDirectory, resourceFilePath.MakeRelative(sourceDirectory));

                    TryCopyResource(resourceFilePath, targetFilePath);
                }
            }

            void TryCopyResource(UFile resourceFilePath, UFile targetFilePath)
            {
                resourcesSourceToTarget.Add(resourceFilePath, targetFilePath);

                if (resourcesTargetToSource.TryGetValue(targetFilePath, out var otherResourceFilePath))
                {
                    result.Error($"Could not copy resource file [{targetFilePath.MakeRelative(resourceOutputPath)}] because it exists in multiple locations: [{resourceFilePath.ToWindowsPath()}] and [{otherResourceFilePath.ToWindowsPath()}]");
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
                        result.Error($"Could not copy resource file from [{resourceFilePath.ToWindowsPath()}] to [{targetFilePath.MakeRelative(resourceOutputPath)}]", e);
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
            var assets = Package.ListAssetFiles(result, package, true, true, null);
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
                                        var newValue = targetResourcePath.MakeRelative(assetOutputPath);
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
                    catch (YamlException e)
                    {
                        // Not a Yaml asset? Process it as binary (copy)
                        File.Copy(asset.FilePath, outputFile, true);
                        RegisterItem(outputFile);
                    }
                    catch (Exception e)
                    {
                        result.Error($"Could not process asset [{asset.FilePath}]", e);
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

                    TryCopyDirectory(templateFolder.Path, target);

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

            // Save package only if there is any resources and/or assets
            if (generatedItems.Count > 0)
            {
                // Make sure we have a standalone package
                var standalonePackage = new StandalonePackage(newPackage);
                standalonePackage.Save(result);
                RegisterItem(newPackage.FullPath);
            }

            GeneratedItems = generatedItems.ToArray();

            return !result.HasErrors;
        }

        class RedirectLog : Core.Diagnostics.Logger
        {
            TaskLoggingHelper log;

            public RedirectLog(TaskLoggingHelper log)
            {
                this.log = log;

                // Report warnings and errors
                ActivateLog(LogMessageType.Warning);
            }

            protected override void LogRaw(ILogMessage logMessage)
            {
                switch (logMessage.Type)
                {
                    case LogMessageType.Debug:
                    case LogMessageType.Verbose:
                    case LogMessageType.Info:
                        log.LogMessage(logMessage.Text);
                        break;
                    case LogMessageType.Warning:
                        log.LogWarning(logMessage.Text);
                        break;
                    case LogMessageType.Error:
                    case LogMessageType.Fatal:
                        log.LogError(logMessage.Text);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
