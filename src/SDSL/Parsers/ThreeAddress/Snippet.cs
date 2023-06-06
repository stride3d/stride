// using SDSL.Parsing.AST.Shader;
// using SDSL.Parsing.AST.Shader.Analysis;

// namespace SDSL.ThreeAddress;
// public partial class TAC
// {
//     SymbolTable symbols;
//     Dictionary<string, int> LookUp {get;set;} = new();
//     List<Register> IntermediateCode {get;set;} = new();
//     HashSet<Constant> Constants {get;set;} = new();

//     public TAC(SymbolTable symbols)
//     {
//         this.symbols = symbols;
//     }


//     public void Add(Register r)
//     {
//         if (r is Copy c && !c.IsDeclare)
//             IntermediateCode.Add(r);
//         else
//         {
//             r.Name ??= $"%{IntermediateCode.Count}";
//             if (!LookUp.ContainsKey(r.Name))
//             {
//                 LookUp[r.Name] = IntermediateCode.Count;
//                 IntermediateCode.Add(r);
//             }
            
//         }
//     }
//     public void Add(Assign r)
//     {
//         if (r.Name is null) r.Name = $"%{IntermediateCode.Count}";
//         LookUp[r.Name] = IntermediateCode.Count;
//         IntermediateCode.Add(r);
//     }
//     public Constant AddConst(Constant c)
//     {
//         if (Constants.TryGetValue(c, out var result))
//         {
//             return result;
//         }
//         else
//         {
//             c.Name ??= $"%{IntermediateCode.Count}";
//             Constants.Add(c);
//             LookUp[c.Name] = IntermediateCode.Count;
//             IntermediateCode.Add(c);
//             return c;
//         }
//     }
//     public void Construct(params Statement[] statements)
//     {
//         Construct(statements.AsEnumerable());
//     }
//     public void Construct(IEnumerable<Statement> statements)
//     {
//         foreach (var s in statements)
//         {
//             LowerToken(s);
//         }
//     }
// }
