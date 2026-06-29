/*
 * Anarres C Preprocessor
 * Copyright (c) 2007-2008, Shevek
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
 * or implied.  See the License for the specific language governing
 * permissions and limitations under the License.
 */
#pragma warning disable

using System;
using System.Text;
using System.Collections.Generic;
using boolean = System.Boolean;
using Debug = System.Diagnostics.Debug;

namespace CppNet {

/* This source should always be active, since we don't expand macros
 * in any inactive context. */
internal class MacroTokenSource : Source {
	private Macro				macro;
	private Iterator<Token>		tokens;	/* Pointer into the macro.  */
	private List<Argument>		args;	/* { unexpanded, expanded } */
	private Iterator<Token>		arg;	/* "current expansion" */

	internal MacroTokenSource(Macro m, List<Argument> args) {
		this.macro = m;
		this.tokens = m.getTokens().iterator();
		this.args = args;
		this.arg = null;
	}

	override internal boolean isExpanding(Macro m) {
		/* When we are expanding an arg, 'this' macro is not
		 * being expanded, and thus we may re-expand it. */
		if (/* XXX this.arg == null && */ this.macro == m)
			return true;
		return base.isExpanding(m);
	}

	/* XXX Called from Preprocessor [ugly]. */
	internal static void escape(StringBuilder buf, string cs) {
	    if (cs == null)
	    {
	        return;
	    }
		for (int i = 0; i < cs.length(); i++) {
			char	c = cs.charAt(i);
			switch (c) {
				case '\\':
					buf.append("\\\\");
					break;
				case '"':
					buf.append("\\\"");
					break;
				case '\n':
					buf.append("\\n");
					break;
				case '\r':
					buf.append("\\r");
					break;
				default:
					buf.append(c);
                    break;
			}
		}
	}

	private void concat(StringBuilder buf, Argument arg) {
		Iterator<Token>	it = arg.iterator();
		while (it.hasNext()) {
			Token	tok = it.next();
			buf.append(tok.getText());
		}
	}

	private Token stringify(Token pos, Argument arg) {
		StringBuilder	buf = new StringBuilder();
		concat(buf, arg);
		// System.out.println("Concat: " + arg + " -> " + buf);
		StringBuilder	str = new StringBuilder("\"");
		escape(str, buf.ToString());
		str.append("\"");
		// System.out.println("Escape: " + buf + " -> " + str);
		return new Token(Token.STRING,
				pos.getLine(), pos.getColumn(),
				str.toString(), buf.toString());
	}


	/* At this point, we have consumed the first M_PASTE.
	 * @see Macro#addPaste(Token) */
	private void paste(Token ptok)  {
		StringBuilder	buf = new StringBuilder();
		Token			err = null;
		/* We know here that arg is null or expired,
		 * since we cannot paste an expanded arg. */

		int	count = 2;
		for (int i = 0; i < count; i++) {
			if (!tokens.hasNext()) {
				/* XXX This one really should throw. */
				error(ptok.getLine(), ptok.getColumn(),
						"Paste at end of expansion");
				buf.append(' ').append(ptok.getText());
				break;
			}
			Token	tok = tokens.next();
			// System.out.println("Paste " + tok);
			switch (tok.getType()) {
				case Token.M_PASTE:
					/* One extra to paste, plus one because the
					 * paste token didn't count. */
					count += 2;
					ptok = tok;
					break;
                case Token.M_ARG:
					int idx = (int)tok.getValue();
					concat(buf, args.get(idx));
					break;
				/* XXX Test this. */
                case Token.CCOMMENT:
                case Token.CPPCOMMENT:
					break;
				default:
					buf.append(tok.getText());
					break;
			}
		}

		/* Push and re-lex. */
		/*
		StringBuilder		src = new StringBuilder();
		escape(src, buf);
		StringLexerSource	sl = new StringLexerSource(src.toString());
		*/
		StringLexerSource	sl = new StringLexerSource(buf.toString());

		/* XXX Check that concatenation produces a valid token. */

		arg = new SourceIterator(sl);
	}

	override public Token token()  {
		for (;;) {
			/* Deal with lexed tokens first. */

			if (arg != null) {
				if (arg.hasNext()) {
					Token	tok2 = arg.next();
					/* XXX PASTE -> INVALID. */
                    Debug.Assert(tok2.getType() != Token.M_PASTE,
                                "Unexpected paste token");
					return tok2;
				}
				arg = null;
			}

			if (!tokens.hasNext())
				return new Token(Token.EOF, -1, -1, "");	/* End of macro. */
			Token	tok = tokens.next();
			int		idx;
			switch (tok.getType()) {
				case Token.M_STRING:
					/* Use the nonexpanded arg. */
					idx = (int)tok.getValue();
					return stringify(tok, args.get(idx));
                case Token.M_ARG:
					/* Expand the arg. */
                    idx = (int)tok.getValue();
					// System.out.println("Pushing arg " + args.get(idx));
					arg = args.get(idx).expansion();
					break;
                case Token.M_PASTE:
					paste(tok);
					break;
				default:
					return tok;
			}
		} /* for */
	}


	override public String ToString() {
		StringBuilder	buf = new StringBuilder();
		buf.Append("expansion of ").Append(macro.getName());
		Source	parent = getParent();
		if (parent != null)
			buf.Append(" in ").Append(parent);
		return buf.ToString();
	}
}

}