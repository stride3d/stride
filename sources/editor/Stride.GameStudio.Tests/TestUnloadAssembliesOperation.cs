// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Stride.Assets.Presentation.AssemblyReloading;
using Stride.Core.Assets;
using Stride.Core.Reflection;
using Stride.Core.Transactions;
using Xunit;

namespace Stride.GameStudio.Tests
{
    public class TestUnloadAssembliesOperation
    {
        [Fact]
        public void UndoLoadsTheUnloadedCopyAgain()
        {
            // The undo puts back instances of the unloaded copy's types: a new copy from the file has other types,
            // with no serializers registered for these
            var assemblyName = "Stride.UnloadProbe" + Guid.NewGuid().ToString("N");
            var directory = Path.Combine(Path.GetTempPath(), assemblyName);
            Directory.CreateDirectory(directory);
            try
            {
                var builder = new PersistedAssemblyBuilder(new AssemblyName(assemblyName), typeof(object).Assembly);
                builder.DefineDynamicModule(assemblyName).DefineType("Probe", TypeAttributes.Public).CreateType();
                var path = Path.Combine(directory, assemblyName + ".dll");
                builder.Save(path);

                var container = new AssemblyContainer();
                var assembly = container.LoadAssemblyFromPath(path);
                Assert.NotNull(assembly);
                var package = new Package();
                var loadedAssembly = new PackageLoadedAssembly(null, path) { Assembly = assembly };
                package.LoadedAssemblies.Add(loadedAssembly);

                var stack = TransactionStackFactory.Create(10);
                using (stack.CreateTransaction())
                {
                    var operation = new UnloadAssembliesOperation(container, package, loadedAssembly, []);
                    operation.Execute(null);
                    stack.PushOperation(operation);
                }
                Assert.Null(loadedAssembly.Assembly);
                Assert.DoesNotContain(loadedAssembly, package.LoadedAssemblies);

                stack.Rollback();
                Assert.Same(assembly, loadedAssembly.Assembly);
                Assert.Contains(container.LoadedAssemblies, x => x.Assembly == assembly);
                Assert.Contains(loadedAssembly, package.LoadedAssemblies);

                stack.Rollforward();
                Assert.Null(loadedAssembly.Assembly);
                Assert.DoesNotContain(container.LoadedAssemblies, x => x.Assembly == assembly);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
