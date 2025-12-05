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
    private ShaderMixinInstantiation EvaluateInheritanceAndCompositions(ShaderSource shaderSource, ShaderMixinInstantiation? root = null)
    {
        bool isRoot = root == null;
        var mixinList = new List<ShaderClassInstantiation>();

        var shaderMixinSource = shaderSource switch
        {
            ShaderMixinSource mixinSource2 => mixinSource2,
            ShaderClassSource classSource => new ShaderMixinSource { Mixins = { classSource } },
        };

        foreach (var mixinToMerge in shaderMixinSource.Mixins)
        {
            if (mixinToMerge.GenericArguments != null && mixinToMerge.GenericArguments.Length > 0)
                throw new NotImplementedException("Generics at the top-level shaders is not supported");
            var mixinToMerge2 = new ShaderClassInstantiation(mixinToMerge.ClassName, []);
            var buffer = SpirvBuilder.GetOrLoadShader(ShaderLoader, mixinToMerge2.ClassName);
            mixinToMerge2.Buffer = buffer;
            //SpirvBuilder.BuildInheritanceList(ShaderLoader, buffer, mixinList, ResolveStep.Mix);
            SpirvBuilder.BuildInheritanceList(ShaderLoader, mixinToMerge2, mixinList, ResolveStep.Mix);
        }

        var compositions = new Dictionary<string, ShaderMixinInstantiation[]>();
        var result = new ShaderMixinInstantiation(mixinList, compositions);

        foreach (var shaderName in mixinList.ToArray())
        {
            var shader = shaderName.Buffer;
            ShaderClass.ProcessNameAndTypes(shader, 0, shader.Count, out var names, out var types);

            bool hasStage = false;
            foreach (var i in shader)
            {
                if (i.Op == Op.OpVariable && (OpVariable)i is { } variable && variable.Storageclass != Specification.StorageClass.Function)
                {
                    var variableType = types[variable.ResultType];
                    if (variableType is PointerType pointer && pointer.BaseType is ShaderSymbol or ArrayType { BaseType: ShaderSymbol })
                    {
                        var variableName = names[variable.ResultId];
                        // Make sure we have a ShaderMixinSource
                        // If composition is not specified, use default class
                        if (!shaderMixinSource.Compositions.TryGetValue(variableName, out var compositionMixin))
                        {
                            if (pointer.BaseType is ShaderSymbol shaderSymbol)
                                compositionMixin = new ShaderMixinSource { Mixins = { new ShaderClassSource(shaderSymbol.Name) } };
                            else if (pointer.BaseType is ArrayType { BaseType: ShaderSymbol })
                                compositionMixin = new ShaderArraySource();
                            else
                                throw new NotImplementedException();
                        }

                        if (compositionMixin is ShaderArraySource shaderArraySource)
                        {
                            var variableCompositions = new List<ShaderMixinInstantiation>();
                            foreach (var value in shaderArraySource.Values)
                                variableCompositions.Add(EvaluateInheritanceAndCompositions(value, root ?? result));
                            compositions[variableName] = [..variableCompositions];
                        }
                        else
                        {
                            var variableComposition = EvaluateInheritanceAndCompositions(compositionMixin, root ?? result);
                            compositions[variableName] = [variableComposition];
                        }
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
                var shaderNameStageOnly = new ShaderClassInstantiation(shaderName.ClassName, shaderName.GenericArguments, ImportStageOnly: true) { Buffer = shaderName.Buffer, ShaderReferences = shaderName.ShaderReferences };
                // Make sure it's not already added yet (either standard or stage only)
                if (!root!.Mixins.Contains(shaderName) && !root!.Mixins.Contains(shaderNameStageOnly))
                    root!.Mixins.Add(shaderNameStageOnly);
            }
        }

        return result;
    }
}