// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using System.Reflection.Emit;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Reflection;
using Stride.Core.Serialization;
using Stride.Core.Storage;
using Stride.Engine;

namespace Stride.Assets.Editor.Tests.Helpers;

/// <summary>
/// Two builds of one game assembly: a component with a field, then the same component with one more field. Each test
/// gets its own assembly name and data contract alias, so that no other test sees its types.
/// </summary>
/// <remarks>
/// The assembly processor gives a game assembly the binary serializers of its types and a module initializer that
/// registers them, and the assembly as Engine: the module initializer of a probe calls <see cref="ReloadProbeModule"/>.
/// </remarks>
internal sealed class ProbeAssemblies : IAsyncDisposable
{
    /// <summary>
    /// The full name of the probe component.
    /// </summary>
    public const string TypeName = "ReloadProbe.ProbeComponent";

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
    /// The project file the probe builds from, when it is loaded with one.
    /// </summary>
    public string ProjectPath => Path.Combine(directory, name + ".csproj");

    /// <summary>
    /// Loads the first build in the session, as a package assembly of its project.
    /// </summary>
    /// <param name="fixture">The session.</param>
    /// <param name="withProject">Whether the package references the project of the probe, at <see cref="ProjectPath"/>.</param>
    public PackageLoadedAssembly LoadFirstVersion(AssetsSessionFixture fixture, bool withProject = false)
    {
        var container = fixture.Session.AssemblyContainer;
        FirstVersion = container.LoadAssemblyFromPath(FirstVersionPath) ?? throw new InvalidOperationException("The probe did not load.");
        // Its module initializer registered it as Engine; the package load registers it as Assets
        AssemblyRegistry.Register(FirstVersion, AssemblyCommonCategories.Assets);
        var loadedAssembly = new PackageLoadedAssembly(withProject ? new ProjectReference(Guid.NewGuid(), ProjectPath, ProjectType.Library) : null!, FirstVersionPath) { Assembly = FirstVersion };
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
        var type = module.DefineType(TypeName, TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class, typeof(EntityComponent));
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
public sealed class ProbeSerializer<T> : ClassDataSerializer<T> where T : EntityComponent, new()
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

/// <summary>
/// What the module initializer of a processed game assembly does, for the probes of <see cref="ProbeAssemblies"/>:
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
            new AssemblySerializerEntry(ObjectId.FromBytes(System.Text.Encoding.UTF8.GetBytes(componentType.FullName!)), componentType, typeof(ProbeSerializer<>).MakeGenericType(componentType)),
        ];
        DataSerializerFactory.RegisterSerializationAssembly(serializers);
        AssemblyRegistry.Register(componentType.Assembly, AssemblyCommonCategories.Engine);
    }
}
