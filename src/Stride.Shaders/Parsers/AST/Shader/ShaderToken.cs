using Eto.Parse;
using Stride.Shaders.Parsing.AST.Shader.Analysis;
using Stride.Shaders.Parsing.Grammars.Expression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Parsing.AST.Shader;


public abstract class ShaderToken
{
	public static string[] KeepValues = {
		"Block",
		"Return",
		"EmptyStatement",
		"ConstantBuffer",
		"ResourceGroup",
	};
	public Match? Match { get; set; }

	public static ShaderToken Tokenize(Match match)
	{
		return GetToken(match,new SymbolTable());
	}
	public static ShaderToken GetToken(Match match, SymbolTable symbols)
	{
		var tmp = match;
		while (tmp.Matches.Count == 1 && !KeepValues.Contains(tmp.Name))
			tmp = tmp.Matches.First();

		return tmp.Name switch
		{
			"Namespace" => GetToken(tmp.Matches.Last(),symbols),
			"ShaderProgram" => new ShaderProgram(tmp,symbols),
			"ResourceGroup" => new ResourceGroup(tmp,symbols),
			"ConstantBuffer" => new ConstantBuffer(tmp,symbols),
			"ShaderValueDeclaration" => new ShaderVariableDeclaration(tmp, symbols),
			"Method" => ShaderMethod.Create(tmp, symbols),
			"ControlFlow" => ControlFlow.Create(tmp, symbols),
			"Block" => new BlockStatement(tmp, symbols),
			"Return" => new ReturnStatement(tmp, symbols),
			"AssignChain" => new AssignChain(tmp, symbols),
			"DeclareAssign" => new DeclareAssign(tmp, symbols),
			"SimpleDeclare" => new SimpleDeclare(tmp, symbols),
			"EmptyStatement" => new EmptyStatement(),
			"MethodCall" => new MethodCall(tmp, symbols),
			"ValueTypesMethods" => new ValueMethodCall(tmp, symbols),
			"Ternary" => new ConditionalExpression(tmp, symbols),
			"LogicalOrExpression" => LogicalOrExpression.Create(tmp, symbols),
			"LogicalAndExpression" => LogicalAndExpression.Create(tmp, symbols),
			"EqualsExpression" => EqualsExpression.Create(tmp, symbols),
			"TestExpression" => TestExpression.Create(tmp, symbols),
			"OrExpression" => OrExpression.Create(tmp, symbols),
			"XorExpression" => XorExpression.Create(tmp, symbols),
			"AndExpression" => AndExpression.Create(tmp, symbols),
			"ShiftExpression" => ShiftExpression.Create(tmp, symbols),
			"SumExpression" => SumExpression.Create(tmp, symbols),
			"MulExpression" => MulExpression.Create(tmp, symbols),
			"CastExpression" => new CastExpression(tmp, symbols),
			"PrefixIncrement" => throw new NotImplementedException("prefix implement not implemented"),
			"ChainAccessor" => new ChainAccessor(tmp, symbols),
			"ArrayAccessor" => new ArrayAccessor(tmp, symbols),
			"IntegerValue" or "FloatValue" or "FloatLiteral" => new NumberLiteral(tmp, symbols),
			"VariableTerm" or "Identifier" => new VariableNameLiteral(tmp, symbols),
			"ValueTypes" or "TypeName" => new TypeNameLiteral(tmp, symbols),
			"Boolean" => new BoolLiteral(tmp, symbols),
			_ => throw new NotImplementedException()
		};
	}

	


	// public IEnumerable<string> GetUsedStream()
	// {
	// 	return this switch 
	// 	{
	// 		AssignChain a => a.Value.GetUsedStream(),
	// 		ChainAccessor{Value: VariableNameLiteral{Name : "streams"}} => new string[1]{((VariableNameLiteral)((ChainAccessor)this).Field).Name},
	// 		Operation {Left : ChainAccessor c} => c.GetUsedStream(),
	// 		Operation {Right : ChainAccessor c} => c.GetUsedStream(),
	// 		_ => Array.Empty<string>()
	// 	};
	// }
	// public IEnumerable<string> GetAssignedStream()
	// {
	// 	return this switch 
	// 	{
	// 		AssignChain{StreamValue: true} c => new string[1]{c.AccessNames.ElementAt(1)},
	// 		BlockStatement b => b.Statements.SelectMany(x => x.GetAssignedStream()),
	// 		_ => Array.Empty<string>()
	// 	};
	// }
	
}