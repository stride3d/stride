using Eto.Parse;
using Stride.Shader.Parsing.Grammars.Expression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shader.Parsing.AST.Expressions;

public abstract class ExpressionToken 
{
	static ExpressionGrammar grammar;
	protected static ExpressionGrammar Grammar { get { return grammar ??= new ExpressionGrammar(); } }

	public Match Match { get; set; }

	public static ExpressionToken Parse(string expr)
	{
		var match = Grammar.Match(expr);
		if (!match.Success)
			throw new ArgumentOutOfRangeException("expr", string.Format("Invalid expr string: {0}", match.ErrorMessage));
		return GetToken(match.Matches.First());
	}

	public static ExpressionToken GetToken(Match match)
	{
		var tmp = match;
		while (tmp.Matches.Count == 1)
			tmp = tmp.Matches.First();

		return tmp.Name switch
		{
			"PrimaryExpression" => GetToken(tmp),
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

			_ => throw new NotImplementedException()
		};
	}
}
