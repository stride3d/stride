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

using System;
using System.Text;
using System.Collections.Generic;

namespace CppNet {

internal class FixedTokenSource : Source {
	private static readonly Token	EOF =
			new Token(Token.EOF, "<ts-eof>");

	private List<Token>	tokens;
	private int			idx;

	internal FixedTokenSource(params Token[] tokens) {
        this.tokens = new List<Token>(tokens);
		this.idx = 0;
	}

	internal FixedTokenSource(List<Token> tokens) {
		this.tokens = tokens;
		this.idx = 0;
	}

	public override Token token() {
		if (idx >= tokens.Count)
			return EOF;
		return tokens[idx++];
	}

	override public String ToString() {
		StringBuilder	buf = new StringBuilder();
		buf.Append("constant token stream " + tokens);
		Source	parent = getParent();
		if (parent != null)
			buf.Append(" in ").Append(parent);
		return buf.ToString();
	}
}

}