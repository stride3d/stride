using Stride.Shaders.Compilers.SDSL;
using Stride.Shaders.Experiments;
using Stride.Shaders.Spirv.Core.Buffers;
using System.Runtime.InteropServices;
using Stride.Shaders.Spirv.Tools;
using Stride.Shaders.Compilers.Direct3D;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders;

Console.WriteLine(Spv2DXIL.spirv_to_dxil_get_version());

// Examples.CompileSDSL("RenderTests/If");

//Examples.CompileSDSL();
var loader = new Examples.ShaderLoader();
loader.LoadExternalBuffer("Test", [], out var testBuffer, out _, out _);
var shaderMixer = new ShaderMixer(loader);
shaderMixer.MergeSDSL(new ShaderClassSource("If"), new ShaderMixer.Options(), out var bytecode, out _, out _, out _);

using var buffer = SpirvBytecode.CreateBufferFromBytecode(bytecode);
var source = Spv.Dis(buffer);
File.WriteAllText("test.spvdis", source);


// Examples.TryAllFiles();
// Examples.CreateShader();

// Examples.GenerateSpirv();
// Examples.CreateNewShader();