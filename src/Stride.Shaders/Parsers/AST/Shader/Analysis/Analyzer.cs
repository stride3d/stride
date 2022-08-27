// namespace Stride.Shaders.Parsing.AST.Shader.Analysis;

// public class Analyzer
// {
//     public string? TypeChecking(ShaderToken t)
//     {
//         return t switch 
//         {
//             Operation o => TypeCheck(o),
//             NumberLiteral nl => TypeCheck(nl),
//             VariableNameLiteral v => TypeCheck(v),
//             _ => throw new NotImplementedException()
//         };
//     }
//     public string? TypeCheck(Operation o)
//     {
//         var left = TypeChecking(o.Left);
//         var right = TypeChecking(o.Right);
//         return Compare(left, right);
//     }
//     public string? TypeCheck(VariableNameLiteral v)
//     {
//         if(variables.TryGetValue(v.Name, out var saved))
//         {
//             v.InferredType = saved.InferredType;
//             return saved.InferredType;
//         }
//         else
//         {
//             variables[v.Name] = v;
//             return null;
//         }
//     }
//     public string? TypeCheck(NumberLiteral n) => n.InferredType;
//     public string? Compare(string a, string b)
//     {
//         return a;
//     }
// }