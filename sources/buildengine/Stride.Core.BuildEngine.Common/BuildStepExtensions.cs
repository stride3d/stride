// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Extensions;

namespace Stride.Core.BuildEngine;

/// <summary>
/// This class contains extension methods related to the <see cref="BuildStep"/> class.
/// </summary>
public static class BuildStepExtensions
{
    /// <summary>
    /// Enumerates this build step, and all its inner build steps recursively when they are themselves <see cref="ListBuildStep"/>.
    /// </summary>
    /// <param name="buildStep">The build step to enumerates with its inner build steps</param>
    /// <returns>An <see cref="IEnumerable{BuildStep}"/> object enumerating this build step, and all its inner build steps recursively.</returns>
    public static IEnumerable<BuildStep> EnumerateRecursively(this BuildStep buildStep)
    {
        if (buildStep is not ListBuildStep enumerableBuildStep)
            return buildStep.Yield()!;

        return enumerableBuildStep.Steps.SelectDeep(static x => x is ListBuildStep step && step.Steps != null ? step.Steps : []);
    }

    public static void Print(this BuildStep buildStep, TextWriter? stream = null)
    {
        PrintRecursively(buildStep, 0, stream ?? Console.Out);
    }

    private static void PrintRecursively(this BuildStep buildStep, int offset, TextWriter stream)
    {
        stream.WriteLine("".PadLeft(offset) + buildStep);
        if (buildStep is ListBuildStep enumerable)
        {
            foreach (var child in enumerable.Steps)
                PrintRecursively(child, offset + 2, stream);
        }
    }
}
