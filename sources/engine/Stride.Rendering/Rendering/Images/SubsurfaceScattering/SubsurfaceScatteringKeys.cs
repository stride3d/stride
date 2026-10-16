using Stride.Core;

namespace Stride.Rendering.SubsurfaceScattering
{
    internal static class SubsurfaceScatteringKeys
    {
        public static readonly PermutationParameterKey<int> MaxMaterialCount = (PermutationParameterKey<int>)ParameterKeys.Merge(ParameterKeys.NewPermutation<int>(), typeof(SubsurfaceScatteringKeys), "SubsurfaceScattering.MaxMaterialCount");
        public static readonly PermutationParameterKey<bool> KernelSizeJittering = (PermutationParameterKey<bool>)ParameterKeys.Merge(ParameterKeys.NewPermutation<bool>(), typeof(SubsurfaceScatteringKeys), "SubsurfaceScattering.KernelSizeJittering");
        public static readonly PermutationParameterKey<bool> BlurHorizontally = (PermutationParameterKey<bool>)ParameterKeys.Merge(ParameterKeys.NewPermutation<bool>(), typeof(SubsurfaceScatteringKeys), "SubsurfaceScattering.BlurHorizontally");
        public static readonly PermutationParameterKey<int> FollowSurface = (PermutationParameterKey<int>)ParameterKeys.Merge(ParameterKeys.NewPermutation<int>(1), typeof(SubsurfaceScatteringKeys), "SubsurfaceScattering.FollowSurface");
        public static readonly PermutationParameterKey<bool> OrthographicProjection = (PermutationParameterKey<bool>)ParameterKeys.Merge(ParameterKeys.NewPermutation<bool>(), typeof(SubsurfaceScatteringKeys), "SubsurfaceScattering.OrthographicProjection");
        public static readonly PermutationParameterKey<int> KernelLength = (PermutationParameterKey<int>)ParameterKeys.Merge(ParameterKeys.NewPermutation<int>(), typeof(SubsurfaceScatteringKeys), "SubsurfaceScattering.KernelLength");
        public static readonly PermutationParameterKey<int> RenderMode = (PermutationParameterKey<int>)ParameterKeys.Merge(ParameterKeys.NewPermutation<int>(), typeof(SubsurfaceScatteringKeys), "SubsurfaceScattering.RenderMode");

        [ModuleInitializer]
        internal static void __Initialize__()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(SubsurfaceScatteringKeys).TypeHandle);
        }
    }
}
