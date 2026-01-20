using CommunityToolkit.HighPerformance;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Compilers.SDSL
{
    internal class EffectEvaluator(IExternalShaderLoader shaderLoader)
    {
        private Stack<ShaderMixinSource> mixinSources = new();

        public ShaderSource EvaluateEffects(ShaderSource source)
        {
            object[] GetGenericsArguments(SpirvContext context, ReadOnlySpan<int> genericIds)
            {
                var genericArguments = new object[genericIds.Length];
                for (int i = 0; i < genericArguments.Length; i++)
                {
                    genericArguments[i] = context.GetConstantValue(genericIds[i]);
                }
                return genericArguments;
            }

            switch (source)
            {
                case ShaderClassSource classSource:
                    var macros = mixinSources.Count > 0 ? mixinSources.Peek().Macros : [];
                    var shaderBuffers = SpirvBuilder.GetOrLoadShader(shaderLoader, classSource.ClassName, classSource.GenericArguments, macros.AsSpan());

                    if (shaderBuffers.Buffer[0].Op == Op.OpSDSLEffect)
                    {
                        var mixinTree = new ShaderMixinSource();
                        foreach (var instruction in shaderBuffers.Buffer)
                        {
                            if (instruction.Op == Op.OpSDSLMixin && (OpSDSLMixin)instruction is { } mixinInstruction)
                            {
                                var instSource = new ShaderClassSource(mixinInstruction.Mixin, GetGenericsArguments(shaderBuffers.Context, mixinInstruction.Values.Elements.Span));
                                var evaluatedSource = EvaluateEffects(instSource);

                                Merge(mixinTree, evaluatedSource);
                            }
                            else if (instruction.Op == Op.OpSDSLMixinCompose && (OpSDSLMixinCompose)instruction is { } mixinComposeInstruction)
                            {
                                var instSource = new ShaderClassSource(mixinComposeInstruction.Mixin, GetGenericsArguments(shaderBuffers.Context, mixinComposeInstruction.Values.Elements.Span));
                                var evaluatedSource = EvaluateEffects(instSource);

                                MergeComposition(mixinTree, mixinComposeInstruction.Identifier, evaluatedSource);
                            }
                            else if (instruction.Op == Op.OpSDSLMixinComposeArray && (OpSDSLMixinComposeArray)instruction is { } mixinComposeArray)
                            {
                                var instSource = new ShaderClassSource(mixinComposeArray.Mixin, GetGenericsArguments(shaderBuffers.Context, mixinComposeArray.Values.Elements.Span));
                                var evaluatedSource = EvaluateEffects(instSource);

                                MergeCompositionArrayItem(mixinTree, mixinComposeArray.Identifier, evaluatedSource);
                            }
                        }

                        return mixinTree;
                    }

                    return classSource;
                case ShaderMixinSource mixinSource:
                    {
                        var result = new ShaderMixinSource();
                        foreach (var macro in mixinSource.Macros)
                            result.Macros.Add(macro);

                        if (mixinSources.Count > 0)
                            PropagateMacrosFromParent(mixinSources.Peek(), result);

                        mixinSources.Push(result);
                        try
                        {
                            foreach (var mixin in mixinSource.Mixins)
                            {
                                var evaluatedMixin = EvaluateEffects(mixin);
                                Merge(result, evaluatedMixin);
                            }

                            foreach (var composition in mixinSource.Compositions)
                            {
                                var evaluatedMixin = EvaluateEffects(composition.Value);
                                MergeComposition(result, composition.Key, evaluatedMixin);
                            }
                        }
                        finally
                        {
                            mixinSources.Pop();
                        }
                        return result;
                    }
                case ShaderArraySource arraySource:
                    {
                        var result = new ShaderArraySource();
                        foreach (var mixin in arraySource.Values)
                        {
                            var evaluatedMixin = EvaluateEffects(mixin);
                            result.Add(evaluatedMixin);
                        }
                        return result;
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        private void PropagateMacrosFromParent(ShaderMixinSource parent, ShaderMixinSource child)
        {
            var existingMacros = new HashSet<string>();
            foreach (var macro in child.Macros)
            {
                existingMacros.Add(macro.Name);
            }
            foreach (var macro in parent.Macros)
            {
                if (!existingMacros.Contains(macro.Name))
                    child.AddMacro(macro.Name, macro.Definition);
            }
        }

        public void Merge(ShaderMixinSource mixinTree, ShaderSource source)
        {
            switch (source)
            {
                case ShaderClassSource classSource:
                    mixinTree.Mixins.Add(classSource);
                    break;
                case ShaderMixinSource mixinSource:
                    mixinTree.Macros.AddRange(mixinSource.Macros);
                    mixinTree.Mixins.AddRange(mixinSource.Mixins);

                    foreach (var composition in mixinSource.Compositions)
                    {
                        MergeComposition(mixinTree, composition.Key, composition.Value);
                    }

                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        public void MergeComposition(ShaderMixinSource mixinTree, string compositionName, ShaderSource compositionToAdd)
        {
            if (!mixinTree.Compositions.TryGetValue(compositionName, out var composition))
                mixinTree.Compositions.Add(compositionName, composition = compositionToAdd is ShaderArraySource ? new ShaderArraySource() : new ShaderMixinSource());

            if (compositionToAdd is ShaderArraySource compositionArrayToAdd)
            {
                var compositionArray = (ShaderArraySource)composition;
                compositionArray.Values.AddRange(compositionArrayToAdd);
            }
            else
            {
                Merge((ShaderMixinSource)composition, compositionToAdd);
            }
        }

        public void MergeCompositionArrayItem(ShaderMixinSource mixinTree, string compositionName, ShaderSource evaluatedSource)
        {
            if (!mixinTree.Compositions.TryGetValue(compositionName, out var composition))
                mixinTree.Compositions.Add(compositionName, composition = new ShaderArraySource());

            var arraySource = (ShaderArraySource)composition;
            arraySource.Add(evaluatedSource);
        }
    }
}
