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
using System.Collections.Generic;
using System.Text;

namespace CppNet {
/**
 * A macro argument.
 *
 * This encapsulates a raw and preprocessed token stream.
 */
internal class Argument : List<Token> {
	public const int	NO_ARGS = -1;

	private List<Token>	_expansion;

	public Argument() {
        this._expansion = null;
	}

	public void addToken(Token tok) {
		Add(tok);
	}

	internal void expand(Preprocessor p) {
		/* Cache expansion. */
        if(_expansion == null) {
            this._expansion = p.expand(this);
			// System.out.println("Expanded arg " + this);
		}
	}

    public Iterator<Token> expansion()
    {
        return _expansion.iterator();
    }

	override public String ToString() {
		StringBuilder	buf = new StringBuilder();
		buf.Append("Argument(");
		// buf.Append(super.toString());
		buf.Append("raw=[ ");
		for (int i = 0; i < this.Count; i++)
			buf.Append(this[i].getText());
		buf.Append(" ];expansion=[ ");
        if(_expansion == null)
			buf.Append("null");
		else
            for(int i = 0; i < _expansion.Count; i++)
                buf.Append(_expansion[i].getText());
		buf.Append(" ])");
		return buf.ToString();
	}

}

}