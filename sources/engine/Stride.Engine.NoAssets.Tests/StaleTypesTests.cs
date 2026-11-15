// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using Stride.Core;
using Stride.Core.Reflection;
using Stride.Engine.Design;
using Stride.Engine.FlexibleProcessing;
using Stride.Games;
using Xunit;

namespace Stride.Engine.Tests;

[CollectionDefinition(nameof(StaleTypesTests), DisableParallelization = true)]
public class StaleTypesCollection;

/// <summary>
/// Tests what a game drops of the types of an assembly the editor unloaded or replaced by a new copy:
/// <see cref="Game.ReconcileDeclaredGameSystems"/> and <see cref="EntityManager.RemoveStaleTypes"/>.
/// </summary>
/// <remarks>
/// Not run in parallel: the tests register assemblies in <see cref="AssemblyRegistry"/>, which every game reads.
/// </remarks>
[Collection(nameof(StaleTypesTests))]
public class StaleTypesTests
{
    [Fact]
    public void ReconcileReplacesTheDeclaredSystemOfAReplacedAssembly()
    {
        var oldAssembly = DeclareGameSystem(typeof(OldSystem));
        Assembly newAssembly = null;
        try
        {
            using var game = new Game();
            var oldSystem = game.Services.GetService<OldSystem>();
            Assert.NotNull(oldSystem);
            Assert.False(game.ReconcileDeclaredGameSystems());

            // What a reload does: the old copy goes, the new one comes
            AssemblyRegistry.Unregister(oldAssembly);
            newAssembly = DeclareGameSystem(typeof(NewSystem));

            Assert.True(game.ReconcileDeclaredGameSystems());
            Assert.Null(game.Services.GetService<OldSystem>());
            Assert.True(oldSystem.IsDisposed);
            // An update already going over the systems skips it
            Assert.False(oldSystem.Enabled);
            Assert.False(oldSystem.Visible);
            Assert.NotNull(game.Services.GetService<NewSystem>());

            Assert.False(game.ReconcileDeclaredGameSystems());
        }
        finally
        {
            AssemblyRegistry.Unregister(oldAssembly);
            if (newAssembly != null)
                AssemblyRegistry.Unregister(newAssembly);
        }
    }

    [Fact]
    public void RemoveStaleTypesRemovesTheProcessorOfAReplacedComponent()
    {
        using var game = new Game();
        var entity = new Entity { new OldComponent() };
        var scene = new SceneInstance(game.Services, new Scene { Entities = { entity } });
        var oldProcessor = scene.GetProcessor<OldProcessor>();
        Assert.NotNull(oldProcessor);

        // What the editor does when the component's assembly is replaced: a new component takes the old one's place
        entity.Components[1] = new NewComponent();
        Assert.Same(oldProcessor, scene.GetProcessor<OldProcessor>());

        Assert.True(scene.RemoveStaleTypes(IsOld));
        Assert.Null(scene.GetProcessor<OldProcessor>());
        Assert.NotNull(scene.GetProcessor<NewProcessor>());
        Assert.DoesNotContain(typeof(OldComponent), scene.ComponentTypes);

        Assert.False(scene.RemoveStaleTypes(IsOld));
    }

    [Fact]
    public void RemoveStaleTypesKeepsAStaleComponentStillOnAnEntity()
    {
        using var game = new Game();
        var entity = new Entity { new OldComponent() };
        var scene = new SceneInstance(game.Services, new Scene { Entities = { entity } });
        var oldProcessor = scene.GetProcessor<OldProcessor>();

        // The reconcile can run before the component is replaced
        Assert.True(scene.RemoveStaleTypes(IsOld));
        Assert.Null(scene.GetProcessor<OldProcessor>());
        Assert.Equal(1, oldProcessor.RemovedCount);
        Assert.Contains(typeof(OldComponent), scene.ComponentTypes);

        entity.Components[1] = new NewComponent();
        Assert.Equal(1, scene.GetProcessor<NewProcessor>().AddedCount);
        Assert.Equal(1, oldProcessor.RemovedCount);

        Assert.True(scene.RemoveStaleTypes(IsOld));
        Assert.DoesNotContain(typeof(OldComponent), scene.ComponentTypes);
    }

    [SkippableFact]
    public void RemoveStaleTypesCreatesTheNewCopyOfAProcessorOfAComponentThatStays()
    {
        TestAssemblyCopy.SkipIfUnavailable();
        using var copy = new TestAssemblyCopy();
        using var game = new Game();
        var scene = new SceneInstance(game.Services, new Scene { Entities = { new Entity { new OldComponent() } } });

        // The processor's assembly is replaced by its copy, the component's type stays
        copy.Register();
        Assert.True(scene.RemoveStaleTypes(x => x == typeof(OldProcessor)));

        // The copy's processor is for the copy's component type, so it takes no component here: in a real case, the
        // component's assembly is not reloaded and both copies of the processor are for its type
        Assert.Null(scene.GetProcessor<OldProcessor>());
        var newProcessor = Assert.Single(scene.Processors, x => x.GetType().FullName == typeof(OldProcessor).FullName);
        Assert.Same(copy.Assembly, newProcessor.GetType().Assembly);

        Assert.False(scene.RemoveStaleTypes(x => x == typeof(OldProcessor)));
    }

    [SkippableFact]
    public void FlexibleProcessorOfANewCopyRunsNextToTheOldOne()
    {
        TestAssemblyCopy.SkipIfUnavailable();
        FlexibleProcessor.Added = FlexibleProcessor.Removed = 0;
        using var copy = new TestAssemblyCopy();
        using var game = new Game();
        var first = new Entity { new FlexibleComponent() };
        var second = new Entity { new FlexibleComponent() };
        var scene = new SceneInstance(game.Services, new Scene { Entities = { first, second } });
        Assert.Equal(1, FlexibleProcessor.Added);

        // Components are replaced one entity at a time: for a while, both copies of the processor run
        copy.Register();
        var newComponentType = copy.Assembly.GetType(typeof(FlexibleComponent).FullName!, throwOnError: true)!;
        first.Components[1] = (EntityComponent)Activator.CreateInstance(newComponentType)!;
        second.Components[1] = (EntityComponent)Activator.CreateInstance(newComponentType)!;

        Assert.Equal(1, FlexibleProcessor.Removed);
        Assert.True(scene.RemoveStaleTypes(x => x.Assembly == typeof(FlexibleComponent).Assembly));
        Assert.False(scene.RemoveStaleTypes(x => x.Assembly == typeof(FlexibleComponent).Assembly));
    }

    private static bool IsOld(Type type) => type == typeof(OldComponent) || type == typeof(OldProcessor);

    // A dynamic assembly whose scan index declares the system, as the assembly processor writes it
    private static Assembly DeclareGameSystem(Type systemType)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Stride.StaleTypesTests" + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.Run);
        AssemblyRegistry.RegisterScanTypes(assembly, new AssemblyRegistry.ScanTypes(new Dictionary<Type, List<Type>> { [typeof(GameSystemAttribute)] = [systemType] }));
        AssemblyRegistry.Register(assembly, "StaleTypesTests");
        return assembly;
    }

    /// <summary>
    /// A second copy of this test assembly, in a load context of its own: its types have the same names as this
    /// assembly's, as after a reload. Registered, it takes the name lookups of <see cref="AssemblyRegistry.GetType"/>.
    /// </summary>
    private sealed class TestAssemblyCopy : IDisposable
    {
        private readonly AssemblyLoadContext context = new(nameof(StaleTypesTests), isCollectible: true);
        private readonly Assembly original = typeof(StaleTypesTests).Assembly;
        private readonly IReadOnlyCollection<string> originalCategories;

        public TestAssemblyCopy()
        {
            Assembly = context.LoadFromAssemblyPath(original.Location);
            originalCategories = AssemblyRegistry.GetCategories(original);
        }

        public Assembly Assembly { get; }

        /// <summary>
        /// The copy is loaded from this assembly's file, which some hosts don't have (Android loads it from the APK:
        /// its location is only a file name).
        /// </summary>
        public static void SkipIfUnavailable()
            => Skip.IfNot(Path.IsPathRooted(typeof(StaleTypesTests).Assembly.Location), "The test assembly isn't loaded from a file");

        public void Register() => AssemblyRegistry.Register(Assembly, "StaleTypesTests");

        public void Dispose()
        {
            AssemblyRegistry.Unregister(Assembly);
            // Takes the name lookups back
            if (originalCategories.Count > 0)
            {
                AssemblyRegistry.Register(original, originalCategories);
            }
            else
            {
                AssemblyRegistry.Register(original, "StaleTypesTests");
                AssemblyRegistry.Unregister(original);
            }
            context.Unload();
        }
    }

    public class OldSystem(IServiceRegistry registry) : GameSystemBase(registry);

    public class NewSystem(IServiceRegistry registry) : GameSystemBase(registry);

    [DefaultEntityComponentProcessor(typeof(OldProcessor))]
    public class OldComponent : EntityComponent;

    [DefaultEntityComponentProcessor(typeof(NewProcessor))]
    public class NewComponent : EntityComponent;

    public class OldProcessor : CountingProcessor<OldComponent>;

    public class NewProcessor : CountingProcessor<NewComponent>;

    public abstract class CountingProcessor<T> : EntityProcessor<T> where T : EntityComponent
    {
        public int AddedCount { get; private set; }

        public int RemovedCount { get; private set; }

        protected override void OnEntityComponentAdding(Entity entity, T component, T data) => AddedCount++;

        protected override void OnEntityComponentRemoved(Entity entity, T component, T data) => RemovedCount++;
    }

    public class FlexibleComponent : EntityComponent, IComponent<FlexibleProcessor, FlexibleComponent>;

    public class FlexibleProcessor : IComponent<FlexibleProcessor, FlexibleComponent>.IProcessor, IUpdateProcessor
    {
        // Per copy of the assembly: the test reads the old copy's
        public static int Added;
        public static int Removed;

        public int Order => 0;

        public void SystemAdded(IServiceRegistry registryParam) => Added++;

        public void SystemRemoved() => Removed++;

        public void OnComponentAdded(FlexibleComponent item)
        {
        }

        public void OnComponentRemoved(FlexibleComponent item)
        {
        }

        public void Update(GameTime gameTime)
        {
        }
    }
}
