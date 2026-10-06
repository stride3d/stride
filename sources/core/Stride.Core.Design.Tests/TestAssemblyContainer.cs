// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Reflection;
using Xunit;

namespace Stride.Core.Design.Tests;

public class TestAssemblyContainer
{
    [Fact]
    public void HostAssemblyIsFound()
    {
        Assert.Same(typeof(AssemblyContainer).Assembly, AssemblyContainer.TryLoadHostAssembly("Stride.Core.Design"));
    }

    [Fact]
    public void MissingHostAssemblyIsAskedOnce()
    {
        var name = "Stride.Missing" + Guid.NewGuid().ToString("N");
        var notFound = 0;
        void OnFirstChance(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            if (e.Exception is FileNotFoundException && e.Exception.Message.Contains(name, StringComparison.Ordinal))
                Interlocked.Increment(ref notFound);
        }

        AppDomain.CurrentDomain.FirstChanceException += OnFirstChance;
        try
        {
            Assert.Null(AssemblyContainer.TryLoadHostAssembly(name));
            Assert.Null(AssemblyContainer.TryLoadHostAssembly(name));
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= OnFirstChance;
        }

        Assert.Equal(1, notFound);
    }

    [Fact]
    public void TypeNameOfAContainerAssemblyResolvesFromOutside()
    {
        // An assembly only a container loads (from bytes, in a load context of its own)
        var assemblyName = "Stride.ContainerProbe" + Guid.NewGuid().ToString("N");
        var directory = Path.Combine(Path.GetTempPath(), assemblyName);
        Directory.CreateDirectory(directory);
        try
        {
            var builder = new System.Reflection.Emit.PersistedAssemblyBuilder(new System.Reflection.AssemblyName(assemblyName), typeof(object).Assembly);
            builder.DefineDynamicModule(assemblyName).DefineType("Probe", System.Reflection.TypeAttributes.Public).CreateType();
            var path = Path.Combine(directory, assemblyName + ".dll");
            builder.Save(path);

            var container = new AssemblyContainer();
            var assembly = container.LoadAssemblyFromPath(path);
            Assert.NotNull(assembly);

            // A generic argument naming it binds through the default context (as WPF reads a BAML type), which only
            // the container knows how to answer
            var type = Type.GetType($"System.Collections.Generic.List`1[[Probe, {assemblyName}]]", throwOnError: false);
            Assert.NotNull(type);
            Assert.Same(assembly, type.GetGenericArguments()[0].Assembly);

            container.UnloadAssembly(assembly);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
