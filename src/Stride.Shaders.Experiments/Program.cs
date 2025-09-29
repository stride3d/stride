using Stride.Shaders.Compilers.SDSL;
using Stride.Shaders.Experiments;
using Stride.Shaders.Spirv.Core.Buffers;
using System.Runtime.InteropServices;
using Stride.Shaders.Spirv.Tools;

Examples.TranslateHLSL();

//Examples.CompileSDSL();
var shaderMixer = new ShaderMixer(new Examples.ShaderLoader());
shaderMixer.MergeSDSL("TestBasic", out var bytecode);
var buffer = new NewSpirvBuffer(MemoryMarshal.Cast<byte, int>(bytecode));
var source = Spv.Dis(buffer, true);
File.WriteAllText("test.spvdis", source);


// Examples.TryAllFiles();
// Examples.CreateShader();

// Examples.GenerateSpirv();
// Examples.CreateNewShader();