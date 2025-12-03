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
loader.LoadExternalFile("Test", out var testBuffer);
var shaderMixer = new ShaderMixer(loader);
shaderMixer.MergeSDSL(new ShaderClassSource("If"), out var bytecode, out _);
var buffer = new NewSpirvBuffer(MemoryMarshal.Cast<byte, int>(bytecode.AsSpan()));
var source = Spv.Dis(buffer);
File.WriteAllText("test.spvdis", source);


// Examples.TryAllFiles();
// Examples.CreateShader();

// Examples.GenerateSpirv();
// Examples.CreateNewShader();