using System.Text;

namespace Stride.Shaders.Spirv.Generators;

public partial class SPVGenerator
{
    public static string ConvertName(OperandData operand)
    {
        return operand switch
        {
            { Name: "name", Quantifier: "*" } => "values",
            { Name: "event", Quantifier: _ } => "eventId",
            { Name: "string", Quantifier: _ } => "value",
            { Name: "base", Quantifier: _ } => "baseId",
            { Name: "object", Quantifier: _ } => "objectId",
            { Name: "default", Quantifier: _ } => "defaultId",
            _ => operand.Name?.Replace("'", "").ToLowerInvariant() ?? ""
        };
    }


    public static string KindToVariableName(string kind)
    {
        return kind switch
        {
            "IdResult" => "result",
            "IdResultType" => "resultType",
            "IdRef" => "idRef",
            _ => kind.Replace("'", "").Replace(" ", "").ToLowerInvariant()
        };
    }
    public static string GenerateTypeName(OperandData operand)
    {
        var type = operand.Kind;
        if (operand.Class == "BitEnum")
            type = $"{type}Mask";
        if (operand.Quantifier == "*")
            type = $"Span<{type}>";
        else if (operand.Quantifier == "?")
            type = $"{type}?";
        return type;
    }
    public static string GenerateVariableName(OperandData operand)
    {
        var nameBuilder = new StringBuilder();
        bool first = true;
        operand.Name ??= ConvertKindToName(operand.Kind);
        foreach (var c in operand.Name)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                nameBuilder.Append(first ? char.ToUpperInvariant(c) : c);
                first &= false;
            }

        }
        return nameBuilder.ToString();
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

    public static string ConvertQuantifier(string quant)
    {
        if (quant == "*")
            return "ZeroOrMore";
        else if (quant == "?")
            return "ZeroOrOne";
        else return "One";
    }
}
