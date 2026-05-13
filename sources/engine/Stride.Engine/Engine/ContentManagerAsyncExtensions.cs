// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Stride.Core.MicroThreading;

namespace Stride.Core.Serialization.Contents;

/// <summary>
/// Async Load/Reload helpers for <see cref="ContentManager"/> that propagate the calling MicroThread's
/// synchronization context onto the task — required for code paths that rely on MicroThreadLocal values
/// (e.g. selecting the active database).
/// </summary>
public static class ContentManagerAsyncExtensions
{
    /// <summary>
    /// Reloads a previously loaded asset asynchronously.
    /// </summary>
    /// <param name="contentManager">The content manager.</param>
    /// <param name="obj">The object to reload.</param>
    /// <param name="newUrl">The url of the new object to load. This allows to replace an asset by another one, or to handle renamed content.</param>
    /// <param name="settings">The loader settings.</param>
    /// <returns>A task that completes when the content has been reloaded. The result of the task is True if it could be reloaded, false otherwise.</returns>
    /// <exception cref="System.InvalidOperationException">Content not loaded through this ContentManager.</exception>
    public static Task<bool> ReloadAsync(this ContentManager contentManager, object obj, string? newUrl = null, ContentManagerLoaderSettings? settings = null)
    {
        return ScheduleAsync(() => contentManager.Reload(obj, newUrl, settings));
    }

    /// <summary>
    /// Loads an asset from the specified URL asynchronously.
    /// </summary>
    /// <typeparam name="T">The content type.</typeparam>
    /// <param name="contentManager">The content manager.</param>
    /// <param name="url">The URL to load from.</param>
    /// <param name="settings">The settings. If null, fallback to <see cref="ContentManagerLoaderSettings.Default" />.</param>
    /// <remarks>If the asset is already loaded, it just increases the reference count of the asset and return the same instance.</remarks>
    /// <returns>The loaded content.</returns>
    public static Task<T> LoadAsync<T>(this IContentManager contentManager, string url, ContentManagerLoaderSettings? settings = null) where T : class
    {
        return ScheduleAsync(() => contentManager.Load<T>(url, settings));
    }

    /// <summary>
    /// Loads content from the specified <see cref="UrlReference{T}"/> asynchronously.
    /// </summary>
    /// <typeparam name="T">The content type.</typeparam>
    /// <param name="content">The <see cref="IContentManager"/>.</param>
    /// <param name="urlReference">The URL to load from.</param>
    /// <param name="settings">The settings. If null, fallback to <see cref="ContentManagerLoaderSettings.Default"/>.</param>
    /// <returns>The loaded content.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="urlReference"/> is <c>null</c> or <c>empty</c>. Or <paramref name="content"/> is <c>null</c>.</exception>
    public static Task<T> LoadAsync<T>(this IContentManager content, UrlReference<T> urlReference, ContentManagerLoaderSettings? settings = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(content);
        if (urlReference?.IsEmpty != false)
            throw new ArgumentNullException(nameof(urlReference));

        return content.LoadAsync<T>(urlReference.Url, settings);
    }

    /// <summary>
    /// Loads an asset from the specified URL asynchronously.
    /// </summary>
    /// <param name="contentManager">The content manager.</param>
    /// <param name="type">The type.</param>
    /// <param name="url">The URL.</param>
    /// <param name="settings">The settings.</param>
    /// <remarks>If the asset is already loaded, it just increases the reference count of the asset and return the same instance.</remarks>
    /// <returns>The loaded content.</returns>
    public static Task<object> LoadAsync(this ContentManager contentManager, Type type, string url, ContentManagerLoaderSettings? settings = null)
    {
        return ScheduleAsync(() => contentManager.Load(type, url, settings));
    }

    private static Task<T> ScheduleAsync<T>(Func<T> action)
    {
        var microThread = Scheduler.CurrentMicroThread;
        return Task.Factory.StartNew(() =>
        {
            var initialContext = SynchronizationContext.Current;
            // This synchronization context gives access to any MicroThreadLocal values. The database to use might actually be micro thread local.
            SynchronizationContext.SetSynchronizationContext(new MicrothreadProxySynchronizationContext(microThread));
            var result = action();
            SynchronizationContext.SetSynchronizationContext(initialContext);
            return result;
        });
    }
}
