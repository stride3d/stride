using Xenko.Shaders;

namespace Xenko.Rendering.Voxels
{
    public partial class BufferToTextureKeys
    {
        public static readonly PermutationParameterKey<ShaderSourceCollection> AttributesIndirect = ParameterKeys.NewPermutation<ShaderSourceCollection>();
        public static readonly PermutationParameterKey<ShaderSourceCollection> AttributesTemp = ParameterKeys.NewPermutation<ShaderSourceCollection>();
        public static readonly PermutationParameterKey<ShaderSourceCollection> AttributeLocalSamples = ParameterKeys.NewPermutation<ShaderSourceCollection>();
        public static readonly PermutationParameterKey<string> IndirectReadAndStoreMacro = ParameterKeys.NewPermutation<string>();
        public static readonly PermutationParameterKey<string> IndirectStoreMacro = ParameterKeys.NewPermutation<string>();
    }
}
