using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;

namespace Stride.Shaders.Compilers.SDSL;
using static Stride.Shaders.Spirv.Specification;

public partial class ShaderMixer
{
    /// <summary>
    /// Expands inheritance (including implicit and transitive ones) and composition (including shaders that should be merged at stage level).
    /// </summary>
    /// <param name="shaderSource"></param>
    /// <param name="root"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private ShaderMixinSource EvaluateInheritanceAndCompositions(ShaderSource shaderSource, ShaderMixinSource? root = null)
    {
        bool isRoot = root == null;
        var mixinList = new List<ShaderClassSource>();

        var shaderMixinSource = shaderSource switch
        {
            ShaderMixinSource mixinSource2 => mixinSource2,
            ShaderClassSource classSource => new ShaderMixinSource { Mixins = { classSource } },
        };
        foreach (var mixinToMerge in shaderMixinSource.Mixins)
        {
            if (mixinToMerge.GenericArguments != null && mixinToMerge.GenericArguments.Length > 0)
                throw new NotImplementedException();

            var buffer = SpirvBuilder.GetOrLoadShader(ShaderLoader, mixinToMerge.ClassName);
            SpirvBuilder.BuildInheritanceList(ShaderLoader, buffer, mixinList);
            if (!mixinList.Contains(mixinToMerge))
                mixinList.Add(mixinToMerge);
        }

        shaderMixinSource.Mixins.Clear();
        shaderMixinSource.Mixins.AddRange(mixinList);

        foreach (var shaderName in mixinList)
        {
            var shader = SpirvBuilder.GetOrLoadShader(ShaderLoader, shaderName.ClassName);
            ShaderClass.ProcessNameAndTypes(shader, 0, shader.Count, out var names, out var types);

            bool hasStage = false;
            foreach (var i in shader)
            {
                if (i.Op == Op.OpVariable && (OpVariable)i is { } variable && variable.Storageclass != Specification.StorageClass.Function)
                {
                    var variableType = types[variable.ResultType];
                    if (variableType is PointerType pointer && pointer.BaseType is ShaderSymbol shaderSymbol)
                    {
                        var variableName = names[variable.ResultId];
                        // Make sure we have a ShaderMixinSource
                        // If composition is not specified, use default class
                        if (!shaderMixinSource.Compositions.TryGetValue(variableName, out var compositionMixin))
                        {
                            compositionMixin = new ShaderMixinSource { Mixins = { new ShaderClassSource(shaderSymbol.Name) } };
                        }
                        compositionMixin = (ShaderMixinSource)EvaluateInheritanceAndCompositions(compositionMixin, root ?? shaderMixinSource);
                        shaderMixinSource.Compositions[variableName] = compositionMixin;
                    }
                }

                if (i.Op == Op.OpSDSLFunctionInfo && (OpSDSLFunctionInfo)i is {} functionInfo)
                {
                    hasStage |= (functionInfo.Flags & FunctionFlagsMask.Stage) != 0;
                }
            }

            // If there are any stage variables, add class to root
            if (!isRoot && hasStage)
            {
                var shaderNameStageOnly = new ShaderClassSource(shaderName.ClassName) { GenericArguments = shaderName.GenericArguments, ImportStageOnly = true };
                // Make sure it's not already added yet (either standard or stage only)
                if (!root!.Mixins.Contains(shaderName) && !root!.Mixins.Contains(shaderNameStageOnly))
                    root!.Mixins.Add(shaderNameStageOnly);
            }
        }

        return shaderMixinSource;
    }
}