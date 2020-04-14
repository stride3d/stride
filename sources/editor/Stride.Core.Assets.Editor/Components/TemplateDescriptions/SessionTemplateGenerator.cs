// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Stride.Core.Assets.Quantum;
using Stride.Core.Assets.Templates;
using Stride.Core.Yaml;
using System.IO;
using Stride.Core.IO;

namespace Stride.Core.Assets.Editor.Components.TemplateDescriptions
{
    /// <summary>
    /// An implementation of <see cref="ITemplateGenerator"/> that will save the session and update the assembly references.
    /// An <see cref="AfterSave"/> protected method is provided to do additional work after saving.
    /// </summary>
    public abstract class SessionTemplateGenerator : TemplateGeneratorBase<SessionTemplateGeneratorParameters>
    {
        private readonly AssetPropertyGraphContainer graphContainer = new AssetPropertyGraphContainer(new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } });
        // TODO: move .gitignore content into an external file
        private static readonly string GitIgnore = @"
*.user
*.lock
*.lock.json
.vs/
_ReSharper*
*.suo
*.VC.db
*.vshost.exe
*.manifest
*.sdf
[Bb]in/
obj/
Cache/
"; //sadly templates and new games are different, the first ones are basically a copy the second is more programatically, so our best bet to have something easy to mantain is this for now.

        public sealed override bool Run(SessionTemplateGeneratorParameters parameters)
        {
            var result = Generate(parameters);
            if (!result)
                return false;

            SaveSession(parameters);

            // Load missing references (we do this after saving)
            // TODO: Better tracking of ProjectReferences (added, removed, etc...)
            parameters.Logger.Verbose("Compiling game assemblies...");
            parameters.Session.UpdateAssemblyReferences(parameters.Logger);
            parameters.Logger.Verbose("Game assemblies compiled...");

            result = AfterSave(parameters).Result;

            return result;
        }

        /// <summary>
        /// Generates the template. This method is called by <see cref="SessionTemplateGenerator.Run"/>, and the session is saved afterward
        /// if the generation is successful.
        /// </summary>
        /// <param name="parameters">The parameters for the template generator.</param>
        /// <remarks>
        /// This method should work in unattended mode and should not ask user for information anymore.
        /// </remarks>
        /// <returns><c>True</c> if the generation was successful, <c>false</c> otherwise.</returns>
        protected abstract bool Generate(SessionTemplateGeneratorParameters parameters);

        /// <summary>
        /// Does additional work after the session has been saved.
        /// </summary>
        /// <param name="parameters">The parameters for the template generator.</param>
        /// <returns>True if the method succeeded, False otherwise.</returns>
        protected virtual Task<bool> AfterSave(SessionTemplateGeneratorParameters parameters)
        {
            return Task.FromResult(true);
        }

        protected void ApplyMetadata(SessionTemplateGeneratorParameters parameters)
        {
            // Create graphs for all assets in the session
            EnsureGraphs(parameters);

            // Then apply metadata from each asset item to the graph
            foreach (var package in parameters.Session.LocalPackages)
            {
                foreach (var asset in package.Assets)
                {                    
                    var graph = graphContainer.TryGetGraph(asset.Id) ?? graphContainer.InitializeAsset(asset, parameters.Logger);
                    var overrides = asset.YamlMetadata.RetrieveMetadata(AssetObjectSerializerBackend.OverrideDictionaryKey);
                    if (graph != null && overrides != null)
                    {
                        graph.RefreshBase();
                        AssetPropertyGraph.ApplyOverrides(graph.RootNode, overrides);
                    }
                }
            }
        }

        protected void SaveSession(SessionTemplateGeneratorParameters parameters)
        {
            // Create graphs for all assets in the session
            EnsureGraphs(parameters);

            // Then run a PrepareForSave pass to prepare asset items to be saved (override, object references, etc.)
            foreach (var package in parameters.Session.LocalPackages)
            {
                foreach (var asset in package.Assets)
                {
                    var graph = graphContainer.TryGetGraph(asset.Id);
                    graph?.PrepareForSave(parameters.Logger, asset);
                }
            }

            // Finally actually save the session.
            parameters.Session.DependencyManager.BeginSavingSession();
            parameters.Session.SourceTracker.BeginSavingSession();
            parameters.Session.Save(parameters.Logger);
            parameters.Session.SourceTracker.EndSavingSession();
            parameters.Session.DependencyManager.EndSavingSession();
        }

        protected void WriteGitIgnore(SessionTemplateGeneratorParameters parameters)
        {
            var fileName = UFile.Combine(parameters.OutputDirectory, ".gitignore");
            File.WriteAllText(fileName.ToWindowsPath(), GitIgnore);
        }

        private void EnsureGraphs(SessionTemplateGeneratorParameters parameters)
        {
            foreach (var package in parameters.Session.Packages)
            {
                foreach (var asset in package.Assets)
                {
                    if (graphContainer.TryGetGraph(asset.Id) == null)
                        graphContainer.InitializeAsset(asset, parameters.Logger);
                }
            }
        }
    }
}
