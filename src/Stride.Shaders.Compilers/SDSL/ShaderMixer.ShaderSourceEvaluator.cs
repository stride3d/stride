using CommunityToolkit.HighPerformance;
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
    private ShaderMixinInstantiation EvaluateInheritanceAndCompositions(IExternalShaderLoader shaderLoader, SpirvContext context, ShaderSource shaderSource, Action<ShaderClassInstantiation>? addToRoot = null)
    {
        var mixinList = new List<ShaderClassInstantiation>();

        var shaderMixinSource = shaderSource switch
        {
            ShaderMixinSource mixinSource2 => mixinSource2,
            ShaderClassSource classSource => new ShaderMixinSource { Mixins = { classSource } },
        };

        var compositions = new Dictionary<string, ShaderMixinInstantiation[]>();
        var result = new ShaderMixinInstantiation(new(), compositions);

        foreach (var mixinToMerge in shaderMixinSource.Mixins)
        {
            var shaderBuffer = SpirvBuilder.GetOrLoadShader(shaderLoader, mixinToMerge.ClassName, mixinToMerge.GenericArguments, shaderMixinSource.Macros.AsSpan());

            var mixinToMerge2 = new ShaderClassInstantiation(mixinToMerge.ClassName, []);
            mixinToMerge2.Buffer = shaderBuffer;
            // Copy back updated shader name (in case it had generic parameters)
            foreach (var i in shaderBuffer.Buffer)
            {
                if (i.Op == Op.OpSDSLShader && (OpSDSLShader)i is { } shaderInstruction)
                {
                    mixinToMerge2.ClassName = shaderInstruction.ShaderName;
                    break;
                }
            }

            SpirvBuilder.BuildInheritanceListIncludingSelf(shaderLoader, context, mixinToMerge2, shaderMixinSource.Macros.AsSpan(), mixinList, ResolveStep.Mix);
        }
        
        ProcessClasses(shaderLoader, context, mixinList, shaderMixinSource, result, compositions, addToRoot);

        return result;
    }

    private void ProcessClasses(IExternalShaderLoader shaderLoader, SpirvContext context, List<ShaderClassInstantiation> mixinList, ShaderMixinSource shaderMixinSource, ShaderMixinInstantiation result, Dictionary<string, ShaderMixinInstantiation[]> compositions, Action<ShaderClassInstantiation>? addToRoot = null)
    {
        int shaderIndex = 0;
        
        var addToRootRecursive = addToRoot;
        if (addToRootRecursive == null)
        {
            addToRootRecursive = shaderName =>
            {
                var shaderNameStageOnly = new ShaderClassInstantiation(shaderName.ClassName, shaderName.GenericArguments, ImportStageOnly: true) { Buffer = shaderName.Buffer, Symbol = shaderName.Symbol };
                
               
                // Make sure it's not already added yet (either standard or stage only)
                if (!result.Mixins.Contains(shaderName) && !result!.Mixins.Contains(shaderNameStageOnly))
                {
                    // Check if mixin will be added in future as a non-stage
                    if (mixinList.Contains(shaderName))
                    {
                        // Special case: the current stage-only mixin is planned to be added later as a normal mixin at the root level
                        // It's a bit complex: we need to inherit from it right now instead of later
                        // (if we simply do a result.Mixins.Add as in normal case, the shader would be added twice)
                        var currentlyMixedList = mixinList[0..shaderIndex];
                        SpirvBuilder.BuildInheritanceListIncludingSelf(shaderLoader, context, shaderName, shaderMixinSource.Macros.AsSpan(), currentlyMixedList, ResolveStep.Mix);

                        var newShadersToMergeNow = currentlyMixedList[shaderIndex..];
                        mixinList.InsertRange(shaderIndex, newShadersToMergeNow);
                        
                        // Note: we're not removing duplicates as we do an extra duplicate check at the beginning of the mixinList loop
                    }
                    else
                    {
                        result.Mixins.Add(shaderNameStageOnly);
                    }
                }
            };
        }

        for (; shaderIndex < mixinList.Count; shaderIndex++)
        {
            var shaderName = mixinList[shaderIndex];
            // Note: this should only happen due to addToRootRecursive readding some mixin earlier
            if (result.Mixins.Contains(shaderName))
                continue;
            
            var shader = shaderName.Buffer.Value;
            bool hasStage = false;
            foreach (var i in shader.Context)
            {
                if (i.Op == Op.OpTypeStruct)
                {
                    hasStage = true;
                }
            }

            foreach (var i in shader.Buffer)
            {
                if (i.Op == Op.OpVariableSDSL && (OpVariableSDSL)i is { } variable && variable.Storageclass != Specification.StorageClass.Function)
                {
                    hasStage |= (variable.Flags & VariableFlagsMask.Stage) != 0;

                    var variableType = shader.Context.ReverseTypes[variable.ResultType];
                    if (variableType is PointerType pointer && pointer.BaseType is ShaderSymbol or ArrayType { BaseType: ShaderSymbol })
                    {
                        var variableName = shader.Context.Names[variable.ResultId];
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
                                variableCompositions.Add(EvaluateInheritanceAndCompositions(shaderLoader, context, value, addToRootRecursive));
                            compositions[variableName] = [..variableCompositions];
                        }
                        else
                        {
                            var variableComposition = EvaluateInheritanceAndCompositions(shaderLoader, context, compositionMixin, addToRootRecursive);
                            compositions[variableName] = [variableComposition];
                        }
                    }
                }

                if (i.Op == Op.OpSDSLFunctionInfo && (OpSDSLFunctionInfo)i is { } functionInfo)
                {
                    hasStage |= (functionInfo.Flags & FunctionFlagsMask.Stage) != 0;
                }
            }

            // If there are any stage variables, add class to root
            if (hasStage)
                addToRoot?.Invoke(shaderName);
            
            // Note: make sure to add only *after* compositions EvaluateInheritanceAndCompositions recursive call is done (a composition might add a "stage" inheritance with root!.Mixins.Add()
            //       and this should be done before the composition mixin is added.
            //       For example, a composition might import a struct, so if we import and mix the composition mixin before the "stage" one defining the struct, the struct is not defined before the composition using it.
            result.Mixins.Add(shaderName);
        }
    }
}