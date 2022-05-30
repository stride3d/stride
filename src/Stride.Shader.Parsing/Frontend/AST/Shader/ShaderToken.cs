using Eto.Parse;
using Stride.Shader.Parsing.Grammars.Expression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shader.Parsing.AST.Shader;


public abstract class ShaderToken
{
	public static string[] KeepValues = {
		"Block",
		"Return",
	};
	public Match? Match { get; set; }

	public static ShaderToken GetToken(Match match)
	{
		var tmp = match;
		while (tmp.Matches.Count == 1 && !KeepValues.Contains(tmp.Name))
			tmp = tmp.Matches.First();

		return tmp.Name switch
		{
			"ShaderProgram" => new ShaderProgram(tmp),
			"ShaderValueDeclaration" => new ShaderValueDeclaration(tmp),
			"Method" => new ShaderMethod(tmp),
			"Block" => new BlockStatement(tmp),
			"Return" => new ReturnStatement(tmp),
			"AssignChain" => new AssignChain(tmp),
			"DeclareAssign" => new DeclareAssign(tmp),
			"MethodCall" => new MethodCall(tmp),
			"Ternary" => new ConditionalExpression(tmp),
			"LogicalOrExpression" => LogicalOrExpression.Create(tmp),
			"LogicalAndExpression" => LogicalAndExpression.Create(tmp),
			"EqualsExpression" => EqualsExpression.Create(tmp),
			"TestExpression" => TestExpression.Create(tmp),
			"OrExpression" => OrExpression.Create(tmp),
			"XorExpression" => XorExpression.Create(tmp),
			"AndExpression" => AndExpression.Create(tmp),
			"ShiftExpression" => ShiftExpression.Create(tmp),
			"SumExpression" => SumExpression.Create(tmp),
			"MulExpression" => MulExpression.Create(tmp),
			"CastExpression" => new CastExpression(tmp),
			"PrefixIncrement" => throw new NotImplementedException("prefix implement not implemented"),
			"IntegerValue" or "FloatValue" => new NumberLiteral(tmp),
			"VariableTerm" or "Identifier" => new VariableNameLiteral(tmp),
			"ValueTypes" or "TypeName" => new TypeNameLiteral(tmp),
			"Boolean" => new BoolLiteral(tmp),
			_ => throw new NotImplementedException()
		};
	}

	public static ShaderToken EvaluateExpression(ShaderToken expr, Dictionary<string,object> macros)
    {
		return expr switch
		{
			ShaderLiteral l => l,
			Operation o => EvaluateOperation(o,macros),
			_ => throw new Exception("Couldn't evaluate expression")
		};
    }
	private static ShaderToken EvaluateOperation(Operation operation, Dictionary<string, object> macros)
    {
		return operation.ProjectConstant() switch
		{
			Operation o => o,
			ShaderLiteral t => t,
			_ => throw new Exception("Couldn't evaluate operation")
		};
    }
}
