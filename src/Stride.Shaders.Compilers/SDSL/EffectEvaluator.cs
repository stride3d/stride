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
    internal class EffectEvaluator(IExternalShaderLoader ShaderLoader)
    {
        public ShaderSource EvaluateEffects(ShaderSource source)
        {
            switch (source)
            {
                case ShaderClassSource classSource:
                    var buffer = SpirvBuilder.GetOrLoadShader(ShaderLoader, classSource.ClassName, classSource.GenericArguments);

                    if (buffer[0].Op == Op.OpSDSLEffect)
                    {
                        var mixinTree = new ShaderMixinSource();
                        foreach (var instruction in buffer)
                        {
                            if (instruction.Op == Op.OpSDSLMixin && (OpSDSLMixin)instruction is { } mixinInstruction)
                            {
                                // Resolve generics
                                var genericArguments = new object[mixinInstruction.Values.Elements.Length];
                                for (int i = 0; i < genericArguments.Length; i++)
                                {
                                    genericArguments[i] = SpirvBuilder.GetConstantValue(mixinInstruction.Values.Elements.Span[i], buffer);
                                }

                                var instSource = new ShaderClassSource(mixinInstruction.Mixin, genericArguments);
                                var evaluatedSource = EvaluateEffects(instSource);

                                Merge(mixinTree, evaluatedSource);
                            }
                            else if (instruction.Op == Op.OpSDSLMixinCompose && (OpSDSLMixinCompose)instruction is { } mixinComposeInstruction)
                            {
                                var instSource = new ShaderClassSource(mixinComposeInstruction.Mixin);
                                var evaluatedSource = EvaluateEffects(instSource);

                                MergeComposition(mixinTree, mixinComposeInstruction.Identifier, evaluatedSource);
                            }
                            else if (instruction.Op == Op.OpSDSLMixinComposeArray && (OpSDSLMixinComposeArray)instruction is { } mixinComposeArray)
                            {
                                var instSource = new ShaderClassSource(mixinComposeArray.Mixin);
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
                        {
                            result.Macros.Add(macro);
                        }
                        foreach (var mixin in mixinSource.Mixins)
                        {
                            var evaluatedMixin = EvaluateEffects(mixin);
                            Merge(result, evaluatedMixin);
                        }

                        foreach (var composition in mixinSource.Compositions)
                        {
                            var evaluatedMixin = EvaluateEffects(composition.Value);
                            if (evaluatedMixin is ShaderArraySource shaderArraySource)
                            {
                                MergeCompositionArray(result, composition.Key, shaderArraySource);
                            }
                            else
                            {
                                MergeComposition(result, composition.Key, evaluatedMixin);
                            }
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

        public void Merge(ShaderMixinSource mixinTree, ShaderSource source)
        {
            switch (source)
            {
                case ShaderClassSource classSource:
                    mixinTree.Mixins.Add(classSource);
                    break;
                case ShaderMixinSource mixinSource:
                    foreach (var mixin in mixinSource.Mixins)
                    {
                        mixinTree.Mixins.Add(mixin);
                    }

                    foreach (var composition in mixinSource.Compositions)
                    {
                        if (mixinTree.Compositions.TryGetValue(composition.Key, out var mixinTreeComposition))
                            mixinTree.Compositions.Add(composition.Key, mixinTreeComposition = new ShaderMixinSource());
                        Merge((ShaderMixinSource)mixinTreeComposition, composition.Value);
                    }

                    break;
            }
        }

        public void MergeComposition(ShaderMixinSource mixinTree, string compositionName, ShaderSource evaluatedSource)
        {
            if (!mixinTree.Compositions.TryGetValue(compositionName, out var composition))
                mixinTree.Compositions.Add(compositionName, composition = new ShaderMixinSource());

            Merge((ShaderMixinSource)composition, evaluatedSource);
        }

        public void MergeCompositionArray(ShaderMixinSource mixinTree, string compositionName, ShaderArraySource evaluatedSource)
        {
            if (!mixinTree.Compositions.TryGetValue(compositionName, out var composition))
                mixinTree.Compositions.Add(compositionName, composition = new ShaderArraySource());

            var arraySource = (ShaderArraySource)composition;
            arraySource.Values.AddRange(evaluatedSource.Values);
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
