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
    public static void AddUnique(this List<(string Name, string Type)> list, string name, string type)
    {
        if (list.Any(x => x.Name == name))
            list.Add(($"{name}{list.Where(x => x.Name.StartsWith(name)).Count()}", type));
        else
            list.Add((name, type));
    }
}

public partial class SPVGenerator
{
    public static string ConvertQuantifier(string quant)
    {
        if (quant == "*")
            return "ZeroOrMore";
        else if (quant == "?")
            return "ZeroOrOne";
        else return "One";
    }
    public static string ConvertNameQuantToName(string name, string quant)
    {
        return (name, quant) switch
        {
            (_, "*") => "values",
            ("event", _) => "eventId",
            ("string", _) => "value",
            ("base", _) => "baseId",
            ("object", _) => "objectId",
            ("default", _) => "defaultId",
            _ => name.Replace("'", "").ToLowerInvariant()
        };
    }

    public static void PreProcessOperands(InstructionData op, Dictionary<string, OpKind> operandKinds, List<(string Name, string Type)> parameters)
    {
        var opname = op.OpName;
        if (op.Operands?.AsArray() is OperandData[] operands)
        {
            for (int i = 0; i < operands.Length; i++)
            {
                var e = operands[i];
                var kind = e.Kind;
                var realKind = ConvertKind(kind!, operandKinds);
                if (e.Quantifier is not null)
                {
                    if (e.Name is string name)
                    {
                        if (e.Quantifier == "?")
                            parameters.AddUnique(ConvertOperandName(name), $"{realKind}?");
                        else if (e.Quantifier == "*")
                            parameters.AddUnique("values", $"Span<{realKind}>");
                    }
                    else
                    {
                        if (e.Quantifier == "?")
                            parameters.AddUnique(ConvertKindToName(kind!), $"{realKind}?");
                        else if (e.Quantifier == "*")
                            parameters.AddUnique("values", $"Span<{realKind}>");
                    }
                }
                else
                {
                    if (e.Name is not null)
                        parameters.AddUnique(ConvertOperandName(e.Name), realKind);
                    else if (kind == "IdResult" && opname == "OpExtInst")
                        parameters.AddUnique(ConvertKindToName(kind), $"{realKind}?");
                    else if (kind == "IdResultType" && opname == "OpExtInst")
                        parameters.AddUnique(ConvertKindToName(kind), $"{realKind}?");
                    else
                        parameters.AddUnique(ConvertKindToName(kind!), realKind);
                }
                e.TypeName = parameters.Last().Type;
                e.Name = parameters.Last().Name;
                e.Class = operandKinds[kind!].Category;
                operands[i] = e;
            }
        }
    }


    // TODO: Include this in the preprocessing of instructions
    public static List<string> ConvertOperandsToParameters(InstructionData op, Dictionary<string, OpKind> operandKinds)
    {
        var opname = op.OpName;
        List<string> parameters = [];
        if (op.Operands is EquatableArray<OperandData> operands)
        {
            foreach (var e in operands)
            {
                var kind = e.Kind;
                var realKind = ConvertKind(kind!, operandKinds);
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

    // TODO: Include this in the preprocessing of instructions
    public static List<string> ConvertOperandsToParameterNames(InstructionData op, Dictionary<string, OpKind> operandKinds)
    {
        var opname = op.OpName;
        var operands = op.Operands;
        List<string> parameters = new(op.Operands?.Count ?? 0);
        if (operands is not null)
            foreach (var e in operands)
            {
                var kind = e.Kind;
                var realKind = ConvertKind(kind!, operandKinds);
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

    public static string ConvertKind(string kind, Dictionary<string, OpKind> operandKinds)
    {
        var opKind = operandKinds[kind];

        return (opKind.Kind, opKind.Category) switch
        {
            ("LiteralInteger", _) => "LiteralInteger",
            ("LiteralFloat", _) => "LiteralFloat",
            ("LiteralString", _) => "LiteralString",
            (_, "BitEnum") => kind + "Mask",
            ("LiteralExtInstInteger", _) => "LiteralInteger",
            ("LiteralSpecConstantOpInteger", _) => "Op",
            _ => kind
        };
    }

    public static string ConvertKindToName(string kind, bool lower = true)
    {
        return (kind, lower) switch
        {
            ("IdRef", true) => "id",
            ("IdResult", true) => "resultId",
            ("IdResultType", true) => "resultType",
            ("IdRef", false) => "Id",
            ("IdResult", false) => "ResultId",
            ("IdResultType", false) => "ResultType",
            (_, true) => kind.ToLower(),
            (_, false) => kind
        };
    }

    public static string ConvertOperandName(string input, string? quant = null, bool lower = true)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }
        var result = new StringBuilder();
        bool firstLetterHit = false;
        for (int i = 0; i < input.Length; i++)
        {

            if (char.IsLetterOrDigit(input[i]) || input[i] == '_')
            {
                if (!firstLetterHit)
                {
                    firstLetterHit = true;
                    if (lower)
                        result.Append(char.ToLowerInvariant(input[i]));
                    else
                        result.Append(input[i]);
                }
                else
                    result.Append(input[i]);
            }

        }
        return (result.ToString(), quant) switch
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
            (string v, _) => v
        };
    }

}

