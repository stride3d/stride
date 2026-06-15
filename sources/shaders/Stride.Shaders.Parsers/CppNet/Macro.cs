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

namespace CppNet
{
    /**
     * A macro object.
     *
     * This encapsulates a name, an argument count, and a token stream
     * for replacement. The replacement token stream may contain the
     * extra tokens {@link Token#M_ARG} and {@link Token#M_STRING}.
     */
    internal class Macro
    {
        private Source source;
        private String name;
        /* It's an explicit decision to keep these around here. We don't
         * need to; the argument token type is M_ARG and the value
         * is the index. The strings themselves are only used in
         * stringification of the macro, for debugging. */
        private List<String> args;
        private bool variadic;
        private List<Token> tokens;

        public Macro(Source source, String name)
        {
            this.source = source;
            this.name = name;
            this.args = null;
            this.variadic = false;
            this.tokens = new List<Token>();
        }

        public Macro(String name)
            : this(null, name)
        {
        }

        /**
         * Sets the Source from which this macro was parsed.
         */
        public void setSource(Source s)
        {
            this.source = s;
        }

        /**
         * Returns the Source from which this macro was parsed.
         *
         * This method may return null if the macro was not parsed
         * from a regular file.
         */
        public Source getSource()
        {
            return source;
        }

        /**
         * Returns the name of this macro.
         */
        public String getName()
        {
            return name;
        }

        /**
         * Sets the arguments to this macro.
         */
        public void setArgs(List<String> args)
        {
            this.args = args;
        }

        /**
         * Returns true if this is a function-like macro.
         */
        public bool isFunctionLike()
        {
            return args != null;
        }

        /**
         * Returns the number of arguments to this macro.
         */
        public int getArgs()
        {
            return args.Count;
        }

        /**
         * Sets the variadic flag on this Macro.
         */
        public void setVariadic(bool b)
        {
            this.variadic = b;
        }

        /**
         * Returns true if this is a variadic function-like macro.
         */
        public bool isVariadic()
        {
            return variadic;
        }

        /**
         * Adds a token to the expansion of this macro.
         */
        public void addToken(Token tok)
        {
            this.tokens.Add(tok);
        }

        /**
         * Adds a "paste" operator to the expansion of this macro.
         *
         * A paste operator causes the next token added to be pasted
         * to the previous token when the macro is expanded.
         * It is an error for a macro to end with a paste token.
         */
        public void addPaste(Token tok)
        {
            /*
             * Given: tok0 ## tok1
             * We generate: M_PASTE, tok0, tok1
             * This extends as per a stack language:
             * tok0 ## tok1 ## tok2 ->
             *   M_PASTE, tok0, M_PASTE, tok1, tok2
             */
            this.tokens.Insert(tokens.Count - 1, tok);
        }

        internal List<Token> getTokens()
        {
            return tokens;
        }

        /* Paste tokens are inserted before the first of the two pasted
         * tokens, so it's a kind of bytecode notation. This method
         * swaps them around again. We know that there will never be two
         * sequential paste tokens, so a bool is sufficient. */
        public String getText() {
		StringBuilder	buf = new StringBuilder();
		bool			paste = false;
		for (int i = 0; i < tokens.Count; i++) {
			Token	tok = tokens[i];
			if (tok.getType() == Token.M_PASTE) {
                System.Diagnostics.Debug.Assert(paste == false, "Two sequential pastes.");
				paste = true;
				continue;
			}
			else {
				buf.Append(tok.getText());
			}
			if (paste) {
				buf.Append(" #" + "# ");
				paste = false;
			}
			// buf.Append(tokens.get(i));
		}
		return buf.ToString();
	}

        override public String ToString()
        {
            StringBuilder buf = new StringBuilder(name);
            if(args != null) {
                buf.Append('(');
                bool first = true;
                foreach(String str in args) {
                    if(!first) {
                        buf.Append(", ");
                    } else {
                        first = false;
                    }
                    buf.Append(str);
                }
                if(isVariadic()) {
                    buf.Append("...");
                }

                buf.Append(')');
            }
            if(tokens.Count != 0) {
                buf.Append(" => ").Append(getText());
            }
            return buf.ToString();
        }

    }
}
