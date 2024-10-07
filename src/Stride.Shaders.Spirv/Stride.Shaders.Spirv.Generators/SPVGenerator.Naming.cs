using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Security.Claims;

namespace Stride.Shaders.Spirv.Generators
{

    public static class ListExtensions
    {
        public static void AddUnique(this List<string> list, string name)
        {
            if (list.Contains(name))
                list.Add(name + list.Where(x => x.StartsWith(name)).Count());
            else
                list.Add(name);
        }
    }

    public partial class SPVGenerator
    {


        public static List<string> ConvertOperandsToParameters(JsonElement op)
        {
            var opname = op.GetProperty("opname").GetString();
            var operands = op.GetProperty("operands").EnumerateArray();
            List<string> parameters = new();
            foreach (var e in operands)
            {
                var kind = e.GetProperty("kind").GetString();
                var realKind = ConvertKind(kind);
                if (e.TryGetProperty("quantifier", out var quant))
                {
                    if (e.TryGetProperty("name", out var name))
                    {
                        if (quant.GetString() == "?")
                            parameters.AddUnique(realKind + "? " + ConvertOperandName(name.GetString()));
                        else if (quant.GetString() == "*")
                            parameters.AddUnique("Span<" + realKind + "> values");
                    }
                    else
                    {
                        if (quant.GetString() == "?")
                            parameters.AddUnique(realKind + "? " + ConvertKindToName(kind));
                        else if (quant.GetString() == "*")
                            parameters.AddUnique("Span<" + realKind + "> values");
                    }
                }
                else
                {
                    if (e.TryGetProperty("name", out var name))
                        parameters.AddUnique(realKind + " " + ConvertOperandName(name.GetString()));
                    else if(kind == "IdResult" && opname == "OpExtInst")
                        parameters.AddUnique(realKind + "? " + ConvertKindToName(kind));
                    else if (kind == "IdResultType" && opname == "OpExtInst")
                        parameters.AddUnique(realKind + "? " + ConvertKindToName(kind));
                    else
                        parameters.AddUnique(realKind + " " + ConvertKindToName(kind));
                }
            }
            if(parameters.Any(x => x.Contains("resultType")) && parameters.Any(x => x.Contains("resultId")))
            {
                var resultType = parameters[0];
                var resultId = parameters[1];
                parameters[0] = resultId;
                parameters[1] = resultType;
            }
            return parameters;
        }

        public static List<string> ConvertOperandsToParameterNames(JsonElement op)
        {
            var opname = op.GetProperty("opname").GetString();
            var operands = op.GetProperty("operands").EnumerateArray();
            List<string> parameters = new(op.GetProperty("operands").GetArrayLength());
            foreach (var e in operands)
            {
                var kind = e.GetProperty("kind").GetString();
                var realKind = ConvertKind(kind);
                if (e.TryGetProperty("quantifier", out var quant))
                {
                    if (e.TryGetProperty("name", out var name))
                    {
                        if (quant.GetString() == "?")
                            parameters.AddUnique(ConvertOperandName(name.GetString()));
                        else if (quant.GetString() == "*")
                            parameters.AddUnique("values");
                    }
                    else
                    {
                        if (quant.GetString() == "?")
                            parameters.AddUnique(ConvertKindToName(kind));
                        else if (quant.GetString() == "*")
                            parameters.AddUnique("values");
                    }
                }
                else
                {
                    if (e.TryGetProperty("name", out var name))
                        parameters.AddUnique(ConvertOperandName(name.GetString()));
                    else
                        parameters.AddUnique(ConvertKindToName(kind));
                }
            }
            return parameters;
        }

        public static string ConvertKind(string kind)
        {
            return kind switch
            {
                "LiteralInteger" => "LiteralInteger",
                "LiteralFloat" => "LiteralFloat",
                "LiteralString" => "LiteralString",
                "ImageOperands" => "ImageOperandsMask",
                "RawAccessChainOperands" => "RawAccessChainOperandsMask",
                "FunctionControl" => "FunctionControlMask",
                "MemoryAccess" => "MemoryAccessMask",
                "LoopControl" => "LoopControlMask",
                "SelectionControl" => "SelectionControlMask",
                "LiteralExtInstInteger" => "LiteralInteger",
                "LiteralSpecConstantOpInteger" => "Op",
                "CooperativeMatrixOperands" => "CooperativeMatrixOperandsMask",
                _ => kind
            };
        }

        public static string ConvertKindToName(string kind)
        {
            return kind switch
            {
                "IdRef" => "id",
                "IdResult" => "resultId",
                "IdResultType" => "resultType",
                _ => kind.ToLower()
            };
        }

        public static string ConvertOperandName(string input, string quant = null)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            var result = "";
            bool firstLetterHit = false;
            for (int i = 0; i < input.Length; i++)
            {

                if (char.IsLetterOrDigit(input[i]) || input[i] == '_')
                {
                    if (!firstLetterHit)
                    {
                        firstLetterHit = true;
                        result += char.ToLowerInvariant(input[i]);
                    }
                    else
                        result += input[i];
                }

            }
            return (result, quant) switch
            {
                ("event", _) => "eventId",
                ("string", _) => "value",
                ("base", _) => "baseId",
                ("object", _) => "objectId",
                ("default", _) => "defaultId",
                ("IdResult", _) => "resultId",
                ("IdResultType", _) => "resultType",
                ("IdRef", "*") => "id",
                ("IdRef", "?") => "id",
                ("IdRef", null) => "id",
                ("LiteralInteger", _) => "",
                ("LiteralFloat", _) => "",
                ("LiteralString", _) => "",
                ("Dim", _) => "",
                ("ImageFormat", _) => "",
                ("ExecutionMode", _) => "",
                ("ExecutionModel", _) => "",
                _ => result
            };
        }
    }
}
