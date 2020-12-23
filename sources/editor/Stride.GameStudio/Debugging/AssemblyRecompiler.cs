// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Mono.Cecil;
using QuickGraph;
using QuickGraph.Algorithms;
using QuickGraph.Algorithms.Condensation;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using AssemblyProcessorProgram = Stride.Core.AssemblyProcessor.AssemblyProcessorProgram;
using ProjectReference = Microsoft.CodeAnalysis.ProjectReference;

namespace Stride.GameStudio.Debugging
{
    public partial class AssemblyRecompiler
    {
        private SourceGroup[] previousSortedConnectedGroups = new SourceGroup[0];
        private IMutableBidirectionalGraph<SourceGroup, CondensedEdge<SyntaxTree, SEdge<SyntaxTree>, SourceGroup>> previousStronglyConnected = new BidirectionalGraph<SourceGroup, CondensedEdge<SyntaxTree, SEdge<SyntaxTree>, SourceGroup>>();
        private ImmutableHashSet<SourceGroup> previousConnectedGroups;
        private int assemblyCounter;
        private Solution solution;

        public AssemblyRecompiler()
        {
            previousConnectedGroups = ImmutableHashSet.Create(SourceGroupComparer.Default, previousSortedConnectedGroups);
        }

        public async Task<UpdateResult> Recompile(Project gameProject, LoggerResult logger)
        {
            var result = new UpdateResult(logger);
            if (solution == null)
                solution = gameProject.Solution;

            // Detect new groups
            var gameProjectCompilation = await gameProject.GetCompilationAsync();

            // Generate source dependency graph
            var sourceDependencyGraph = new SourceGroup();

            // Make sure all vertices are added
            foreach (var syntaxTree in gameProjectCompilation.SyntaxTrees)
            {
                sourceDependencyGraph.AddVertex(syntaxTree);
            }

            foreach (var syntaxTree in gameProjectCompilation.SyntaxTrees)
            {
                var syntaxRoot = syntaxTree.GetRoot();
                var semanticModel = gameProjectCompilation.GetSemanticModel(syntaxTree);

                var dependencies = new SourceDependencySyntaxVisitor(new HashSet<SyntaxTree>(gameProjectCompilation.SyntaxTrees), semanticModel).DefaultVisit(syntaxRoot);

                foreach (var dependency in dependencies)
                {
                    sourceDependencyGraph.AddEdge(new SEdge<SyntaxTree>(syntaxTree, dependency));
                }
            }

            // Generate strongly connected components for sources (group of sources that needs to be compiled together, and their dependencies)
            var stronglyConnected = sourceDependencyGraph.CondensateStronglyConnected<SyntaxTree, SEdge<SyntaxTree>, SourceGroup>();
            var sortedConnectedGroups = stronglyConnected.TopologicalSort().ToArray();
            var connectedGroups = ImmutableHashSet.Create(SourceGroupComparer.Default, sortedConnectedGroups);

            // Merge changes since previous time
            // 1. Tag obsolete groups (everything that don't match, and their dependencies)
            var groupsToUnload = new HashSet<SourceGroup>(SourceGroupComparer.Default);
            foreach (var sourceGroup in previousSortedConnectedGroups.Reverse())
            {
                // Does this group needs reload?
                SourceGroup newSourceGroup;
                if (connectedGroups.TryGetValue(sourceGroup, out newSourceGroup))
                {
                    // Transfer project, as it can be reused
                    newSourceGroup.Project = sourceGroup.Project;
                    newSourceGroup.PE = sourceGroup.PE;
                    newSourceGroup.PDB = sourceGroup.PDB;
                    newSourceGroup.Assembly = sourceGroup.Assembly;
                }
                else
                {
                    groupsToUnload.Add(sourceGroup);
                }

                // Mark dependencies
                if (groupsToUnload.Contains(sourceGroup))
                {
                    foreach (var test in previousStronglyConnected.InEdges(sourceGroup))
                    {
                        groupsToUnload.Add(test.Source);
                    }
                }
            }

            // Generate common InternalsVisibleTo attributes
            // TODO: Find more graceful solution
            var internalsVisibleToBuilder = new StringBuilder();
            internalsVisibleToBuilder.Append("using System.Runtime.CompilerServices;");

            for (int i = 0; i < 1000; i++)
                internalsVisibleToBuilder.AppendFormat(@"[assembly: InternalsVisibleTo(""{0}.Part{1}"")]", gameProject.AssemblyName, i);

            var internalsVisibleToSource = CSharpSyntaxTree.ParseText(internalsVisibleToBuilder.ToString(), null, "", Encoding.UTF8).GetText();

            // 2. Compile assemblies
            foreach (var sourceGroup in sortedConnectedGroups.Reverse())
            {
                // Check if it's either a new group, or one that has been unloaded
                if (!previousConnectedGroups.Contains(sourceGroup) || groupsToUnload.Contains(sourceGroup))
                {
                    var assemblyName = gameProject.AssemblyName + ".Part" + assemblyCounter++;

                    // Create a project out of the source group
                    var project = solution.AddProject(assemblyName, assemblyName, LanguageNames.CSharp)
                        .WithMetadataReferences(gameProject.MetadataReferences)
                        .WithProjectReferences(gameProject.AllProjectReferences)
                        .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                    // Add sources
                    foreach (var syntaxTree in sourceGroup.Vertices)
                    {
                        project = project.AddDocument(syntaxTree.FilePath, syntaxTree.GetText()).Project;
                    }

                    // Add references to other sources
                    foreach (var dependencySourceGroup in stronglyConnected.OutEdges(sourceGroup))
                    {
                        project = project.AddProjectReference(new ProjectReference(dependencySourceGroup.Target.Project.Id));
                    }

                    // Make internals visible to other assembly parts
                    project = project.AddDocument("GeneratedInternalsVisibleTo", internalsVisibleToSource).Project;

                    sourceGroup.Project = project;
                    solution = project.Solution;

                    var compilation = await project.GetCompilationAsync();

                    using (var peStream = new MemoryStream())
                    using (var pdbStream = new MemoryStream())
                    {
                        var emitResult = compilation.Emit(peStream, pdbStream);
                        result.Info($"Compiling assembly containing {sourceGroup}");

                        foreach (var diagnostic in emitResult.Diagnostics)
                        {
                            switch (diagnostic.Severity)
                            {
                                case DiagnosticSeverity.Error:
                                    result.Error(diagnostic.GetMessage());
                                    break;
                                case DiagnosticSeverity.Warning:
                                    result.Warning(diagnostic.GetMessage());
                                    break;
                                case DiagnosticSeverity.Info:
                                    result.Info(diagnostic.GetMessage());
                                    break;
                            }
                        }

                        if (!emitResult.Success)
                        {
                            result.Error($"Error compiling assembly containing {sourceGroup}");
                            break;
                        }

                        // Load csproj to evaluate assembly processor parameters
                        var msbuildProject = await Task.Run(() => VSProjectHelper.LoadProject(gameProject.FilePath));
                        if (msbuildProject.GetPropertyValue("StrideAssemblyProcessor") == "true")
                        {
                            var referenceBuild = await Task.Run(() => VSProjectHelper.CompileProjectAssemblyAsync(null, gameProject.FilePath, result, "ResolveReferences", flags: Microsoft.Build.Execution.BuildRequestDataFlags.ProvideProjectStateAfterBuild));
                            if (referenceBuild == null)
                            {
                                result.Error("Could not properly run ResolveAssemblyReferences");
                                break;
                            }
                            var referenceBuildResult = await referenceBuild.BuildTask;
                            if (referenceBuild.IsCanceled || result.HasErrors)
                                break;

                            var assemblyProcessorParameters = "--parameter-key --auto-module-initializer --serialization";
                            var assemblyProcessorApp = AssemblyProcessorProgram.CreateAssemblyProcessorApp(SplitCommandLine(assemblyProcessorParameters).ToArray(), new LoggerAssemblyProcessorWrapper(result));

                            foreach (var referencePath in referenceBuildResult.ProjectStateAfterBuild.Items.Where(x => x.ItemType == "ReferencePath"))
                            {
                                assemblyProcessorApp.References.Add(referencePath.EvaluatedInclude);
                                if (referencePath.EvaluatedInclude.EndsWith("Stride.SpriteStudio.Runtime.dll")) //todo hard-coded! needs to go when plug in system is in
                                {
                                    assemblyProcessorApp.ReferencesToAdd.Add(referencePath.EvaluatedInclude);
                                }
                                else if (referencePath.EvaluatedInclude.EndsWith("Stride.Physics.dll")) //todo hard-coded! needs to go when plug in system is in
                                {
                                    assemblyProcessorApp.ReferencesToAdd.Add(referencePath.EvaluatedInclude);
                                }
                                else if (referencePath.EvaluatedInclude.EndsWith("Stride.Particles.dll")) //todo hard-coded! needs to go when plug in system is in
                                {
                                    assemblyProcessorApp.ReferencesToAdd.Add(referencePath.EvaluatedInclude);
                                }
                                else if (referencePath.EvaluatedInclude.EndsWith("Stride.Native.dll")) //todo hard-coded! needs to go when plug in system is in
                                {
                                    assemblyProcessorApp.ReferencesToAdd.Add(referencePath.EvaluatedInclude);
                                }
                                else if (referencePath.EvaluatedInclude.EndsWith("Stride.UI.dll")) //todo hard-coded! needs to go when plug in system is in
                                {
                                    assemblyProcessorApp.ReferencesToAdd.Add(referencePath.EvaluatedInclude);
                                }
                                else if (referencePath.EvaluatedInclude.EndsWith("Stride.Video.dll")) //todo hard-coded! needs to go when plug in system is in
                                {
                                    assemblyProcessorApp.ReferencesToAdd.Add(referencePath.EvaluatedInclude);
                                }
                            }

                            var assemblyResolver = assemblyProcessorApp.CreateAssemblyResolver();

                            // Add dependencies to assembly resolver
                            var recursiveDependencies = stronglyConnected.OutEdges(sourceGroup).SelectDeep(edge => stronglyConnected.OutEdges(edge.Target));
                            foreach (var dependencySourceGroup in recursiveDependencies)
                            {
                                assemblyResolver.Register(dependencySourceGroup.Target.Assembly, dependencySourceGroup.Target.PE);
                                assemblyProcessorApp.MemoryReferences.Add(dependencySourceGroup.Target.Assembly);
                            }

                            // Rewind streams
                            peStream.Position = 0;
                            pdbStream.Position = 0;

                            var assemblyDefinition = AssemblyDefinition.ReadAssembly(peStream,
                                new ReaderParameters { AssemblyResolver = assemblyResolver, ReadSymbols = true, SymbolStream = pdbStream });

                            // Run assembly processor
                            bool readWriteSymbols = true;
                            bool modified;
                            assemblyProcessorApp.SerializationAssembly = true;
                            if (!assemblyProcessorApp.Run(ref assemblyDefinition, ref readWriteSymbols, out modified, out var _))
                            {
                                result.Error("Error running assembly processor");
                                break;
                            }

                            sourceGroup.Assembly = assemblyDefinition;

                            // Write to file for now, since Cecil does not use the SymbolStream
                            var peFileName = Path.ChangeExtension(Path.GetTempFileName(), ".dll");
                            var pdbFileName = Path.ChangeExtension(peFileName, ".pdb");
                            assemblyDefinition.Write(peFileName, new WriterParameters { WriteSymbols = true });

                            sourceGroup.PE = File.ReadAllBytes(peFileName);
                            sourceGroup.PDB = File.ReadAllBytes(pdbFileName);

                            File.Delete(peFileName);
                            File.Delete(pdbFileName);
                        }
                        else
                        {
                            sourceGroup.PE = peStream.ToArray();
                            sourceGroup.PDB = pdbStream.ToArray();
                        }
                    }
                }
            }

            // We register unloading/loading only if everything succeeded
            if (!result.HasErrors)
            {
                // 3. Register old assemblies to unload
                foreach (var sourceGroup in previousSortedConnectedGroups)
                {
                    if (groupsToUnload.Contains(sourceGroup))
                    {
                        sourceGroup.Project = null;
                        sourceGroup.Assembly = null;
                        result.UnloadedProjects.Add(sourceGroup);
                    }
                }

                // 4. Register new assemblies to load
                foreach (var sourceGroup in sortedConnectedGroups.Reverse())
                {
                    // Check if it's either a new group, or one that has been unloaded
                    if (!previousConnectedGroups.Contains(sourceGroup) || groupsToUnload.Contains(sourceGroup))
                    {
                        result.LoadedProjects.Add(sourceGroup);
                    }
                }

                // Set as new state
                previousSortedConnectedGroups = sortedConnectedGroups;
                previousStronglyConnected = stronglyConnected;
                previousConnectedGroups = connectedGroups;
            }

            return result;
        }

        public static IEnumerable<string> SplitCommandLine(string commandLine)
        {
            bool inQuotes = false;

            return Split(commandLine, c =>
                                        {
                                            if (c == '\"')
                                                inQuotes = !inQuotes;

                                            return !inQuotes && c == ' ';
                                        })
                                .Select(arg => TrimMatchingQuotes(arg.Trim(), '\"'))
                                .Where(arg => !string.IsNullOrEmpty(arg));
        }

        public static string TrimMatchingQuotes(string input, char quote)
        {
            if ((input.Length >= 2) && 
                (input[0] == quote) && (input[input.Length - 1] == quote))
                return input.Substring(1, input.Length - 2);

            return input;
        }

        public static IEnumerable<string> Split(string str, Func<char, bool> controller)
        {
            int nextPiece = 0;

            for (int c = 0; c < str.Length; c++)
            {
                if (controller(str[c]))
                {
                    yield return str.Substring(nextPiece, c - nextPiece);
                    nextPiece = c + 1;
                }
            }

            yield return str.Substring(nextPiece);
        }

        /// <summary>
        /// Wraps <see cref="ILogger"/> defined by Core into one defined by AssemblyProcessor (source code sharing, not the same class).
        /// </summary>
        class LoggerAssemblyProcessorWrapper : TextWriter
        {
            private readonly ILogger result;
            private readonly StringBuilder content = new StringBuilder();

            public LoggerAssemblyProcessorWrapper(ILogger result)
            {
                this.result = result;
            }

            public string Module
            {
                get { return result.Module; }
            }

            public override void Write(char value)
            {
                if (value == '\n')
                {
                    result.Log(new LogMessage("AssemblyProcessor", LogMessageType.Info, content.ToString()));
                    content.Clear();
                }
                else
                {
                    content.Append(value);
                }
            }

            public override Encoding Encoding => Encoding.UTF8;
        }
    }
}
