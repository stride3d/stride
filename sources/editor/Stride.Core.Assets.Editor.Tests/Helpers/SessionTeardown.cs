// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.Core.Assets.Editor.Tests.Helpers;

/// <summary>
/// The end of the session of a session fixture.
/// </summary>
public static class SessionTeardown
{
    /// <summary>
    /// Disposes the build services, then the session services and the session, then waits for the background work of the session.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="buildServices">The build services (the database, then the builder), disposed outside the dispatcher: the builder
    /// waits for its thread, which can dispatch work. The object database they opened is closed after them.</param>
    /// <param name="sessionServices">The services of the session, disposed in the dispatcher before the session is destroyed there.</param>
    /// <exception cref="TimeoutException">The background work of the session did not end within 30 seconds.</exception>
    public static async Task DestroyAsync(SessionViewModel session, IDisposable?[]? buildServices = null, IDisposable?[]? sessionServices = null)
    {
        if (buildServices?.Any(x => x is not null) == true)
        {
            await Task.Run(() =>
            {
                foreach (var service in buildServices)
                    service?.Dispose();
                // The object database is process-wide (the editor opens one session per process); the next fixture opens its own
                Stride.Core.BuildEngine.Builder.CloseObjectDatabase();
            });
        }
        session.Dispatcher.Invoke(() =>
        {
            foreach (var service in sessionServices ?? [])
                service?.Dispose();
            session.Destroy();
        });
        // Nothing the test started may outlive it; the wait lets the dispatched work run
        try
        {
            await session.BackgroundWork.WaitAsync(TimeSpan.FromSeconds(30));
        }
        catch (TimeoutException)
        {
            throw new TimeoutException("The background work of the session did not end within 30 seconds of its destruction.");
        }
    }
}
