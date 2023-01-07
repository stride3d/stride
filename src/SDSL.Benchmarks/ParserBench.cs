using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using Stride.Core.Shaders.Grammar.Stride;
using Stride.Core.Shaders.Parser;

namespace SDSL.Benchmarks;

[MemoryDiagnoser]
public class ParserBench
{
    public string shaderText = File.ReadAllText(@"C:\Users\kafia\source\repos\SDSLParser\src\SDSL.Benchmarks\shader.sdsl");
    ShaderParser strideParser =  ShaderParser.GetParser<StrideGrammar>();
    Parsing.ShaderMixinParser etoParser =  new();

    [Benchmark]
    public void StrideParse()
    {
        strideParser.Parse(shaderText, "");
    }
    [Benchmark]
    public void EtoParse()
    {
        etoParser.Parse(shaderText);
    }
}
