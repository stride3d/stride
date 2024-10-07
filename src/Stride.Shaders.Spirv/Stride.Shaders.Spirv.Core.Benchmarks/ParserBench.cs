using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Parsing;
using CommunityToolkit.HighPerformance.Buffers;
using System.Runtime.InteropServices;

namespace Stride.Shaders.Spirv.Core.Benchmarks;

[MemoryDiagnoser]
public class ParserBench
{
    public MemoryOwner<int> shader;
    public List<Instruction> instructions;

    public ParserBench()
    {
        

    }


    // [Benchmark]
    // public void MemorySlice()
    // {
    //     var slice = shader.Memory[5..];
    // }
    // [Benchmark]
    // public void Count()
    // {
    //     var reader = new SpirvReader(shader);
    //     var count = reader.Count;
    // }

    // [Benchmark]
    // public void Parse()
    // {
    //     var reader = new SpirvReader(shader);
    //     foreach (var i in reader)
    //     {
            
    //     }
    // }
    // [Benchmark]
    // public void ParseToList()
    // {
    //     SpirvReader.ParseToList(shader, instructions);
    //     instructions.Clear();
    // }
}
