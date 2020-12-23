// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.BuildEngine;
using Stride.Assets.Effect;

namespace Stride.Editor.Build
{
    public class StrideShaderImporter
    {
        /// <summary>
        /// The current session being processed
        /// </summary>
        private readonly HashSet<string> systemProjectsLoaded = new HashSet<string>();

        private class UpdateImportShaderCacheBuildStep : BuildStep
        {
            private readonly HashSet<string> cachedProject;

            private readonly List<string> importedProjectIds;

            public UpdateImportShaderCacheBuildStep(HashSet<string> cachedProject, List<string> importedProjectIds)
            {
                this.cachedProject = cachedProject;
                this.importedProjectIds = importedProjectIds;
            }

            public override string Title
            {
                get { return "UpdateImportShaderCacheBuildStep"; }
            }

            public override Task<ResultStatus> Execute(IExecuteContext executeContext, BuilderContext builderContext)
            {
                // check the status of the import build steps
                if (((ListBuildStep)Parent).Steps.Any(s => s.Failed))
                    return Task.FromResult(ResultStatus.Successful);

                // Mark System projects as loaded
                foreach (var projectId in importedProjectIds)
                    cachedProject.Add(projectId);

                return Task.FromResult(ResultStatus.Successful);
            }

            public override string ToString()
            {
                return Title;
            }
        }

        /// <summary>
        /// Creates a build step that will build all shaders from system packages.
        /// </summary>
        /// <param name="session">The session used to retrieve currently used system packages.</param>
        /// <returns>A <see cref="ListBuildStep"/> containing the steps to build all shaders from system packages.</returns>
        public ListBuildStep CreateSystemShaderBuildSteps(SessionViewModel session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            // Check if there are any new system projects to preload
            // TODO: PDX-1251: For now, allow non-system project as well (which means they will be loaded only once at startup)
            // Later, they should be imported depending on what project the currently previewed/built asset is
            var systemPackages = session.AllPackages.Where(project => /*project.IsSystem &&*/ !systemProjectsLoaded.Contains(project.Package.Meta.Name)).ToList();
            if (systemPackages.Count == 0)
                return null;

            var importShadersRootProject = new StandalonePackage(new Package());
            var importShadersProjectSession = new PackageSession();
            importShadersProjectSession.Projects.Add(importShadersRootProject);

            foreach (var package in systemPackages)
            {
                var mapPackage = new Package { FullPath = package.PackagePath };
                foreach (var asset in package.Assets)
                {
                    if (typeof(EffectShaderAsset).IsAssignableFrom(asset.AssetType))
                        mapPackage.Assets.Add(new AssetItem(asset.Url, asset.Asset) { SourceFolder = asset.AssetItem.SourceFolder, AlternativePath = asset.AssetItem.AlternativePath });
                }

                importShadersProjectSession.Projects.Add(new StandalonePackage(mapPackage));
                importShadersRootProject.FlattenedDependencies.Add(new Dependency(mapPackage));
            }

            // compile the fake project (create the build steps)
            var assetProjectCompiler = new PackageCompiler(new PackageAssetEnumerator(importShadersRootProject.Package));
            var context = new AssetCompilerContext { CompilationContext = typeof(AssetCompilationContext) };
            var dependenciesCompileResult = assetProjectCompiler.Prepare(context);
            context.Dispose();

            var buildSteps = dependenciesCompileResult.BuildSteps;
            buildSteps?.Add(new UpdateImportShaderCacheBuildStep(systemProjectsLoaded, systemPackages.Select(x => x.Package.Meta.Name).ToList()));

            return buildSteps;
        }

        public ListBuildStep CreateUserShaderBuildSteps(SessionViewModel session)
        {
            var packages = session.AllPackages.Where(project => !project.Package.IsSystem).ToList();
            if (packages.Count == 0)
                return null;

            var importShadersRootProject = new StandalonePackage(new Package());
            var importShadersProjectSession = new PackageSession();
            importShadersProjectSession.Projects.Add(importShadersRootProject);

            foreach (var package in packages)
            {
                var mapPackage = new Package { FullPath = package.PackagePath };
                foreach (var asset in package.Assets)
                {
                    if (typeof(EffectShaderAsset).IsAssignableFrom(asset.AssetType))
                    {
                        mapPackage.Assets.Add(new AssetItem(asset.Url, asset.Asset) { SourceFolder = asset.AssetItem.SourceFolder, AlternativePath = asset.AssetItem.AlternativePath });
                    }
                }

                importShadersProjectSession.Projects.Add(new StandalonePackage(mapPackage));
                importShadersRootProject.FlattenedDependencies.Add(new Dependency(mapPackage));
            }

            // compile the fake project (create the build steps)
            var assetProjectCompiler = new PackageCompiler(new PackageAssetEnumerator(importShadersRootProject.Package));
            var dependenciesCompileResult = assetProjectCompiler.Prepare(new AssetCompilerContext { CompilationContext = typeof(AssetCompilationContext) });

            var buildSteps = dependenciesCompileResult.BuildSteps;
            buildSteps?.Add(new UpdateImportShaderCacheBuildStep(new HashSet<string>(), packages.Select(x => x.Package.Meta.Name).ToList()));

            return buildSteps;
        }
    }
}
