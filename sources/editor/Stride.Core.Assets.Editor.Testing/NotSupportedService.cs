// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;

namespace Stride.Core.Assets.Editor.Testing;

/// <summary>
/// A stand-in for a service that a test doesn't use: a call that returns nothing (such as showing the progress of the loading of the
/// session) does nothing, and any other call throws a <see cref="NotSupportedException"/>.
/// </summary>
/// <typeparam name="T">The interface of the service.</typeparam>
/// <remarks>A test that needs the service registers a fake of its own instead.</remarks>
public class NotSupportedService<T> : DispatchProxy where T : class
{
    /// <summary>
    /// Creates a stand-in for the service.
    /// </summary>
    public static T Create() => Create<T, NotSupportedService<T>>();

    /// <inheritdoc/>
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        => targetMethod?.ReturnType == typeof(void) ? null : throw new NotSupportedException($"{typeof(T).Name}.{targetMethod?.Name} is not supported in this test.");
}
