using System.Diagnostics;
using System.Text;

namespace Stride.Shaders.Spirv.Tools;

public readonly record struct ValidationResult(bool IsValid, string Output)
{
    public override string ToString() => IsValid ? "Valid" : Output;
}

public static partial class Spv
{
    static string? FindSpirvVal()
    {
        // 1. Check VULKAN_SDK env var
        var vulkanSdk = Environment.GetEnvironmentVariable("VULKAN_SDK");
        if (!string.IsNullOrEmpty(vulkanSdk))
        {
            var path = Path.Combine(vulkanSdk, "Bin", "spirv-val.exe");
            if (File.Exists(path))
                return path;
            // Linux/macOS
            path = Path.Combine(vulkanSdk, "bin", "spirv-val");
            if (File.Exists(path))
                return path;
        }

        // 2. Assume it's on PATH
        return "spirv-val";
    }

    /// <summary>
    /// Validates a SPIR-V file using the spirv-val tool from the Vulkan SDK.
    /// </summary>
    /// <param name="filePath">Path to a .spv file.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating whether the bytecode is valid.</returns>
    public static ValidationResult ValidateFile(string filePath)
    {
        var exe = FindSpirvVal();

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = filePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        process.Start();

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        var output = new StringBuilder();
        if (stdout.Length > 0)
            output.Append(stdout);
        if (stderr.Length > 0)
            output.Append(stderr);

        return new ValidationResult(process.ExitCode == 0, output.ToString().Trim());
    }

    /// <summary>
    /// Validates SPIR-V bytecode using the spirv-val tool from the Vulkan SDK.
    /// </summary>
    /// <param name="spirvBytes">Raw SPIR-V bytecode as a byte span.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating whether the bytecode is valid.</returns>
    public static ValidationResult ValidateBinary(ReadOnlySpan<byte> spirvBytes)
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempFile, spirvBytes.ToArray());
            return ValidateFile(tempFile);
        }
        finally
        {
            try { File.Delete(tempFile); } catch { }
        }
    }
}
