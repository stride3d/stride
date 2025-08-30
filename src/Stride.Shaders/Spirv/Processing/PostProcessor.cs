// using Stride.Shaders.Spirv.Core;
// using Stride.Shaders.Spirv.Core.Buffers;
// using Stride.Shaders.Spirv.Processing;

// namespace Stride.Shaders.Spirv.PostProcessing;

// /// <summary>
// /// Nano pass merger/optimizer/compiler
// /// </summary>
// public static class PostProcessor
// {
//     public static SpirvBuffer Process(string mixinName)
//     {
//         var buffer = new SpirvBuffer();
//         var mixin = MixinSourceProvider.Get(mixinName);
//         var parents = MixinSourceProvider.GetMixinGraph(mixinName);
//         var bound = 0;
//         foreach(var p in parents)
//         {
//             foreach (var i in p.Instructions)
//                 buffer.Duplicate(i.AsRef(), bound);
//             bound += p.Bound;
//         }
//         foreach(var i in mixin.Instructions)
//             buffer.Duplicate(i.AsRef(), bound);
//         Apply(buffer);

//         return new(buffer);
//     }

//     static void Apply(SpirvBuffer buffer)
//     {
//         Apply<IOVariableDecorator>(buffer);
//         Apply<SDSLVariableReplace>(buffer);
//         Apply<FunctionVariableOrderer>(buffer);
//         Apply<TypeDuplicateRemover>(buffer);
//         Apply<MemoryModelDuplicatesRemover>(buffer);
//         Apply<BoundReducer>(buffer);
//         Apply<OpRemover>(buffer);
//     }

//     static void Apply<T>(SpirvBuffer buffer)
//         where T : struct, INanoPass
//     {
//         var p = new T();
//         p.Apply(buffer);
//     }
// }