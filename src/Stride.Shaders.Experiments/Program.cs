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
Examples.CreateShader();

var buffer = new SpirvBuffer(32);
var t_int = buffer.AddOpTypeInt(1, 32, 0);
buffer.AddOpTypeStruct(2, [t_int, t_int]);

new SpirvDis<SpirvBuffer>(buffer).Disassemble(true);