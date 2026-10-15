// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Stride.Core.Reflection;
using Xunit;

namespace Stride.Core.Tests;

public class AssemblyRegistryTests
{
    [Fact]
    public void AFailingHandlerDoesNotStopTheOthersNorTheRegistration()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Stride.Test.Registered." + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.Run);
        var registered = 0;
        var unregistered = 0;
        EventHandler<AssemblyRegisteredEventArgs> failing = (_, e) =>
        {
            if (e.Assembly == assembly)
                throw new InvalidOperationException("A plugin's handler failing");
        };
        EventHandler<AssemblyRegisteredEventArgs> counting = (_, e) =>
        {
            if (e.Assembly == assembly)
                registered++;
        };
        EventHandler<AssemblyRegisteredEventArgs> countingUnregistered = (_, e) =>
        {
            if (e.Assembly == assembly)
                unregistered++;
        };

        AssemblyRegistry.AssemblyRegistered += failing;
        AssemblyRegistry.AssemblyRegistered += counting;
        AssemblyRegistry.AssemblyUnregistered += failing;
        AssemblyRegistry.AssemblyUnregistered += countingUnregistered;
        try
        {
            AssemblyRegistry.Register(assembly, "Test");
            Assert.Equal(1, registered);
            Assert.Contains(assembly, AssemblyRegistry.Find("Test"));

            AssemblyRegistry.Unregister(assembly);
            Assert.Equal(1, unregistered);
            Assert.DoesNotContain(assembly, AssemblyRegistry.Find("Test"));
        }
        finally
        {
            AssemblyRegistry.AssemblyRegistered -= failing;
            AssemblyRegistry.AssemblyRegistered -= counting;
            AssemblyRegistry.AssemblyUnregistered -= failing;
            AssemblyRegistry.AssemblyUnregistered -= countingUnregistered;
            AssemblyRegistry.Unregister(assembly);
        }
    }
}
