using System.Text;
using Stride.Shaders.Core;
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
        public Dictionary<(string MethodName, FunctionType FunctionType), int> MethodGroupsByName { get; } = new();
        public Dictionary<int, MethodGroup> MethodGroups { get; } = new();
        
        public Dictionary<int, MixinNode> Compositions { get; } = new();
        public Dictionary<int, MixinNode[]> CompositionArrays { get; } = new();

        public override string ToString()
            => $"MixinNode ({(CompositionPath != null ? $" {CompositionPath} " : "")}{StartInstruction}..{EndInstruction}) ({Shaders.Count} shaders, {Compositions.Count} compositions, {CompositionArrays.Count} composition arrays)";

        public string ToDetailedString()
        {
            var sb = new StringBuilder();
            Recurse(sb, this);
            return sb.ToString();

            static void Recurse(StringBuilder sb, MixinNode node, int indent = 0)
            {
                sb.Append(' ', indent * 2);
                sb.AppendLine(node.ToString());
                foreach (var composition in node.Compositions)
                    Recurse(sb, composition.Value, indent + 1);
                foreach (var compositions in node.CompositionArrays)
                    foreach (var composition in compositions.Value)
                        Recurse(sb, composition, indent + 1);
            }
        }
    }

    class MethodGroup
    {
        public string Name;
        public ShaderInfo Shader;
        public FunctionType FunctionType;
        public List<(ShaderInfo Shader, int MethodId, Spirv.Specification.FunctionFlagsMask Flags)> Methods { get; } = new();

        public override string ToString() => $"{Name} (shader: {Shader}, function Id: {string.Join(", ", Methods.Select(x => $"{x.Shader.ShaderName} {x.MethodId}"))})";
    }
}