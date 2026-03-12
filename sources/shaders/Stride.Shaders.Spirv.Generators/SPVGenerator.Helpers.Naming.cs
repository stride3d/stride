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
            ("event", _) => "eventId",
            ("string", _) => "value",
            ("base", _) => "baseId",
            ("object", _) => "objectId",
            ("default", _) => "defaultId",
            _ => name.Replace("'", "").ToLowerInvariant()
        };
    }

    /// <summary>
    /// Extracts the first quoted name from a SPIR-V operand name that may contain
    /// multi-value documentation like "'Member 0 type', +\n'member 1 type', +\n...".
    /// Strips digit suffixes to produce a clean base name (e.g., "Member 0 type" → "Member type").
    /// </summary>
    static string ExtractFirstOperandName(string name)
    {
        // Take up to the first comma or + that separates multiple values
        var commaIdx = name.IndexOf(',');
        var plusIdx = name.IndexOf('+');
        var newlineIdx = name.IndexOf('\n');
        var end = name.Length;
        if (commaIdx > 0) end = Math.Min(end, commaIdx);
        if (plusIdx > 0) end = Math.Min(end, plusIdx);
        if (newlineIdx > 0) end = Math.Min(end, newlineIdx);
        var first = name[..end].Trim().Trim('\'').Trim();

        // Strip digit suffixes and capitalize each word for CamelCase
        // e.g. "Member 0 type" → "MemberTypes", "Argument 0" → "Arguments"
        // If a digit was removed, pluralize by adding "s" at the end
        var sb = new StringBuilder();
        var parts = first.Split(' ');
        bool hadDigit = false;
        foreach (var part in parts)
        {
            if (part.Length > 0 && part.All(char.IsDigit))
            {
                hadDigit = true;
                continue;
            }
            sb.Append(char.ToUpperInvariant(part[0]));
            sb.Append(part[1..]);
        }
        return sb.Length > 0 ? sb.ToString() : first;
    }

    public static void PreProcessOperands(InstructionData op, Dictionary<string, OpKind> operandKinds, List<(string Name, string Type)> parameters)
    {
        var opname = op.OpName;
        if (op.Operands?.AsList() is List<OperandData> operands)
        {
            if (op.OpName.EndsWith("DecorateString"))
                operands.Add(new() { Kind = "LiteralString", Name = "value" });
            else if (op.OpName.EndsWith("DecorateId"))
                operands.Add(new() { Kind = "IdRef", Name = "value" });
            bool computable = true;
            for (int i = 0; i < operands.Count; i++)
            {
                var e = operands[i];
                e.IsIndexKnown = computable;
                computable = (computable, e.Kind, e.Quantifier) switch
                {
                    (true, _, "*" or "+") => false,
                    (true, "LiteralString", _) => false,
                    _ => computable
                };
                var kind = e.Kind;
                var realKind = ConvertKind(kind, operandKinds);
                if (e.Quantifier is not null)
                {
                    if (e.Name is string name)
                    {
                        if (e.Quantifier == "?")
                            parameters.AddUnique(ConvertOperandName(name), $"{realKind}?");
                        else if (e.Quantifier == "*")
                            parameters.AddUnique(ConvertOperandName(ExtractFirstOperandName(name), "*"), $"Span<{realKind}>");
                    }
                    else
                    {
                        if (e.Quantifier == "?")
                            parameters.AddUnique(ConvertKindToName(kind!), $"{realKind}?");
                        else if (e.Quantifier == "*")
                            parameters.AddUnique(ConvertKindToName(kind!), $"Span<{realKind}>");
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
                        parameters.AddUnique(ConvertKindToName(kind), realKind);
                }
                e.TypeName = parameters.Last().Type;
                e.Name = parameters.Last().Name;
                e.Class = operandKinds[kind].Category;
                if (
                    !op.OpName.EndsWith("DecorateString")
                    && !op.OpName.EndsWith("DecorateId")
                    && operandKinds[kind].Enumerants?.AsList() is List<Enumerant> enumerants
                    && enumerants.Any(x => x.Parameters?.AsList() is List<EnumerantParameter> { Count: > 0 })
                )
                    e.IsParameterized = true;
                operands[i] = e;
            }
        }
    }


    // TODO: Include this in the preprocessing of instructions
    public static List<string> ConvertOperandsToParameters(InstructionData op, Dictionary<string, OpKind> operandKinds)
    {
        var opname = op.OpName;
        List<string> parameters = [];
        if (op.Operands is EquatableList<OperandData> operands)
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
                            parameters.AddUnique("Span<" + realKind + "> " + ConvertOperandName(ExtractFirstOperandName(name), "*"));
                    }
                    else
                    {
                        if (e.Quantifier == "?")
                            parameters.AddUnique(realKind + "? " + ConvertKindToName(kind!));
                        else if (e.Quantifier == "*")
                            parameters.AddUnique("Span<" + realKind + "> " + ConvertKindToName(kind!));
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
                            parameters.AddUnique(ConvertOperandName(ExtractFirstOperandName(name), "*"));
                    }
                    else
                    {
                        if (quant == "?")
                            parameters.AddUnique(ConvertKindToName(kind!));
                        else if (quant == "*")
                            parameters.AddUnique(ConvertKindToName(kind!));
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
            (_, true) => LowerFirst(kind),
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
        var name = result.ToString() switch
        {
            "event" => "eventId",
            "string" => "value",
            "base" => "baseId",
            "object" => "objectId",
            "default" => "defaultId",
            "interface" => "interfaceId",
            "IdResult" => "resultId",
            "IdResultType" => "resultType",
            "IdRef" => "id",
            "LiteralInteger" => "",
            "LiteralFloat" => "",
            "LiteralString" => "",
            "Dim" => "",
            "ImageFormat" => "",
            "ExecutionMode" => "",
            "ExecutionModel" => "",
            string v => v
        };
        if (quant == "*" && name.Length > 0 && !name.EndsWith("s"))
            name += "s";
        return name;
    }
    static string LowerFirst(string s)
        => char.IsLower(s[0]) ? s : $"{char.ToLowerInvariant(s[0])}{s[1..]}";

}
