// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// A map of contributions of a plugin, removed when the plugin leaves. Registering the value a key already has does
/// nothing; registering another value for it throws, as <see cref="IDictionary{TKey,TValue}.Add(TKey,TValue)"/> does.
/// </summary>
/// <typeparam name="TKey">The type of the keys.</typeparam>
/// <typeparam name="TValue">The type of the values.</typeparam>
public interface IPluginMap<in TKey, in TValue>
    where TKey : notnull
    where TValue : notnull
{
    /// <summary>
    /// Maps <paramref name="key"/> to <paramref name="value"/> in the name of the plugin.
    /// </summary>
    void Register(TKey key, TValue value);
}
