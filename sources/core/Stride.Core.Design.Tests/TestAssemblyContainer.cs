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
}
