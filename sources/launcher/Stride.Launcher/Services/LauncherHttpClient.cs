// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Launcher.Services;

/// <summary>
/// The HTTP client shared by the requests of the launcher (release notes, news, documentation, setup download).
/// </summary>
internal static class LauncherHttpClient
{
    public static HttpClient Instance { get; } = Create();

    private static HttpClient Create()
    {
        // Connections are renewed now and then, so that DNS changes are picked up
        var client = new HttpClient(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(15) });
        // Identifies the launcher and its version in the logs of the Stride sites
        client.DefaultRequestHeaders.UserAgent.TryParseAdd($"Stride-Launcher/{SelfUpdater.DisplayVersion ?? "unknown"}");
        return client;
    }
}
