using Eto.Parse;
using Stride.Shaders.Parsing.Grammars.Expression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Parsing.AST.Directives;


public abstract class DirectiveToken
{
	public Match Match { get; set; }
	public abstract Type InferredType { get; set; }

	public abstract void EvaluateMacros(Dictionary<string, object> macros);

	public static DirectiveToken GetToken(Match match)
	{
		var tmp = match;
		while (tmp.Matches.Count == 1)
			tmp = tmp.Matches.First();

		return tmp.Name switch
		{
			"Directives" => new Directives(tmp),
			"CodeSnippet" => new CodeSnippet(tmp),
			"DefineDirective" => new DefineDirective(tmp),
			"IfCode" => new IfCode(tmp),
			"IfDefCode" or "IfNDefCode" => new IfDefCode(tmp),
			"IfDefDirective" or "IfNDefDirective" => new IfDefineDirective(tmp),
			"DirectiveTernary" => new ConditionalExpression(tmp),
			"DirectiveLogicalOrExpression" => LogicalOrExpression.Create(tmp),
			"DirectiveLogicalAndExpression" => LogicalAndExpression.Create(tmp),
			"DirectiveEqualsExpression" => EqualsExpression.Create(tmp),
			"DirectiveTestExpression" => TestExpression.Create(tmp),
			"DirectiveOrExpression" => OrExpression.Create(tmp),
			"DirectiveXorExpression" => XorExpression.Create(tmp),
			"DirectiveAndExpression" => AndExpression.Create(tmp),
			"DirectiveShiftExpression" => ShiftExpression.Create(tmp),
			"DirectiveSumExpression" => SumExpression.Create(tmp),
			"DirectiveMulExpression" => MulExpression.Create(tmp),
			"DirectiveCastExpression" => new CastExpression(tmp),
			"DirectivePrefixIncrement" => throw new NotImplementedException("prefix implement not implemented"),
			"IntegerValue" or "FloatValue" => new NumberLiteral(tmp),
			"VariableTerm" or "Identifier" => new VariableNameLiteral(tmp),
			"ValueTypes" or "TypeName" => new TypeNameLiteral(tmp),
			"Boolean" => new BoolLiteral(tmp),
			_ => throw new NotImplementedException()
		};
	}

	public static void Evaluate(DirectiveToken token, Dictionary<string, object> macros, StringBuilder code)
    {
		switch(token)
        {
			case Directives d:
				foreach(var c in d.DirectiveList)
					Evaluate(c, macros, code);
				break;
			case CodeSnippet snippet:
				code.Append(snippet.Content);
				break;

			case DefineDirective def:
				macros.Add(def.VariableName, def.Value);
				break;

			case IfDefCode ifdefcode:
				if(CheckCondition(ifdefcode.If,macros))
					foreach (var c in ifdefcode.Children)
						Evaluate(c, macros, code);
                else
					foreach (var c in ifdefcode.Children)
						Evaluate(c, macros, code);
				break;

			case IfCode ifCode:
				if (CheckCondition(ifCode.If, macros))
					foreach (var c in ifCode.Children)
						Evaluate(c, macros, code);
				else
                {
					bool passed = false;
					if (ifCode.Elifs is not null)
					{
						foreach (var e in ifCode.Elifs)
						{
							if (CheckCondition(e.Elif, macros))
							{
								foreach (var c in e.Children)
									Evaluate(c, macros, code);
								passed = true;
								break;
							}
						}
						if (passed)
							foreach (var c in ifCode.Children)
								Evaluate(c, macros, code);
					}
					else if(!passed && ifCode.Else is not null)
						foreach(var c in ifCode.Else.Children)
							Evaluate(c, macros, code);
				}
				break;
			default:
				throw new NotImplementedException("");
        }
    }

	public static bool CheckCondition(DirectiveToken token, Dictionary<string, object> macros)
    {
		return token switch
		{
			IfDefineDirective ifDefine => ifDefine.IsDefined && macros.ContainsKey(ifDefine.Name),
			IfDirective ifd => EvaluateExpression(ifd.Condition,macros),
			ElifDirective elifd => EvaluateExpression(elifd.Condition,macros),
			_ => throw new NotImplementedException("")
		};
    }

	public static bool EvaluateExpression(DirectiveToken expr, Dictionary<string,object> macros)
    {
		return expr switch
		{
			BoolLiteral b => b.Value,
			Operation o => EvaluateOperation(o,macros),
			_ => throw new Exception("Couldn't evaluate expression")
		};
    }
	private static bool EvaluateOperation(Operation operation, Dictionary<string, object> macros)
    {
		operation.EvaluateMacros(macros);
		return operation.ProjectConstant() switch
		{
			BoolLiteral b => b.Value,
			_ => throw new Exception("Couldn't evaluate operation")
		};
    }
}
