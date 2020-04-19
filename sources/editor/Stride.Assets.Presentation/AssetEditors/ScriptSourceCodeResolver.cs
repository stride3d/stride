// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Reflection;
using Stride.Core.Presentation.Services;
using Stride.Assets.Presentation.AssetEditors;
using Stride.Assets.Presentation.ViewModel;
using Stride.Assets.Scripts;
using Stride.Engine;

namespace Stride.Assets.Presentation.AssetEditors
{
    public static class SymbolExtensions
    {
        public static string GetFullNamespace(this ISymbol symbol)
        {
            if (String.IsNullOrEmpty(symbol.ContainingNamespace?.Name))
            {
                return null;
            }

            // get the rest of the full namespace string
            var restOfResult = symbol.ContainingNamespace.GetFullNamespace();

            var result = symbol.ContainingNamespace.Name;

            if (restOfResult != null)
                // if restOfResult is not null, append it after a period
                result = restOfResult + '.' + result;

            return result;
        }
    }

    public class ScriptSourceCodeResolver : IScriptSourceCodeResolver
    {
        private ProjectWatcher watcher;
        private readonly Dictionary<string, List<Type>> typesForPath = new Dictionary<string, List<Type>>();

        public ScriptSourceCodeResolver()
        {
        }

        public Compilation LatestCompilation { get; private set; }

        public event EventHandler LatestCompilationChanged;

        public async Task Initialize(SessionViewModel session, ProjectWatcher watcher, CancellationToken token)
        {
            this.watcher = watcher;
            var assemblies = watcher.TrackedAssemblies.ToList();
            foreach (var trackedAssembly in assemblies.Where(trackedAssembly => trackedAssembly.Project != null))
            {
                await AnalyzeProject(session, trackedAssembly.Project, token);
            }
        }

        public async Task AnalyzeProject(SessionViewModel session, Project project, CancellationToken token)
        {
            //Handle Scripts discovery
            var gameProjectCompilation = await project.GetCompilationAsync(token);

            // Update latest compilation
            LatestCompilation = gameProjectCompilation;
            LatestCompilationChanged?.Invoke(this, EventArgs.Empty);

            var assemblyFullName = gameProjectCompilation.Assembly.Identity.GetDisplayName();

            var strideScriptType = gameProjectCompilation.GetTypeByMetadataName(typeof(ScriptComponent).FullName);

            var symbols = gameProjectCompilation.GetSymbolsWithName(x => true, SymbolFilter.Type).Cast<ITypeSymbol>().ToList();
            if (!symbols.Any())
            {
                return;
            }

            var assembly = AssemblyRegistry.FindAll()?.FirstOrDefault(x => x.FullName == assemblyFullName);
            if (assembly == null)
            {
                return;
            }

            var types = assembly.GetTypes();

            var typesDict = new Dictionary<string, List<Type>>();

            foreach (var symbol in symbols)
            {
                //recurse basetypes up to finding Script type
                var baseType = symbol.BaseType;
                var scriptType = false;
                while (baseType != null)
                {
                    if (baseType == strideScriptType)
                    {
                        scriptType = true;
                        break;
                    }
                    baseType = baseType.BaseType;
                }

                if (!scriptType) continue;

                //find the script paths, (could be multiple in the case of partial)
                foreach (var location in symbol.Locations)
                {
                    var csPath = new UFile(location.SourceTree.FilePath);

                    //find the real type, and add to the dictionary
                    var realType = types.FirstOrDefault(x => x.Name == symbol.Name && x.Namespace == symbol.GetFullNamespace());

                    if (!typesDict.ContainsKey(csPath))
                    {
                        typesDict.Add(csPath, new List<Type> { realType });
                    }
                    else
                    {
                        typesDict[csPath].Add(realType);
                    }
                }
            }

            lock (this)
            {
                foreach (var x in typesDict)
                {
                    typesForPath[x.Key] = x.Value;
                }

                //clean up paths that do not exist anymore
                var toRemove = typesForPath.Where(x => !File.Exists(x.Key)).Select(x => x.Key).ToList();
                typesForPath.RemoveWhere(x => toRemove.Contains(x.Key));
            } 
        }

        public async void Updater(SessionViewModel session, CancellationToken token)
        {
            var buffer = new BufferBlock<AssemblyChangedEvent>();
            using (watcher.AssemblyChangedBroadcast.LinkTo(buffer))
            {
                while (!token.IsCancellationRequested)
                {
                    var change = await buffer.ReceiveAsync(token);
                    if (change?.Project == null)
                        continue;

                    // Ignore Binary changes
                    if (change.ChangeType == AssemblyChangeType.Binary)
                        continue;

                    await AnalyzeProject(session, change.Project, token);
                }
            }
        }

        public IEnumerable<Type> GetTypesFromSourceFile(UFile file)
        {
            if (file == null) return Enumerable.Empty<Type>();

            lock (this)
            {
                return !typesForPath.ContainsKey(file) ? Enumerable.Empty<Type>() : typesForPath[file];
            }
        }
    }
}
