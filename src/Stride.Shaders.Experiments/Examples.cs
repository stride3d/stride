using CommunityToolkit.HighPerformance;
using Silk.NET.Shaderc;
using Silk.NET.SPIRV.Cross;
using Stride.Shaders.Compilers;
using Stride.Shaders.Compilers.Direct3D;
using Stride.Shaders.Compilers.SDSL;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Processing;
using Stride.Shaders.Spirv.Tools;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static Stride.Shaders.Spirv.Specification;
using SourceLanguage = Silk.NET.Shaderc.SourceLanguage;

namespace Stride.Shaders.Experiments;

public record struct TextPosition(int Line, int Character)
{
    public static implicit operator TextPosition((int, int) pos) => new TextPosition(pos.Item1, pos.Item2);
}
public static class ASTExtensions
{
    public static bool Intersects<N>(this N node, TextPosition position)
        where N : Node
    {

        if (
            position.Line + 1 >= node.Info.Line
            && position.Line + 1 <= node.Info.EndLine
            && position.Character + 1 >= node.Info.Column
            && position.Character + 1 < node.Info.Column + node.Info.Length
        )
        {
            return true;
        }
        return false;
    }
}

public static partial class Examples
{
    static uint[] words = [
            // Offset 0x00000000 to 0x0000016F
            0x03022307, 0x00050100, 0x00000E00,
            0x0C000000, 0x00000000, 0x11000200,
            0x01000000, 0x0E000300, 0x00000000,
            0x01000000, 0x0F000700, 0x04000000,
            0x01000000, 0x50534D61, 0x696E0000,
            0x02000000, 0x03000000, 0x10000300,
            0x01000000, 0x07000000, 0x03000300,
            0x05000000, 0x58020000, 0x05000600,
            0x02000000, 0x696E2E76, 0x61722E43,
            0x4F4C4F52, 0x00000000, 0x05000700,
            0x03000000, 0x6F75742E, 0x7661722E,
            0x53565F54, 0x41524745, 0x54000000,
            0x05000400, 0x01000000, 0x50534D61,
            0x696E0000, 0x47000400, 0x02000000,
            0x1E000000, 0x00000000, 0x47000400,
            0x03000000, 0x1E000000, 0x00000000,
            0x16000300, 0x04000000, 0x20000000,
            0x17000400, 0x05000000, 0x04000000,
            0x04000000, 0x20000400, 0x06000000,
            0x01000000, 0x05000000, 0x20000400,
            0x07000000, 0x03000000, 0x05000000,
            0x13000200, 0x08000000, 0x21000300,
            0x09000000, 0x08000000, 0x3B000400,
            0x06000000, 0x02000000, 0x01000000,
            0x3B000400, 0x07000000, 0x03000000,
            0x03000000, 0x36000500, 0x08000000,
            0x01000000, 0x00000000, 0x09000000,
            0xF8000200, 0x0A000000, 0x3D000400,
            0x05000000, 0x0B000000, 0x02000000,
            0x3E000300, 0x03000000, 0x0B000000,
            0xFD000100, 0x38000100
        ];
    public static void UseSpirvCross()
    {
        unsafe
        {
            var code = new SpirvTranslator(words.AsMemory());
            File.WriteAllBytes("shader.bin", words.SelectMany(x => BitConverter.GetBytes(x).Reverse()).ToArray());
            Console.WriteLine(code.Translate(Backend.Hlsl));
        }
    }
    public static void TranslateHLSL()
    {

        Console.WriteLine(SpirvOptimizer.CompileAssembly(DXCompiler.sampleCode, "PSMain", SourceLanguage.Hlsl, OptimizationLevel.Zero));
        Console.WriteLine(SpirvOptimizer.Translate(DXCompiler.sampleCode, "PSMain", SourceLanguage.Hlsl, Backend.Hlsl));
    }

    public static void CompileHLSL()
    {
        var dxc = new DXCompiler();
        dxc.Compile(DXCompiler.sampleCode, out var compiled);
    }
    public static void CompileOldHLSL()
    {

        var fxc = new FXCompiler();
        fxc.Compile(DXCompiler.sampleCode, out var compiled);
    }
    public static void SpvOpt()
    {

        // var spvopt = new SpirvOptimpizer();
        // spvopt.Optimize(words);
    }

    public static void ParseSDSL()
    {
        var text = MonoGamePreProcessor.OpenAndRun("./assets/SDSL/Test.sdsl");
        var parsed = SDSLParser.Parse(text);
        Console.WriteLine(parsed.AST);
        if (parsed.Errors.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            foreach (var e in parsed.Errors)
                Console.WriteLine(e);
        }
    }

    public static void TryAllFiles()
    {
        foreach (var f in Directory.EnumerateFiles("./assets/Stride/SDSL"))
        {
            // var text = File.ReadAllText(f);
            if (f.Contains("BasicMixin.sdsl"))
                continue;
            var preprocessed = MonoGamePreProcessor.OpenAndRun(f);
            var parsed = SDSLParser.Parse(preprocessed);
            if (parsed.Errors.Count > 0)
            {
                Console.WriteLine(preprocessed);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(string.Join("; ", parsed.Errors.Select(x => x.ToString())));
                Console.WriteLine(f);
                Console.ForegroundColor = ConsoleColor.White;
                break;
            }
            else
            {
                Console.WriteLine(f);
            }
        }
        Console.ForegroundColor = ConsoleColor.White;
    }
    static bool ComputeIntersection(TextPosition position, Node node, out Node n)
    {
        n = null!;
        if (node is ShaderFile sf)
        {
            foreach (var ns in sf.Namespaces)
                if (ns.Intersects(position))
                    return ComputeIntersection(position, ns, out n);
            foreach (var e in sf.RootDeclarations)
                if (e.Intersects(position))
                    return ComputeIntersection(position, e, out n);
        }
        else if (node is ShaderNamespace sn)
        {
            if (sn.Namespace is not null && sn.Namespace.Intersects(position))
            {
                n = sn.Namespace;
                return true;
            }
            foreach (var decl in sn.Declarations)
            {
                if (decl.Intersects(position))
                    return ComputeIntersection(position, decl, out n);
            }
        }
        else if (node is ShaderClass sc)
        {
            if (sc.Name.Intersects(position))
            {
                n = sc.Name;
                return true;
            }
            foreach (var parent in sc.Mixins)
                if (parent.Intersects(position))
                {
                    n = parent;
                    return true;
                }
            foreach (var e in sc.Elements)
                if (e.Intersects(position))
                    return ComputeIntersection(position, e, out n);
        }
        else if (node is ShaderMethod method)
        {
            if (method.Name.Intersects(position))
            {
                n = method.Name;
                return true;
            }
            foreach (var arg in method.Parameters)
                if (arg.Intersects(position))
                {
                    n = arg;
                    return true;
                }
            if (method.Body is not null)
                foreach (var s in method.Body.Statements)
                    if (s.Intersects(position))
                        return ComputeIntersection(position, s, out n);
        }
        return false;
    }

    class ShaderLoader : IExternalShaderLoader
    {
        public bool LoadExternalReference(string name, [MaybeNullWhen(false)] out byte[] bytecode)
        {
            var filename = $"./assets/SDSL/{name}.sdsl";
            if (!File.Exists(filename))
            {
                bytecode = null;
                return false;
            }
            var text = MonoGamePreProcessor.OpenAndRun($"./assets/SDSL/{name}.sdsl");
            var sdslc = new SDSLC();
            sdslc.ShaderLoader = this;
            return sdslc.Compile(text, out bytecode);
        }
    }

    public static void CompileSDSL()
    {
        var text = MonoGamePreProcessor.OpenAndRun("./assets/SDSL/TestBasic.sdsl");

        var sdslc = new SDSLC
        {
            ShaderLoader = new ShaderLoader()
        };
        sdslc.Compile(text, out var bytecode);

        File.WriteAllBytes("TestBasic.sdspv", bytecode);
        var test = bytecode.AsMemory().Cast<byte, uint>().ToArray();
        var code = new SpirvTranslator(bytecode.AsMemory().Cast<byte, uint>());
        // Console.WriteLine(code.Translate(Backend.Hlsl));

    }

    public abstract class ShaderSource
    {
    }

    public struct ShaderMacro
    {
        public string Name;
        public string Definition;
    }

    public class ShaderMixinSource : ShaderSource
    {
        public List<ShaderClassCode> Mixins { get; } = [];

        public SortedList<string, ShaderSource> Compositions { get; } = [];

        public List<ShaderMacro> Macros { get; } = [];
    }

    public sealed class ShaderClassCode(string className) : ShaderSource
    {
        public string ClassName { get; } = className;
    }

    static Dictionary<string, NewSpirvBuffer> loadedShaders = new();

    static NewSpirvBuffer GetOrLoadShader(string name)
    {
        if (loadedShaders.TryGetValue(name, out var buffer))
            return buffer;

        new ShaderLoader().LoadExternalReference(name, out var bytecode);
        buffer = new NewSpirvBuffer(MemoryMarshal.Cast<byte, int>(bytecode));

        loadedShaders.Add(name, buffer);

        return buffer;
    }

    class ShaderInfo
    {
        public Dictionary<string, int> Functions { get; } = new();
        public Dictionary<string, int> Variables { get; } = new();
    }

    public static void MergeSDSL()
    {
        CompileSDSL();

        var shaderMixin = new ShaderMixinSource { Mixins = { new ShaderClassCode("TestBasic") } };

        var buffer = GetOrLoadShader("TestBasic");

        // Step: expand "for"
        // TODO

        // Step: build mixins: top level and (TODO) compose
        var inheritanceList = new List<string>();
        BuildInheritanceList(buffer, inheritanceList);
        inheritanceList.Add("TestBasic");

        var temp = new NewSpirvBuffer();
        var offset = 0;
        var nextOffset = 0;

        foreach (var shaderName in inheritanceList)
        {
            var shader = GetOrLoadShader(shaderName);
            offset += nextOffset;
            nextOffset = 0;
            Spv.Dis(shader, true);
            foreach (var i in shader)
            {
                var i2 = new OpData(i.Data.Memory.Span);
                temp.Add(i2);

                if (i.Data.IdResult != null && i.Data.IdResult.Value > nextOffset)
                    nextOffset = i.Data.IdResult.Value;

                if (offset > 0)
                    OffsetIds(i2, offset);
            }
            Spv.Dis(temp, true);
        }

        var shaders = new Dictionary<string, ShaderInfo>();
        ShaderInfo? currentShader = null;

        var names = new Dictionary<int, string>();
        var importedShaders = new Dictionary<int, ShaderInfo>();
        var idRemapping = new Dictionary<int, int>();
        foreach (var i in temp)
        {
            if (i.Data.Op == Op.OpName && (OpName)i is {} nameInstruction)
            {
                if (idRemapping.ContainsKey(nameInstruction.Target))
                    SetOpNop(i.Data.Memory.Span);
                else
                    names.Add(nameInstruction.Target, nameInstruction.Name);
            }
            else if (i.Data.Op == Op.OpSDSLShader && (OpSDSLShader)i is {} shaderInstruction)
            {
                currentShader = new ShaderInfo();
                var shaderName = shaderInstruction.ShaderName;
                shaders.Add(shaderName, currentShader);
                SetOpNop(i.Data.Memory.Span);
            }
            else if (i.Data.Op == Op.OpSDSLShaderEnd)
            {
                currentShader = null;
                importedShaders.Clear();
                SetOpNop(i.Data.Memory.Span);
            }
            else if (i.Data.Op == Op.OpSDSLMixinInherit)
            {
                SetOpNop(i.Data.Memory.Span);
            }

            if (i.Data.Op == Op.OpFunction && (OpFunction)i is {} function)
            {
                var functionName = names[function.ResultId];
                currentShader!.Functions.Add(functionName, function.ResultId);

                //temp.Remove(i.Position);
                //temp.InsertOpFunction(i.Position, i.ResultId.Value, i.ResultType!.Value, function.FunctionControl, function.FunctionType);
            }

            if (i.Data.Op == Op.OpVariable && (OpVariable)i is {} variable)
            {
                var variableName = names[variable.ResultId];
                currentShader!.Variables.Add(variableName, variable.ResultId);
            }

            if (i.Data.Op == Op.OpSDSLImportShader && (OpSDSLImportShader)i is {} importShader)
            {
                importedShaders.Add(importShader.ResultId, shaders[importShader.ShaderName]);

                SetOpNop(i.Data.Memory.Span);
            }
            else if (i.Data.Op == Op.OpSDSLImportVariable && (OpSDSLImportVariable)i is {} importVariable)
            {
                var importedShader = importedShaders[importVariable.Shader];

                var importedVariable = importedShader.Variables[importVariable.VariableName];

                idRemapping.Add(importVariable.ResultId, importedVariable);

                SetOpNop(i.Data.Memory.Span);
            }
            else if (i.Data.Op == Op.OpSDSLImportFunction && (OpSDSLImportFunction)i is {} importFunction)
            {
                var importedShader = importedShaders[importFunction.Shader];
                var importedFunction = importedShader.Functions[importFunction.FunctionName];
                idRemapping.Add(importFunction.ResultId, importedFunction);

                SetOpNop(i.Data.Memory.Span);
            }

            foreach (var op in i.Data)
            {
                if ((op.Kind == OperandKind.IdRef
                        || op.Kind == OperandKind.IdResultType
                        || op.Kind == OperandKind.PairIdRefLiteralInteger
                        || op.Kind == OperandKind.PairIdRefIdRef)
                    && op.Words.Length > 0
                    && idRemapping.TryGetValue(op.Words[0], out var to1))
                    op.Words[0] = to1;
                if ((op.Kind == OperandKind.PairLiteralIntegerIdRef
                     || op.Kind == OperandKind.PairIdRefIdRef)
                    && idRemapping.TryGetValue(op.Words[1], out var to2))
                    op.Words[1] = to2;
            }
        }

        Console.WriteLine("Done SDSL importing");
        Spv.Dis(temp, true);

        // Step: merge mixins
        //       start from most-derived class and import on demand
        // Step: analyze streams and generate in/out variables

        new TypeDuplicateRemover().Apply(temp);

        Console.WriteLine("Done type remapping");
        Spv.Dis(temp, true);

        var context = new SpirvContext(new());
        context.Bound = offset + nextOffset + 1;
        Spv.Dis(temp, true);
        ShaderClass.ProcessNameAndTypes(temp, out var names2, out var types);
        foreach (var i in temp)
        {
            if (i.Data.Op == Op.OpFunction && (OpFunction)i is {} function)
            {
                var functionName = names2[function.ResultId];
                context.Module.Functions.Add(functionName, new SpirvFunction(function.ResultId, functionName, (FunctionType)types[function.FunctionType]));
            }
        }

        foreach (var type in types)
        {
            context.Types.Add(type.Value, type.Key);
            context.ReverseTypes.Add(type.Key, type.Value);
        }

        context.Insert(0, new OpCapability(Capability.Shader));
        context.Insert(1, new OpMemoryModel(AddressingModel.Logical, MemoryModel.GLSL450));
        context.Insert(2, new OpExtension("SPV_GOOGLE_hlsl_functionality1"));
        new StreamAnalyzer().Process(temp, context);

        foreach (var inst in context.GetBuffer())
            temp.Add(inst.Data);

        new TypeDuplicateRemover().Apply(temp);
        for (int i = 0; i < temp.Count; i++)
        {
            if (temp[i].Op == Op.OpNop)
                temp.RemoveAt(i--);
        }

        var source = Spv.Dis(temp, true);

        File.WriteAllText("test.spvdis", source);
    }

    public static void OffsetIds(OpData inst, int offset)
    {
        foreach (var o in inst)
        {
            if (o.Kind == OperandKind.IdRef
                || o.Kind == OperandKind.IdResult
                || o.Kind == OperandKind.IdResultType)
            {
                for (int i = 0; i < o.Words.Length; ++i)
                    o.Words[i] += offset;
            }
            else if (o.Kind == OperandKind.PairIdRefLiteralInteger
                     || o.Kind == OperandKind.PairLiteralIntegerIdRef
                     || o.Kind == OperandKind.PairIdRefIdRef)
            {
                for (int i = 0; i < o.Words.Length; i += 2)
                {
                    if (o.Kind == OperandKind.PairIdRefLiteralInteger || o.Kind == OperandKind.PairIdRefIdRef)
                        o.Words[i * 2 + 0] += offset;
                    if (o.Kind == OperandKind.PairLiteralIntegerIdRef || o.Kind == OperandKind.PairIdRefIdRef)
                        o.Words[i * 2 + 1] += offset;
                }
            }
        }
    }

    static void SetOpNop(Span<int> words)
    {
        words[0] = words.Length << 16;
        words[1..].Clear();
    }

    private static void BuildInheritanceList(NewSpirvBuffer buffer, List<string> inheritanceList)
    {
        // Build shader name mapping
        var shaderMapping = new Dictionary<int, string>();
        foreach (var i in buffer)
            if (i.Op == Specification.Op.OpSDSLImportShader && (OpSDSLImportShader) i is {} importShader)
                shaderMapping[importShader.ResultId] = importShader.ShaderName;

        // Check inheritance
        foreach (var i in buffer)
        {
            if (i.Op == Specification.Op.OpSDSLMixinInherit && (OpSDSLMixinInherit)i is {} inherit)
            {
                var shaderName = shaderMapping[inherit.Shader];
                var shader = GetOrLoadShader(shaderName);
                BuildInheritanceList(shader, inheritanceList);
                inheritanceList.Add(shaderName);
            }
        }
    }
}


