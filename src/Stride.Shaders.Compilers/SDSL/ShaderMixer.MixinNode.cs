using Stride.Shaders.Spirv.Building;

namespace Stride.Shaders.Compilers.SDSL;

public partial class ShaderMixer
{
    /// <summary>
    /// Represents a mixin node which will merge multiple shaders.
    /// </summary>
    /// <example>
    /// The following shader would have 3 <see cref="MixinNode"/>: top-level stage one (with MyShader and MyBase) and 2 compositions nodes (Comp0 and Comp1).
    /// <code>
    /// shader MyShader : MyBase
    /// {
    ///     ComputeColor Comp0;
    ///     ComputeColor Comp1;
    /// }
    /// </code>
    /// </example>
    /// <param name="stage"></param>
    private partial class MixinNode(MixinNode? stage, string? compositionPath)
    {
        /// <summary>
        /// If we are inside a composition node, this provides access to the stage (top-level) <see cref="MixinNode"/>.
        /// </summary>
        public MixinNode? Stage { get; } = stage;

        public bool IsRoot => Stage == null;

        public string? CompositionPath { get; } = compositionPath;

        public int StartInstruction { get; internal set; }
        public int EndInstruction { get; internal set; }

        /// <summary>
        /// List of shaders mixed in this node.
        /// </summary>
        public List<ShaderInfo> Shaders { get; } = new();

        public Dictionary<string, ShaderInfo> ShadersByName { get; } = new();
        public Dictionary<string, int> MethodGroupsByName { get; } = new();
        public Dictionary<int, MethodGroup> MethodGroups { get; } = new();
        
        public Dictionary<int, MixinNode> Compositions { get; } = new();
        public Dictionary<int, MixinNode[]> CompositionArrays { get; } = new();

        public Dictionary<int, string> ExternalShaders { get; } = new();
        public Dictionary<int, (int ShaderId, string Name)> ExternalFunctions { get; } = new();
        public Dictionary<int, (int ShaderId, string Name)> ExternalVariables { get; } = new();
    }

    class MethodGroup
    {
        public string Name;
        public ShaderInfo Shader;
        public List<(ShaderInfo Shader, int MethodId)> Methods { get; } = new();

        public override string ToString() => $"{Name} (shader: {Shader}, function Id: {string.Join(", ", Methods.Select(x => $"{x.Shader.ShaderName} {x.MethodId}"))})";
    }
}