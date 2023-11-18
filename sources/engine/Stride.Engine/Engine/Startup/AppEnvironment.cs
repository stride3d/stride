// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Extensions.FileProviders;

namespace Stride.Engine.Startup;

public interface IAppEnvironment
{
    string EnvironmentName { get; set; }

    string ApplicationName { get; set; }

    string ContentRootPath { get; set; }

    IFileProvider ContentRootFileProvider { get; set; }
}

internal class AppEnvironment : IAppEnvironment
{
    // This was just a string, instead of en enum.
    // I guess it allows users to add their own environments if necessary.
    public string EnvironmentName { get; set; } = string.Empty;

    public string ApplicationName { get; set; } = string.Empty;

    public string ContentRootPath { get; set; } = string.Empty;

    public IFileProvider ContentRootFileProvider { get; set; } = null!;
}

public static class Environments
{
    public static readonly string Development = "Development";
    public static readonly string Staging = "Staging";
    public static readonly string Production = "Production";
}

public static class AppEnvironmentExtensions
{
    // This extension method is the standard way of checking
    // for debug mode during startup
    public static bool IsDevelopment(this IAppEnvironment appEnvironment) =>
        appEnvironment.EnvironmentName == Environments.Development;
}
