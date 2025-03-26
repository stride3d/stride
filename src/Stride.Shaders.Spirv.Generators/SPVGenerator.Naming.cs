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

namespace Stride.Shaders.Spirv.Generators;


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


    public List<string> ConvertOperandsToParameters(InstructionData op)
    {
        var opname = op.OpName;
        var operands = op.Operands;
        List<string> parameters = [];
        foreach (var e in operands)
        {
            var kind = e.Kind;
            var realKind = ConvertKind(kind!);
            if (e.Quantifier is not null)
            {
                if (e.Name is string name)
                {
                    if (e.Quantifier == "?")
                        parameters.AddUnique(realKind + "? " + ConvertOperandName(name));
                    else if (e.Quantifier == "*")
                        parameters.AddUnique("Span<" + realKind + "> values");
                }
                else
                {
                    if (e.Quantifier == "?")
                        parameters.AddUnique(realKind + "? " + ConvertKindToName(kind!));
                    else if (e.Quantifier == "*")
                        parameters.AddUnique("Span<" + realKind + "> values");
                }
            }
            else
            {
                if (e.Name is not null)
                    parameters.AddUnique(realKind + " " + ConvertOperandName(e.Name));
                else if (kind == "IdResult" && opname == "OpExtInst")
                    parameters.AddUnique(realKind + "? " + ConvertKindToName(kind));
                else if (kind == "IdResultType" && opname == "OpExtInst")
                    parameters.AddUnique(realKind + "? " + ConvertKindToName(kind));
                else
                    parameters.AddUnique(realKind + " " + ConvertKindToName(kind!));
            }
        }
        if (parameters.Any(x => x.Contains("resultType")) && parameters.Any(x => x.Contains("resultId")))
        {
            var resultType = parameters[0];
            var resultId = parameters[1];
            parameters[0] = resultId;
            parameters[1] = resultType;
        }
        return parameters;
    }

    public List<string> ConvertOperandsToParameterNames(InstructionData op)
    {
        var opname = op.OpName;
        var operands = op.Operands;
        List<string> parameters = new(op.Operands.Count);
        foreach (var e in operands)
        {
            var kind = e.Kind;
            var realKind = ConvertKind(kind!);
            if (e.Quantifier is string quant)
            {
                if (e.Name is string name)
                {
                    if (quant == "?")
                        parameters.AddUnique(ConvertOperandName(name));
                    else if (quant == "*")
                        parameters.AddUnique("values");
                }
                else
                {
                    if (quant == "?")
                        parameters.AddUnique(ConvertKindToName(kind!));
                    else if (quant == "*")
                        parameters.AddUnique("values");
                }
            }
            else
            {
                if (e.Name is string name)
                    parameters.AddUnique(ConvertOperandName(name));
                else
                    parameters.AddUnique(ConvertKindToName(kind));
            }
        }
        return parameters;
    }

    public string ConvertKind(string kind)
    {
        var opKind = operandKinds[kind];

        return (opKind.Kind, opKind.Category) switch
        {
            ("LiteralInteger", _) => "LiteralInteger",
            ("LiteralFloat", _) => "LiteralFloat",
            ("LiteralString", _) => "LiteralString",
            ( _ , "BitEnum") => kind + "Mask", 
            ("LiteralExtInstInteger", _) => "LiteralInteger",
            ("LiteralSpecConstantOpInteger", _) => "Op",
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

    public static string ConvertOperandName(string input, string? quant = null)
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

