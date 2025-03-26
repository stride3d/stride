// using Stride.Shaders.Spirv.Core;
// using Stride.Shaders.Spirv.Core.Buffers;
// using Stride.Shaders.Spirv.Processing;
// using static Spv.Specification;

// namespace Stride.Shaders.Spirv.PostProcessing;

// public struct IOVariableDecorator : INanoPass
// {
//     public void Apply(SpirvBuffer buffer)
//     {
//         int inputLocation = -1;
//         int outputLocation = -1;
//         foreach (var i in buffer.Declarations)
//         {
//             if(i.OpCode == SDSLOp.OpSDSLIOVariable)
//             {
//                 var execution = (ExecutionModel)(i.GetOperand<LiteralInteger>("executionModel")?.Words ?? -1);
//                 var storage = (StorageClass)(i.GetOperand<LiteralInteger>("storageclass")?.Words ?? -1);
//                 var semantic = i.GetOperand<LiteralString>("semantic")?.Value ?? throw new NotImplementedException();
//                 if (semantic == "SV_Position")
//                 {
//                     buffer.AddOpDecorate(
//                         i.ResultId ?? -1, 
//                         Decoration.BuiltIn, 
//                         (storage,execution) switch
//                         {
//                             (StorageClass.Input, ExecutionModel.Fragment) => (int)BuiltIn.FragCoord,
//                             (StorageClass.Input or StorageClass.Output, _) 
//                                 => (int)BuiltIn.Position,
//                             _ => throw new NotImplementedException()
//                         }
//                     );
//                 }
//                 else if (semantic == "SV_ClipDistance")
//                 {
//                     buffer.AddOpDecorate(
//                         i.ResultId ?? -1,
//                         Decoration.BuiltIn,
//                         (storage, execution) switch
//                         {
//                             (StorageClass.Input or StorageClass.Output, _)
//                                 => (int)BuiltIn.ClipDistance,
//                             _ => throw new NotImplementedException()
//                         }
//                     );
//                 }
//                 else if (semantic == "SV_CullDistance")
//                 {
//                     buffer.AddOpDecorate(
//                         i.ResultId ?? -1,
//                         Decoration.BuiltIn,
//                         (storage, execution) switch
//                         {
//                             (StorageClass.Input or StorageClass.Output, _)
//                                 => (int)BuiltIn.CullDistance,
//                             _ => throw new NotImplementedException()
//                         }
//                     );
//                 }
//                 else if (semantic == "SV_VertexID")
//                 {
//                     buffer.AddOpDecorate(
//                         i.ResultId ?? -1,
//                         Decoration.BuiltIn,
//                         (storage, execution) switch
//                         {
//                             (StorageClass.Input, ExecutionModel.Vertex)
//                                 => (int)BuiltIn.VertexIndex,
//                             _ => throw new NotImplementedException()
//                         }
//                     );
//                 }
//                 else if (semantic == "SV_InstanceID")
//                 {
//                     buffer.AddOpDecorate(
//                         i.ResultId ?? -1,
//                         Decoration.BuiltIn,
//                         (storage, execution) switch
//                         {
//                             (StorageClass.Input, ExecutionModel.Vertex)
//                                 => (int)BuiltIn.InstanceIndex,
//                             _ => throw new NotImplementedException()
//                         }
//                     );
//                 }
//                 else if (semantic == "SV_Depth" || semantic == "SV_DepthGreaterEqual" || semantic == "SV_DepthLessEqual")
//                 {
//                     buffer.AddOpDecorate(
//                         i.ResultId ?? -1,
//                         Decoration.BuiltIn,
//                         (storage, execution) switch
//                         {
//                             (StorageClass.Output,ExecutionModel.Fragment)
//                                 => (int)BuiltIn.FragDepth,
//                             _ => throw new NotImplementedException()
//                         }
//                     );
//                 }
//                 else if (semantic == "SV_IsFrontFace")
//                 {
//                     buffer.AddOpDecorate(
//                         i.ResultId ?? -1,
//                         Decoration.BuiltIn,
//                         (storage, execution) switch
//                         {
//                             (StorageClass.Input, ExecutionModel.Fragment)
//                                 => (int)BuiltIn.FrontFacing,
//                             _ => throw new NotImplementedException()
//                         }
//                     );
//                 }
//                 else if (semantic == "SV_DispatchThreadID")
//                 {
//                     buffer.AddOpDecorate(
//                         i.ResultId ?? -1,
//                         Decoration.BuiltIn,
//                         (storage, execution) switch
//                         {
//                             (
//                                 StorageClass.Input, 
//                                 ExecutionModel.GLCompute 
//                                 or ExecutionModel.MeshEXT
//                                 or ExecutionModel.MeshNV
//                                 or ExecutionModel.TaskEXT
//                                 or ExecutionModel.TaskNV
//                             )
//                                 => (int)BuiltIn.GlobalInvocationId,
//                             _ => throw new NotImplementedException()
//                         }
//                     );
//                 }
//                 else if (semantic == "SV_GroupID")
//                 {
//                     buffer.AddOpDecorate(
//                         i.ResultId ?? -1,
//                         Decoration.BuiltIn,
//                         (storage, execution) switch
//                         {
//                             (
//                                 StorageClass.Input, 
//                                 ExecutionModel.GLCompute 
//                                 or ExecutionModel.MeshEXT
//                                 or ExecutionModel.MeshNV
//                                 or ExecutionModel.TaskEXT
//                                 or ExecutionModel.TaskNV
//                             )
//                                 => (int)BuiltIn.WorkgroupId,
//                             _ => throw new NotImplementedException()
//                         }
//                     );
//                 }
//                 else if (semantic == "SV_GroupThreadID")
//                 {
//                     buffer.AddOpDecorate(
//                         i.ResultId ?? -1,
//                         Decoration.BuiltIn,
//                         (storage, execution) switch
//                         {
//                             (
//                                 StorageClass.Input,
//                                 ExecutionModel.GLCompute
//                                 or ExecutionModel.MeshEXT
//                                 or ExecutionModel.MeshNV
//                                 or ExecutionModel.TaskEXT
//                                 or ExecutionModel.TaskNV
//                             )
//                                 => (int)BuiltIn.LocalInvocationId,
//                             _ => throw new NotImplementedException()
//                         }
//                     );
//                 }
//                 else if (semantic == "SV_GroupIndex")
//                 {
//                     buffer.AddOpDecorate(
//                         i.ResultId ?? -1,
//                         Decoration.BuiltIn,
//                         (storage, execution) switch
//                         {
//                             (
//                                 StorageClass.Input,
//                                 ExecutionModel.GLCompute
//                                 or ExecutionModel.MeshEXT
//                                 or ExecutionModel.MeshNV
//                                 or ExecutionModel.TaskEXT
//                                 or ExecutionModel.TaskNV
//                             )
//                                 => (int)BuiltIn.LocalInvocationIndex,
//                             _ => throw new NotImplementedException()
//                         }
//                     );
//                 }
//                 else if (semantic == "SV_OutputControlPointID")
//                 {
//                     buffer.AddOpDecorate(
//                         i.ResultId ?? -1,
//                         Decoration.BuiltIn,
//                         (storage, execution) switch
//                         {
//                             (
//                                 StorageClass.Input,
//                                 ExecutionModel.TessellationControl
//                             )
//                                 => (int)BuiltIn.InvocationId,
//                             _ => throw new NotImplementedException()
//                         }
//                     );
//                 }
//                 else if (semantic == "SV_GSInstanceID")
//                 {
//                     buffer.AddOpDecorate(
//                         i.ResultId ?? -1,
//                         Decoration.BuiltIn,
//                         (storage, execution) switch
//                         {
//                             (
//                                 StorageClass.Input,
//                                 ExecutionModel.Geometry
//                             )
//                                 => (int)BuiltIn.InvocationId,
//                             _ => throw new NotImplementedException()
//                         }
//                     );
//                 }
//                 else if (semantic == "SV_DomainLocation")
//                 {
//                     buffer.AddOpDecorate(
//                         i.ResultId ?? -1,
//                         Decoration.BuiltIn,
//                         (storage, execution) switch
//                         {
//                             (
//                                 StorageClass.Input,
//                                 ExecutionModel.TessellationEvaluation
//                             )
//                                 => (int)BuiltIn.TessCoord,
//                             _ => throw new NotImplementedException()
//                         }
//                     );
//                 }
//                 else if (semantic == "SV_PrimitiveID")
//                 {
//                     buffer.AddOpDecorate(
//                         i.ResultId ?? -1,
//                         Decoration.BuiltIn,
//                         (storage, execution) switch
//                         {
//                             (
//                                 StorageClass.Input,
//                                 ExecutionModel.TessellationControl
//                                 or ExecutionModel.TessellationEvaluation
//                                 or ExecutionModel.Geometry
//                                 or ExecutionModel.Fragment
//                             )
//                             or(
//                                 StorageClass.Output,
//                                 ExecutionModel.MeshEXT
//                                 or ExecutionModel.MeshNV
//                                 or ExecutionModel.Geometry
//                             )
//                                 => (int)BuiltIn.TessCoord,
//                             _ => throw new NotImplementedException()
//                         }
//                     );
//                 }
//                 else if (semantic == "SV_TessFactor")
//                 {
//                     buffer.AddOpDecorate(
//                         i.ResultId ?? -1,
//                         Decoration.BuiltIn,
//                         (storage, execution) switch
//                         {
//                             (
//                                 StorageClass.Input,
//                                 ExecutionModel.TessellationControl
//                             )
//                                 => (int)BuiltIn.TessLevelOuter,
//                             _ => throw new NotImplementedException()
//                         }
//                     );
//                 }
//                 else if (semantic == "SV_InsideTessFactor")
//                 {
//                     buffer.AddOpDecorate(
//                         i.ResultId ?? -1,
//                         Decoration.BuiltIn,
//                         (storage, execution) switch
//                         {
//                             (
//                                 StorageClass.Input,
//                                 ExecutionModel.TessellationControl
//                             )
//                                 => (int)BuiltIn.TessLevelInner,
//                             _ => throw new NotImplementedException()
//                         }
//                     );
//                 }
//                 else if (semantic == "SV_SampleIndex")
//                 {
//                     buffer.AddOpDecorate(
//                         i.ResultId ?? -1,
//                         Decoration.BuiltIn,
//                         (storage, execution) switch
//                         {
//                             (
//                                 StorageClass.Input,
//                                 ExecutionModel.Fragment
//                             )
//                                 => (int)BuiltIn.SampleId,
//                             _ => throw new NotImplementedException()
//                         }
//                     );
//                 }
//                 else if (semantic == "SV_StencilRef")
//                 {
//                     buffer.AddOpDecorate(
//                         i.ResultId ?? -1,
//                         Decoration.BuiltIn,
//                         (storage, execution) switch
//                         {
//                             (
//                                 StorageClass.Output,
//                                 ExecutionModel.Fragment
//                             )
//                                 => (int)BuiltIn.FragStencilRefEXT,
//                             _ => throw new NotImplementedException()
//                         }
//                     );
//                 }
//                 else if (semantic == "SV_Barycentrics")
//                 {
//                     buffer.AddOpDecorate(
//                         i.ResultId ?? -1,
//                         Decoration.BuiltIn,
//                         (storage, execution) switch
//                         {
//                             (
//                                 StorageClass.Input,
//                                 ExecutionModel.Fragment
//                             )
//                                 => (int)BuiltIn.BaryCoordKHR,
//                             _ => throw new NotImplementedException()
//                         }
//                     );
//                 }
//                 else if (semantic == "SV_RenderTargetArrayIndex")
//                 {
//                     buffer.AddOpDecorate(
//                         i.ResultId ?? -1,
//                         Decoration.BuiltIn,
//                         (storage, execution) switch
//                         {
//                             (
//                                 StorageClass.Input,
//                                 ExecutionModel.Fragment
//                             )
//                             or
//                             (
//                                 StorageClass.Output,
//                                 ExecutionModel.Geometry
//                                 or ExecutionModel.MeshEXT
//                                 or ExecutionModel.MeshNV
//                             )
//                                 => (int)BuiltIn.Layer,
//                             _ => throw new NotImplementedException()
//                         }
//                     );
//                 }
//                 else if (semantic == "SV_ViewportArrayIndex")
//                 {
//                     buffer.AddOpDecorate(
//                         i.ResultId ?? -1,
//                         Decoration.BuiltIn,
//                         (storage, execution) switch
//                         {
//                             (
//                                 StorageClass.Input,
//                                 ExecutionModel.Fragment
//                             )
//                             or
//                             (
//                                 StorageClass.Output,
//                                 ExecutionModel.Geometry
//                                 or ExecutionModel.MeshEXT
//                                 or ExecutionModel.MeshNV
//                             )
//                                 => (int)BuiltIn.ViewportIndex,
//                             _ => throw new NotImplementedException()
//                         }
//                     );
//                 }
//                 else if (semantic == "SV_Coverage")
//                 {
//                     buffer.AddOpDecorate(
//                         i.ResultId ?? -1,
//                         Decoration.BuiltIn,
//                         (storage, execution) switch
//                         {
//                             (
//                                 StorageClass.Input or StorageClass.Output,
//                                 ExecutionModel.Fragment
//                             )
//                                 => (int)BuiltIn.SampleMask,
//                             _ => throw new NotImplementedException()
//                         }
//                     );
//                 }
//                 else if (semantic == "SV_InnerCoverage")
//                 {
//                     buffer.AddOpDecorate(
//                         i.ResultId ?? -1,
//                         Decoration.BuiltIn,
//                         (storage, execution) switch
//                         {
//                             (
//                                 StorageClass.Input,
//                                 ExecutionModel.Fragment
//                             )
//                                 => (int)BuiltIn.FullyCoveredEXT,
//                             _ => throw new NotImplementedException()
//                         }
//                     );
//                 }
//                 else if (semantic == "SV_ViewID")
//                 {
//                     buffer.AddOpDecorate(
//                         i.ResultId ?? -1,
//                         Decoration.BuiltIn,
//                         (storage, execution) switch
//                         {
//                             (
//                                 StorageClass.Input,
//                                 ExecutionModel.Vertex
//                                 or ExecutionModel.TessellationControl
//                                 or ExecutionModel.TessellationEvaluation
//                                 or ExecutionModel.Geometry
//                                 or ExecutionModel.Fragment
//                                 or ExecutionModel.MeshEXT
//                                 or ExecutionModel.MeshNV
//                             )
//                                 => (int)BuiltIn.ViewIndex,
//                             _ => throw new NotImplementedException()
//                         }
//                     );
//                 }
//                 else if (semantic == "SV_ShadingRate")
//                 {
//                     buffer.AddOpDecorate(
//                         i.ResultId ?? -1,
//                         Decoration.BuiltIn,
//                         (storage, execution) switch
//                         {
//                             (
//                                 StorageClass.Output,
//                                 ExecutionModel.Vertex
//                                 or ExecutionModel.Geometry
//                                 or ExecutionModel.MeshEXT
//                                 or ExecutionModel.MeshNV
//                             )
//                                 => (int)BuiltIn.PrimitiveShadingRateKHR,
//                             (
//                                 StorageClass.Input,
//                                 ExecutionModel.Fragment
//                             )
//                                 => (int)BuiltIn.ShadingRateKHR,
//                             _ => throw new NotImplementedException()
//                         }
//                     );
//                 }
//                 else if (semantic == "SV_CullPrimitive")
//                 {
//                     buffer.AddOpDecorate(
//                         i.ResultId ?? -1,
//                         Decoration.BuiltIn,
//                         (storage, execution) switch
//                         {
//                             (
//                                 StorageClass.Output,
//                                 ExecutionModel.MeshEXT
//                                 or ExecutionModel.MeshNV
//                             )
//                                 => (int)BuiltIn.CullPrimitiveEXT,
//                             _ => throw new NotImplementedException()
//                         }
//                     );
//                 }
//                 else 
//                 {
//                     buffer.AddOpDecorate(
//                         i.ResultId ?? -1,
//                         Decoration.Location,
//                         (storage, execution) switch
//                         {
//                             (StorageClass.Input, _)
//                                 => ++inputLocation,
//                             (StorageClass.Output, _)
//                                 => ++outputLocation,
//                             _ => throw new NotImplementedException()
//                         }
//                     );
//                 }
//             }
//         }
//     }
// }