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
    private ShaderMixinInstantiation EvaluateInheritanceAndCompositions(IExternalShaderLoader shaderLoader, SpirvContext context, ShaderMixinSource? parent, ShaderSource shaderSource, Action<ShaderClassInstantiation>? addToRoot = null, HashSet<string>? needsFullImport = null)
    {
        var mixinList = new List<ShaderClassInstantiation>();

        Span<ShaderMacro> macros = (shaderSource, parent) switch
        {
            (ShaderMixinSource mixinSource, _) => mixinSource.Macros.AsSpan(),
            (_, not null) => parent.Macros.AsSpan(),
            _ => Span<ShaderMacro>.Empty,
        };

        var shaderMixinSource = shaderSource switch
        {
            ShaderMixinSource mixinSource2 => mixinSource2,
            ShaderClassSource classSource => new ShaderMixinSource { Mixins = { classSource } },
        };

        var compositions = new Dictionary<string, ShaderMixinInstantiation[]>();
        var result = new ShaderMixinInstantiation(new(), compositions);

        foreach (var mixinToMerge in shaderMixinSource.Mixins)
        {
            var shaderBuffer = SpirvBuilder.GetOrLoadShader(shaderLoader, mixinToMerge.ClassName, mixinToMerge.GenericArguments, macros);

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

            SpirvBuilder.BuildInheritanceListIncludingSelf(shaderLoader, context, mixinToMerge2, macros, mixinList, ResolveStep.Mix);
        }

        ProcessClasses(shaderLoader, context, mixinList, shaderMixinSource, result, compositions, addToRoot, needsFullImport);

        return result;
    }

    private void ProcessClasses(IExternalShaderLoader shaderLoader, SpirvContext context, List<ShaderClassInstantiation> mixinList, ShaderMixinSource shaderMixinSource, ShaderMixinInstantiation result, Dictionary<string, ShaderMixinInstantiation[]> compositions, Action<ShaderClassInstantiation>? addToRoot = null, HashSet<string>? needsFullImport = null)
    {
        int shaderIndex = 0;

        // Pre-scan: build set of shader names that need full import (not stage-only) at root level.
        // Two sources:
        //   - OpSDSLMixinInherit with NeedsFullImport: parent shader whose non-stage members are called by a child's stage method
        //   - OpSDSLFunctionInfo with ReferencesNonStage: shader's own non-stage members are called by its own stage method
        // The set is shared across root and composition calls so that compositions can contribute.
        needsFullImport ??= new HashSet<string>();
        foreach (var shader in mixinList)
        {
            var buf = shader.Buffer.Value;
            foreach (var i in buf.Context)
            {
                if (i.Op == Op.OpSDSLMixinInherit && (OpSDSLMixinInherit)i is { } inherit
                    && (inherit.Flags & MixinInheritFlagsMask.NeedsFullImport) != 0
                    && buf.Context.ReverseTypes.TryGetValue(inherit.Shader, out var inheritType) && inheritType is LoadedShaderSymbol lss)
                {
                    needsFullImport.Add(lss.Name);
                }
            }
            foreach (var i in buf.Buffer)
            {
                if (i.Op == Op.OpSDSLFunctionInfo && (OpSDSLFunctionInfo)i is { } fi
                    && (fi.Flags & FunctionFlagsMask.ReferencesNonStage) != 0)
                {
                    needsFullImport.Add(shader.ClassName);
                    break;
                }
            }
        }

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
                        var currentlyMixedList = result.Mixins[..];
                        SpirvBuilder.BuildInheritanceListIncludingSelf(shaderLoader, context, shaderName, shaderMixinSource.Macros.AsSpan(), currentlyMixedList, ResolveStep.Mix);

                        var newShadersToMergeNow = currentlyMixedList[result.Mixins.Count..];
                        result.Mixins.AddRange(newShadersToMergeNow);
                    }
                    else if (needsFullImport.Contains(shaderName.ClassName))
                    {
                        // Stage methods reference non-stage members from this shader, import fully
                        result.Mixins.Add(shaderName);
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
                if (i.Op == Op.OpVariableSDSL && (OpVariableSDSL)i is { } variable && variable.StorageClass != Specification.StorageClass.Function)
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
                                variableCompositions.Add(EvaluateInheritanceAndCompositions(shaderLoader, context, shaderMixinSource, value, addToRootRecursive, needsFullImport));
                            compositions[variableName] = [.. variableCompositions];
                        }
                        else
                        {
                            var variableComposition = EvaluateInheritanceAndCompositions(shaderLoader, context, shaderMixinSource, compositionMixin, addToRootRecursive, needsFullImport);
                            compositions[variableName] = [variableComposition];
                        }
                    }
                }

                if (i.Op == Op.OpSDSLFunctionInfo && (OpSDSLFunctionInfo)i is { } functionInfo)
                {
                    hasStage |= (functionInfo.Flags & FunctionFlagsMask.Stage) != 0;
                }
            }

            // If there are any stage variables/methods, add class to root
            if (hasStage)
                addToRoot?.Invoke(shaderName);

            // Note: make sure to add only *after* compositions EvaluateInheritanceAndCompositions recursive call is done (a composition might add a "stage" inheritance with root!.Mixins.Add()
            //       and this should be done before the composition mixin is added.
            //       For example, a composition might import a struct, so if we import and mix the composition mixin before the "stage" one defining the struct, the struct is not defined before the composition using it.
            result.Mixins.Add(shaderName);
        }

        // Post-processing: upgrade stage-only imports to full imports if compositions discovered
        // that their non-stage members are needed (via needsFullImport set populated during composition processing)
        if (addToRoot == null)
        {
            for (int i = 0; i < result.Mixins.Count; i++)
            {
                var mixin = result.Mixins[i];
                if (mixin.ImportStageOnly && needsFullImport.Contains(mixin.ClassName))
                {
                    result.Mixins[i] = new ShaderClassInstantiation(mixin.ClassName, mixin.GenericArguments) { Buffer = mixin.Buffer, Symbol = mixin.Symbol };
                }
            }
        }
    }

    private void PropagateMacrosRecursively(ShaderSource child, ShaderMixinSource? parent = null)
    {
        var existingMacros = new HashSet<string>();
        if (child is ShaderMixinSource mixinChild)
        {
            foreach (var macro in mixinChild.Macros)
            {
                existingMacros.Add(macro.Name);
            }
            if (parent != null)
            {
                foreach (var macro in parent.Macros)
                {
                    if (!existingMacros.Contains(macro.Name))
                        mixinChild.AddMacro(macro.Name, macro.Definition);
                }
            }

            // Recurse
            foreach (var mixin in mixinChild.Mixins)
            {
                PropagateMacrosRecursively(mixin, mixinChild);
            }
            foreach (var composition in mixinChild.Compositions)
            {
                PropagateMacrosRecursively(composition.Value, mixinChild);
            }
        }
        else if (child is ShaderArraySource arrayChild)
        {
            foreach (var mixin in arrayChild)
            {
                PropagateMacrosRecursively(mixin, parent);
            }
        }
    }
}
