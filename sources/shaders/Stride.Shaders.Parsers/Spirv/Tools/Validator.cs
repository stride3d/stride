namespace Stride.Shaders.Spirv.Tools;

public readonly record struct ValidationResult(bool IsValid, string Output)
{
    public override string ToString() => IsValid ? "Valid" : Output;
}

public static partial class Spv
{
    /// <summary>
    /// Validates a SPIR-V file using the in-process SPIRV-Tools validator.
    /// </summary>
    /// <param name="filePath">Path to a .spv file.</param>
    /// <param name="targetVulkan">When true, validates against Vulkan 1.4 with relaxed block layout and UBO standard layout.</param>
    public static ValidationResult ValidateFile(string filePath, bool targetVulkan = false)
        => ValidateBinary(File.ReadAllBytes(filePath), targetVulkan);

    /// <summary>
    /// Validates SPIR-V bytecode using the in-process SPIRV-Tools validator.
    /// </summary>
    public static ValidationResult ValidateBinary(ReadOnlySpan<byte> spirvBytes, bool targetVulkan = false)
    {
        var env = targetVulkan ? SpirvTools.TargetEnv.Vulkan_1_4 : SpirvTools.TargetEnv.Universal_1_6;
        var options = targetVulkan
            ? SpirvTools.ValidatorOptions.RelaxBlockLayout | SpirvTools.ValidatorOptions.UniformBufferStandardLayout
            : SpirvTools.ValidatorOptions.None;
        var message = SpirvTools.Validate(spirvBytes, env, options);
        return message is null
            ? new ValidationResult(true, "")
            : new ValidationResult(false, message);
    }
}
