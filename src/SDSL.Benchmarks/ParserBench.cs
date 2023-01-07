using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using Stride.Core.Shaders.Grammar.Stride;
using Stride.Core.Shaders.Parser;

namespace SDSL.Benchmarks;

public class AntiVirusFriendlyConfig : ManualConfig
{
    public AntiVirusFriendlyConfig()
    {
        AddJob(Job.MediumRun
            .WithToolchain(InProcessNoEmitToolchain.Instance));
    }
}

[Config(typeof(AntiVirusFriendlyConfig))]
[MemoryDiagnoser]
public class ParserBench
{
    public string shaderText = File.ReadAllText(@"C:\Users\kafia\source\repos\SDSLParser\src\SDSL.Benchmarks\shader.sdsl");
    ShaderParser strideParser =  ShaderParser.GetParser<StrideGrammar>();
    Parsing.ShaderMixinParser etoParser =  new();

    [Benchmark]
    public void StrideParse()
    {
        strideParser.PreProcessAndParse(shaderText, "");
    }
    [Benchmark]
    public void EtoParse()
    {
        etoParser.Parse(shaderText);
    }
}
