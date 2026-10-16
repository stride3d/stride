// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;
using Stride.Core;
using Stride.Core.Reflection;
using Stride.Engine.FlexibleProcessing;
using Stride.Games;
using Xunit;

namespace Stride.Engine.Tests;

[CollectionDefinition(nameof(StaleTypesTests), DisableParallelization = true)]
public class StaleTypesCollection;

/// <summary>
/// Tests a game running with the types of an assembly the editor replaced by a new copy.
/// </summary>
/// <remarks>
/// Not run in parallel: the tests register assemblies in <see cref="AssemblyRegistry"/>, which every game reads.
/// </remarks>
[Collection(nameof(StaleTypesTests))]
public class StaleTypesTests
{
    [Fact]
    public void FlexibleProcessorOfANewCopyRunsNextToTheOldOne()
    {
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
