using Stride.Core.Diagnostics;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Processing.Interfaces;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Compilers.SDSL;

public partial class ShaderMixer
{
    /// <summary>
    /// Reports operations that need screen-space derivatives from a stage that cannot compute them:
    /// texture sampling at an implicit level of detail, the derivative instructions behind
    /// <c>ddx</c>/<c>ddy</c>/<c>fwidth</c>, and <c>CalculateLevelOfDetail</c>. SPIR-V restricts all of
    /// them to Fragment/GLCompute/Mesh/Task, and fxc (error X4532) and dxc reject the equivalent HLSL, so
    /// this is diagnosed here instead of failing later as an opaque optimizer error while legalizing for
    /// HLSL.
    /// </summary>
    private static bool ValidateDerivativeUsage(SpirvContext context, SpirvBuffer buffer, List<InterfaceProcessor.EntryPointInfo> entryPoints, ILogger log)
    {
        var sourceFiles = new Dictionary<int, string>();
        foreach (var i in context)
        {
            if (i.Op == Op.OpString)
            {
                OpString sourceFile = i;
                sourceFiles[sourceFile.ResultId] = sourceFile.Value;
            }
        }

        var callees = new Dictionary<int, List<int>>();
        var derivativeSites = new Dictionary<int, List<(string? Location, Op Op)>>();
        var currentFunction = 0;
        string? currentLocation = null;

        foreach (var i in buffer)
        {
            switch (i.Op)
            {
                case Op.OpFunction:
                    currentFunction = ((OpFunction)i).ResultId;
                    currentLocation = null;
                    break;

                case Op.OpLine:
                    OpLine line = i;
                    currentLocation = sourceFiles.TryGetValue(line.File, out var fileName)
                        ? $"{fileName}({line.Line},{line.Column})"
                        : null;
                    break;

                case Op.OpFunctionCall:
                    if (!callees.TryGetValue(currentFunction, out var calls))
                        callees.Add(currentFunction, calls = []);
                    calls.Add(((OpFunctionCall)i).Function);
                    break;

                case Op.OpImageSampleImplicitLod:
                case Op.OpImageSampleDrefImplicitLod:
                case Op.OpImageSampleProjImplicitLod:
                case Op.OpImageSampleProjDrefImplicitLod:
                case Op.OpImageQueryLod:
                case Op.OpDPdx:
                case Op.OpDPdy:
                case Op.OpFwidth:
                case Op.OpDPdxFine:
                case Op.OpDPdyFine:
                case Op.OpFwidthFine:
                case Op.OpDPdxCoarse:
                case Op.OpDPdyCoarse:
                case Op.OpFwidthCoarse:
                    if (!derivativeSites.TryGetValue(currentFunction, out var sites))
                        derivativeSites.Add(currentFunction, sites = []);
                    sites.Add((currentLocation, i.Op));
                    break;
            }
        }

        if (derivativeSites.Count == 0)
            return true;

        var reported = new HashSet<string>();
        foreach (var entryPoint in entryPoints)
        {
            if (entryPoint.Stage is not (ShaderStage.Vertex or ShaderStage.Hull or ShaderStage.Domain or ShaderStage.Geometry))
                continue;

            var visited = new HashSet<int>();
            var pending = new Stack<int>();
            pending.Push(entryPoint.Id);

            while (pending.TryPop(out var functionId))
            {
                if (!visited.Add(functionId))
                    continue;

                if (derivativeSites.TryGetValue(functionId, out var sites))
                {
                    foreach (var (site, op) in sites)
                    {
                        var location = site is null ? string.Empty : $"{site}: ";
                        reported.Add($"{location}{DescribeDerivativeUse(op)} A {entryPoint.Stage} shader reaches this code.");
                    }
                }

                if (callees.TryGetValue(functionId, out var calls))
                {
                    foreach (var callee in calls)
                        pending.Push(callee);
                }
            }
        }

        foreach (var message in reported)
            log.Error(message);

        return reported.Count == 0;
    }

    private static string DescribeDerivativeUse(Op op) => op switch
    {
        Op.OpImageQueryLod => "CalculateLevelOfDetail needs a pixel or compute shader, because it derives the level of detail from screen-space derivatives.",
        Op.OpDPdx or Op.OpDPdy or Op.OpFwidth
            or Op.OpDPdxFine or Op.OpDPdyFine or Op.OpFwidthFine
            or Op.OpDPdxCoarse or Op.OpDPdyCoarse or Op.OpFwidthCoarse
            => "A screen-space derivative (ddx, ddy or fwidth) needs a pixel or compute shader.",
        _ => "A texture sample with an implicit level of detail needs a pixel or compute shader. Use SampleLevel, SampleGrad or SampleCmpLevelZero to give an explicit level of detail.",
    };
}
