using Stride.Shaders.Core;
using Stride.Shaders.Experiments;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;

Examples.CompileSDSL();
// Examples.TryAllFiles();
// Examples.CreateShader();

var buffer = new SpirvBuffer(32);

var i = buffer.AddOpTypeFloat(0, 32, null);
var fl = i.UnsafeAs<RefOpTypeFloat>();
Console.WriteLine(fl.Width);