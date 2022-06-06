using Eto.Parse;
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

	public static ShaderToken GetToken(Match match)
	{
		var tmp = match;
		while (tmp.Matches.Count == 1 && !KeepValues.Contains(tmp.Name))
			tmp = tmp.Matches.First();

		return tmp.Name switch
		{
			"Namespace" => GetToken(tmp.Matches.Last()),
			"ShaderProgram" => new ShaderProgram(tmp),
			"ResourceGroup" => new ResourceGroup(tmp),
			"ConstantBuffer" => new ConstantBuffer(tmp),
			"ShaderValueDeclaration" => new ShaderValueDeclaration(tmp),
			"Method" => new ShaderMethod(tmp),
			"ControlFlow" => ControlFlow.Create(tmp),
			"Block" => new BlockStatement(tmp),
			"Return" => new ReturnStatement(tmp),
			"AssignChain" => new AssignChain(tmp),
			"DeclareAssign" => new DeclareAssign(tmp),
			"SimpleDeclare" => throw new NotImplementedException(),
			"EmptyStatement" => new EmptyStatement(),
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
			"ChainAccessor" => throw new NotImplementedException(),
			"IntegerValue" or "FloatValue" or "FloatLiteral" => new NumberLiteral(tmp),
			"VariableTerm" or "Identifier" => new VariableNameLiteral(tmp),
			"ValueTypes" or "TypeName" => new TypeNameLiteral(tmp),
			"Boolean" => new BoolLiteral(tmp),
			_ => throw new NotImplementedException()
		};
	}
}
