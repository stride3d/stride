// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Stride.BepuPhysics.Soft.Tests
{
    /// <summary> A bench to run by hand: reported as skipped unless its environment variable is set to 1 </summary>
    public sealed class BenchFactAttribute : FactAttribute
    {
        public BenchFactAttribute(string environmentVariable)
        {
            if (System.Environment.GetEnvironmentVariable(environmentVariable) != "1")
                Skip = $"Bench, run by hand with {environmentVariable}=1";
        }
    }
}
