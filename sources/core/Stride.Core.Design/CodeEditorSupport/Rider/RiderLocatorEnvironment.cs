// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text.Json;
using JetBrains.Rider.PathLocator;

namespace Stride.Core.CodeEditorSupport.Rider;

internal sealed class RiderLocatorEnvironment : IRiderLocatorEnvironment
{
    public T? FromJson<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json);
    }

    public void Info(string message, Exception e = null)
    {
       
    }

    public void Warn(string message, Exception e = null)
    {
    }

    public void Error(string message, Exception e = null)
    {
    }

    public void Verbose(string message, Exception e = null)
    {
    }

    public OS CurrentOS =>
        OperatingSystem.IsWindows() ? OS.Windows :
        OperatingSystem.IsLinux() ? OS.Linux :
        OperatingSystem.IsMacOS() ? OS.MacOSX : OS.Other;
}
