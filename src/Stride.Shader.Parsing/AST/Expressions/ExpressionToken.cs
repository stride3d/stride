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

	internal static ExpressionToken GetToken(Match match)
	{
		return match.Name switch
		{
			"PrimaryExpression" => PrimaryExpression.GetSubToken(match),
			_ => throw new NotImplementedException()
		};
	}
}
