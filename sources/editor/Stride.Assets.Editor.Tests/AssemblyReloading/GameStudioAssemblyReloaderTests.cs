// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using System.Reflection.Emit;
using Stride.Assets.Editor.AssemblyReloading;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Quantum;
using Stride.Core.Diagnostics;
using Stride.Core.Reflection;
using Stride.Core.Serialization;
using Stride.Core.Storage;
using Stride.Engine;

namespace Stride.Assets.Editor.Tests.AssemblyReloading;

/// <summary>
/// A game assembly reloaded in the editor's process: the live objects of its types take the types of the new copy and keep
/// their values, and the reload is undone and redone as one change.
/// </summary>
public sealed class GameStudioAssemblyReloaderTests
{
    private const string ProbeTypeName = "ReloadProbe.ProbeComponent";

    [Fact]
    public async Task AComponentOfAReloadedAssemblyTakesTheNewTypeAndKeepsItsValues()
    {
        await using var probe = new ProbeAssemblies();
        await using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false, [("MainScene", new SceneAsset())]);
        Assert.False(fixture.Result.HasErrors, fixture.Log);
        var loadedAssembly = probe.LoadFirstVersion(fixture);

        var component = (EntityComponent)Activator.CreateInstance(loadedAssembly.Assembly!.GetType(ProbeTypeName)!)!;
        component.GetType().GetField("Speed")!.SetValue(component, 7);
        AddEntity(fixture, new Entity("Probe") { component });

        var undoRedo = fixture.Session.UndoRedoService;
        var log = new LoggerResult();
        using (undoRedo.CreateTransaction())
        {
            GameStudioAssemblyReloader.Reload(fixture.Session, log, () => { }, () => { }, new() { [loadedAssembly] = probe.SecondVersionPath });
        }
        Assert.DoesNotContain(log.Messages, x => x.Type >= LogMessageType.Warning);

        var reloaded = ProbeComponent(fixture);
        Assert.Same(loadedAssembly.Assembly, reloaded.GetType().Assembly);
        Assert.NotSame(probe.FirstVersion, reloaded.GetType().Assembly);
        Assert.NotNull(reloaded.GetType().GetField("Range"));
        Assert.Equal(7, reloaded.GetType().GetField("Speed")!.GetValue(reloaded));
        Assert.Equal(component.Id, reloaded.Id);

        undoRedo.Undo();
        var undone = ProbeComponent(fixture);
        Assert.Same(probe.FirstVersion, undone.GetType().Assembly);
        Assert.Same(probe.FirstVersion, loadedAssembly.Assembly);
        Assert.Equal(7, undone.GetType().GetField("Speed")!.GetValue(undone));

        undoRedo.Redo();
        var redone = ProbeComponent(fixture);
        Assert.NotSame(probe.FirstVersion, redone.GetType().Assembly);
        Assert.Equal(7, redone.GetType().GetField("Speed")!.GetValue(redone));
    }

    private static void AddEntity(AssetsSessionFixture fixture, Entity entity)
    {
        var graph = (AssetCompositeHierarchyPropertyGraph<EntityDesign, Entity>)fixture.Asset<SceneViewModel>("MainScene").PropertyGraph;
        using (fixture.Session.UndoRedoService.CreateTransaction())
        {
            var design = new EntityDesign(entity);
            graph.AddPartToAsset(new AssetPartCollection<EntityDesign, Entity> { design }, design, null, 0);
        }
    }

    private static EntityComponent ProbeComponent(AssetsSessionFixture fixture)
    {
        var scene = (SceneAsset)fixture.Asset("MainScene").Asset;
        return scene.Hierarchy.Parts.Values.Single().Entity.Components.Single(x => x.GetType().FullName == ProbeTypeName);
    }

    /// <summary>
    /// Two builds of one game assembly: a component with a field, then the same component with one more field. Each test
    /// gets its own assembly name and data contract alias, so that no other test sees its types.
    /// </summary>
    /// <remarks>
    /// The assembly processor gives a game assembly the binary serializers of its types and a module initializer that
    /// registers them, and the assembly as Engine: the module initializer of a probe calls <see cref="ReloadProbeModule"/>.
    /// </remarks>
    private sealed class ProbeAssemblies : IAsyncDisposable
    {
        private readonly string name = "Stride.ReloadProbe" + Guid.NewGuid().ToString("N");
        private readonly string directory;
        private readonly List<(AssemblyContainer Container, PackageLoadedAssembly Loaded)> loaded = [];

        public ProbeAssemblies()
        {
            directory = Path.Combine(Path.GetTempPath(), name);
            FirstVersionPath = Emit("v1", withRange: false);
            SecondVersionPath = Emit("v2", withRange: true);
        }

        public string FirstVersionPath { get; }

        public string SecondVersionPath { get; }

        public Assembly? FirstVersion { get; private set; }

        /// <summary>
        /// Loads the first build in the session, as a package assembly of its project.
        /// </summary>
        public PackageLoadedAssembly LoadFirstVersion(AssetsSessionFixture fixture)
        {
            var container = fixture.Session.AssemblyContainer;
            FirstVersion = container.LoadAssemblyFromPath(FirstVersionPath) ?? throw new InvalidOperationException("The probe did not load.");
            // Its module initializer registered it as Engine; the package load registers it as Assets
            AssemblyRegistry.Register(FirstVersion, AssemblyCommonCategories.Assets);
            var loadedAssembly = new PackageLoadedAssembly(null!, FirstVersionPath) { Assembly = FirstVersion };
            fixture.Package.Package.LoadedAssemblies.Add(loadedAssembly);
            loaded.Add((container, loadedAssembly));
            return loadedAssembly;
        }

        public ValueTask DisposeAsync()
        {
            // Whichever copy is current goes, and the copy the reload took out too
            foreach (var (container, loadedAssembly) in loaded)
            {
                foreach (var assembly in new[] { loadedAssembly.Assembly, FirstVersion }.OfType<Assembly>().Distinct())
                {
                    AssemblyRegistry.Unregister(assembly);
                    DataSerializerFactory.UnregisterSerializationAssembly(assembly);
                    container.RemoveAssembly(assembly);
                }
            }
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch (IOException)
            {
                // Best effort
            }
            return ValueTask.CompletedTask;
        }

        private string Emit(string version, bool withRange)
        {
            var builder = new PersistedAssemblyBuilder(new AssemblyName(name), typeof(object).Assembly);
            var module = builder.DefineDynamicModule(name);
            var type = module.DefineType(ProbeTypeName, TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class, typeof(EntityComponent));
            type.SetCustomAttribute(new CustomAttributeBuilder(typeof(DataContractAttribute).GetConstructor([typeof(string)])!, [name]));
            type.DefineField("Speed", typeof(int), FieldAttributes.Public);
            if (withRange)
                type.DefineField("Range", typeof(float), FieldAttributes.Public);
            type.DefineDefaultConstructor(MethodAttributes.Public);
            type.CreateType();

            var initializer = module.DefineGlobalMethod(".cctor", MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig, typeof(void), Type.EmptyTypes);
            var il = initializer.GetILGenerator();
            il.Emit(OpCodes.Ldtoken, type);
            il.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!);
            il.Emit(OpCodes.Ldstr, name);
            il.Emit(OpCodes.Call, typeof(ReloadProbeModule).GetMethod(nameof(ReloadProbeModule.Initialize))!);
            il.Emit(OpCodes.Ret);
            module.CreateGlobalFunctions();

            var versionDirectory = Path.Combine(directory, version);
            Directory.CreateDirectory(versionDirectory);
            var path = Path.Combine(versionDirectory, name + ".dll");
            builder.Save(path);
            return path;
        }
    }

    /// <summary>
    /// The binary serializer the assembly processor would generate for a probe component: its base, then its fields.
    /// </summary>
    internal sealed class ProbeSerializer<T> : ClassDataSerializer<T> where T : EntityComponent, new()
    {
        private DataSerializer<EntityComponent>? parentSerializer;
        private SerializerSelector? selector;

        public override void Initialize(SerializerSelector serializerSelector)
        {
            selector = serializerSelector;
            parentSerializer = serializerSelector.GetSerializer<EntityComponent>();
        }

        public override void Serialize(ref T obj, ArchiveMode mode, SerializationStream stream)
        {
            EntityComponent component = obj;
            parentSerializer!.Serialize(ref component, mode, stream);
            foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var value = field.GetValue(obj);
                selector!.GetSerializer(field.FieldType)!.Serialize(ref value!, mode, stream);
                if (mode == ArchiveMode.Deserialize)
                    field.SetValue(obj, value);
            }
        }
    }
}

/// <summary>
/// What the module initializer of a processed game assembly does, for the probes of <see cref="GameStudioAssemblyReloaderTests"/>:
/// it registers the binary serializers and the data contract alias of its component, then the assembly as Engine.
/// </summary>
public static class ReloadProbeModule
{
    public static void Initialize(Type componentType, string alias)
    {
        var serializers = new AssemblySerializers(componentType.Assembly);
        serializers.DataContractAliases.Add(new AssemblySerializers.DataContractAlias(alias, componentType, false));
        serializers.Profiles["Default"] =
        [
            new AssemblySerializerEntry(ObjectId.FromBytes(System.Text.Encoding.UTF8.GetBytes(componentType.FullName!)), componentType, typeof(GameStudioAssemblyReloaderTests.ProbeSerializer<>).MakeGenericType(componentType)),
        ];
        DataSerializerFactory.RegisterSerializationAssembly(serializers);
        AssemblyRegistry.Register(componentType.Assembly, AssemblyCommonCategories.Engine);
    }
}
