namespace Stride.Shaders.Parsing.AST.Shader.Analysis;

public class NumberTypeCasting
{
    static Dictionary<string, string[]> implicitCastingException = new Dictionary<string,string[]>{
        {"byte", new string[0]},
        {"sbyte", new string[0]},
        {"short", new string[]{"byte","sbyte"}},
        {"ushort", new string[]{"byte","sbyte"}},
        {"half", new string[]{"byte","sbyte"}},
        {"int", new string[]{"sbyte", "ushort", "short"}},
        {"uint", new string[]{"sbyte", "ushort", "short"}},
        {"byte", new string[]{"sbyte", "ushort", "short"}},
        {"byte", new string[]{"sbyte", "ushort", "short"}},

    };
}