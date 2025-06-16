using Stride.Shaders.Core;
using Stride.Shaders.Experiments;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Tools;
using static Stride.Shaders.Spirv.Specification;

//Examples.CompileSDSL();
Examples.MergeSDSL();
// Examples.TryAllFiles();
// Examples.CreateShader();

var buffer = new SpirvBuffer(32);
var t_int = buffer.AddOpTypeInt(1, 32, 0);
InstOpTypeStruct tstr = buffer.AddOpTypeStruct(3, [t_int, t_int]);
InstOpExecutionMode tmode = buffer.AddOpExecutionMode(4, ExecutionMode.LocalSize);
tmode.Mode = ExecutionMode.Invocations;
Console.WriteLine(tmode.Mode);
