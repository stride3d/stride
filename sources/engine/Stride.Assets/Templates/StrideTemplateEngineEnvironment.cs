// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;

namespace Stride.Assets.Templates;

/// <summary>
/// <see cref="IEnvironment"/> that delegates to <see cref="Environment"/> for most queries but
/// returns a Stride-specific path for <c>USERPROFILE</c> / <c>HOME</c>. The bootstrapper's
/// <see cref="DefaultPathInfo"/> resolves <c>GlobalSettingsDir</c> as
/// <c>{UserProfile}/.templateengine/</c>, so redirecting the env var moves all persisted state
/// (<c>packages.json</c>, the downloaded <c>packages/</c> dir, host settings) under our own
/// folder — keeping <c>dotnet new</c>'s global state and ours completely isolated.
/// </summary>
internal sealed class StrideTemplateEngineEnvironment : IEnvironment
{
    private readonly string profileDir;

    /// <summary>
    /// <paramref name="profileDir"/> becomes the value returned for USERPROFILE / HOME lookups
    /// (and ExpandEnvironmentVariables substitutions). Typically a path like
    /// <c>%LocalAppData%\stride\template-engine\profile</c> — the bootstrapper then derives its
    /// <c>{profileDir}/.templateengine/</c> tree from there.
    /// </summary>
    public StrideTemplateEngineEnvironment(string profileDir)
    {
        this.profileDir = profileDir;
    }

    public string NewLine => Environment.NewLine;

    // Match Console.BufferWidth's fallback when no console is attached (WPF host case).
    public int ConsoleBufferWidth => 80;

    public string ExpandEnvironmentVariables(string name) =>
        Environment.ExpandEnvironmentVariables(name ?? string.Empty)
                   .Replace("%USERPROFILE%", profileDir, StringComparison.OrdinalIgnoreCase);

    public string? GetEnvironmentVariable(string name) => name switch
    {
        "USERPROFILE" => profileDir,
        "HOME"        => profileDir,
        _             => Environment.GetEnvironmentVariable(name),
    };

    public IReadOnlyDictionary<string, string> GetEnvironmentVariables()
    {
        var dict = Environment.GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .ToDictionary(e => (string)e.Key, e => (string)(e.Value ?? string.Empty), StringComparer.OrdinalIgnoreCase);
        dict["USERPROFILE"] = profileDir;
        dict["HOME"] = profileDir;
        return dict;
    }
}
