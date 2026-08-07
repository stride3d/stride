// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Management;
using System.Text;
using Stride.Core.Extensions;
using System.Runtime.InteropServices;

namespace Stride.Core.Windows;

public static class AppHelper
{
    public static string[] GetCommandLineArgs()
    {
        return Environment.GetCommandLineArgs().Skip(1).ToArray();
    }

    public static string BuildErrorMessage(Exception exception, string? header = null)
    {
        var body = new StringBuilder();

        if (header != null)
        {
            body.Append(header);
        }
        body.AppendLine($"Current Directory: {Environment.CurrentDirectory}");
        body.AppendLine($"Command Line Args: {string.Join(" ", GetCommandLineArgs())}");
        body.AppendLine($"OS Version: {RuntimeInformation.OSDescription} ({(Environment.Is64BitOperatingSystem ? "x64" : "x86")})");
        body.AppendLine($"Processor Count: {Environment.ProcessorCount}");
        body.AppendLine("Video configuration:");
        WriteVideoConfig(body);
        body.AppendLine($"Exception: {exception.FormatFull()}");
        return body.ToString();
    }

    /// <summary>
    /// Only fields useful for diagnosing graphics issues; the full Win32_VideoController dump
    /// leaks machine-identifying values such as SystemName and PNPDeviceID.
    /// </summary>
    private const string VideoControllerQuery =
        "SELECT Name,AdapterCompatibility,DriverVersion,DriverDate," +
        "CurrentHorizontalResolution,CurrentVerticalResolution,CurrentBitsPerPixel,CurrentRefreshRate " +
        "FROM Win32_VideoController";

    public static void WriteVideoConfig(StringBuilder writer)
    {
        try
        {
            var i = 0;
            foreach (var properties in QueryVideoControllers())
            {
                writer.AppendLine($"GPU {++i}");
                foreach (var (name, value) in properties)
                {
                    writer.AppendLine($"  {name}: {value}");
                }
            }
        }
        catch (Exception)
        {
            writer.AppendLine("An error occurred while trying to retrieve video configuration.");
        }
    }

    public static Dictionary<string, string> GetVideoConfig()
    {
        var result = new Dictionary<string, string>();
        try
        {
            var deviceId = 0;
            foreach (var properties in QueryVideoControllers())
            {
                foreach (var (name, value) in properties)
                {
                    result.Add($"GPU{deviceId}.{name}", value);
                }
                deviceId++;
            }
        }
        catch (Exception)
        {
            // ignored
        }

        return result;
    }

    private static IEnumerable<List<(string Name, string Value)>> QueryVideoControllers()
    {
        if (!OperatingSystem.IsWindows())
            yield break;

        var searcher = new ManagementObjectSearcher(VideoControllerQuery);
        foreach (var managementObject in searcher.Get().OfType<ManagementObject>())
        {
            var properties = new List<(string Name, string Value)>();

            AddProperty(properties, managementObject, "Name");
            AddProperty(properties, managementObject, "AdapterCompatibility");
            AddProperty(properties, managementObject, "DriverVersion");

            if (managementObject.GetPropertyValue("DriverDate") is string driverDate)
            {
                try
                {
                    driverDate = ManagementDateTimeConverter.ToDateTime(driverDate).ToString("yyyy-MM-dd");
                }
                catch (Exception)
                {
                    // Keep the raw DMTF string
                }
                properties.Add(("DriverDate", driverDate));
            }

            if (managementObject.GetPropertyValue("CurrentHorizontalResolution") is uint width
                && managementObject.GetPropertyValue("CurrentVerticalResolution") is uint height)
            {
                var mode = $"{width} x {height}";
                if (managementObject.GetPropertyValue("CurrentBitsPerPixel") is uint bitsPerPixel)
                    mode += $", {bitsPerPixel} bpp";
                if (managementObject.GetPropertyValue("CurrentRefreshRate") is uint refreshRate)
                    mode += $", {refreshRate} Hz";
                properties.Add(("DisplayMode", mode));
            }

            yield return properties;
        }
    }

    private static void AddProperty(List<(string Name, string Value)> properties, ManagementObject managementObject, string name)
    {
        if (managementObject.GetPropertyValue(name) is { } value)
            properties.Add((name, value.ToString()));
    }
}
