// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Stride.Core.Diagnostics;

namespace Stride.GameStudio.Mcp;

/// <summary>
/// JSON data model for <c>.stride/mcp.json</c> at the solution root.
/// Serves as both user configuration and runtime discovery for AI agents.
/// </summary>
public sealed class McpConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("port")]
    public int Port { get; set; }

    [JsonPropertyName("runtime")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public McpRuntimeInfo? Runtime { get; set; }
}

/// <summary>
/// Runtime information written by the MCP server on start, cleared on stop.
/// Allows AI agents to discover the actual port and check PID liveness.
/// </summary>
public sealed class McpRuntimeInfo
{
    [JsonPropertyName("actualPort")]
    public int ActualPort { get; set; }

    [JsonPropertyName("pid")]
    public int Pid { get; set; }

    [JsonPropertyName("startedAt")]
    public DateTimeOffset StartedAt { get; set; }
}

/// <summary>
/// File I/O helpers for reading and writing <c>.stride/mcp.json</c>.
/// All I/O is wrapped in try-catch so file issues never prevent the editor from starting.
/// </summary>
public static class McpConfigFile
{
    private static readonly Logger Log = GlobalLogger.GetLogger("McpConfig");
    private const string DirectoryName = ".stride";
    private const string FileName = "mcp.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    /// <summary>
    /// Returns the full path to <c>.stride/mcp.json</c> for the given solution directory.
    /// </summary>
    public static string GetConfigPath(string solutionDir)
        => Path.Combine(solutionDir, DirectoryName, FileName);

    /// <summary>
    /// Reads and deserializes the config file. Returns defaults if missing or malformed.
    /// </summary>
    public static McpConfig Load(string? solutionDir)
    {
        if (string.IsNullOrEmpty(solutionDir))
            return new McpConfig();

        try
        {
            var path = GetConfigPath(solutionDir);
            if (!File.Exists(path))
                return new McpConfig();

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<McpConfig>(json, JsonOptions) ?? new McpConfig();
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to read MCP config: {ex.Message}");
            return new McpConfig();
        }
    }

    /// <summary>
    /// Serializes and writes the config file, creating the <c>.stride/</c> directory if needed.
    /// </summary>
    public static void Save(string? solutionDir, McpConfig config)
    {
        if (string.IsNullOrEmpty(solutionDir))
            return;

        try
        {
            var dirPath = Path.Combine(solutionDir, DirectoryName);
            Directory.CreateDirectory(dirPath);

            var path = Path.Combine(dirPath, FileName);
            var json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to write MCP config: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads the config, sets runtime info (actual port, PID, timestamp), and saves.
    /// </summary>
    public static void WriteRuntimeInfo(string? solutionDir, int port, int pid)
    {
        if (string.IsNullOrEmpty(solutionDir))
            return;

        try
        {
            var config = Load(solutionDir);
            config.Runtime = new McpRuntimeInfo
            {
                ActualPort = port,
                Pid = pid,
                StartedAt = DateTimeOffset.UtcNow,
            };
            Save(solutionDir, config);
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to write MCP runtime info: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads the config, clears runtime info, and saves.
    /// </summary>
    public static void ClearRuntimeInfo(string? solutionDir)
    {
        if (string.IsNullOrEmpty(solutionDir))
            return;

        try
        {
            var config = Load(solutionDir);
            config.Runtime = null;
            Save(solutionDir, config);
        }
        catch (Exception ex)
        {
            Log.Warning($"Failed to clear MCP runtime info: {ex.Message}");
        }
    }
}
