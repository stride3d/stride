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
using System.IO;

using boolean = System.Boolean;

namespace CppNet
{

    /**
     * An Iterator for {@link Source Sources},
     * returning {@link Token Tokens}.
     */
    internal class SourceIterator : Iterator<Token>
    {
        private Source source;
        private Token tok;

        public SourceIterator(Source s)
        {
            this.source = s;
            this.tok = null;
        }

        /**
         * Rethrows IOException inside IllegalStateException.
         */
        private void advance()
        {
            try {
                if(tok == null)
                    tok = source.token();
            } catch(LexerException e) {
                throw new IllegalStateException(e);
            } catch(IOException e) {
                throw new ApplicationException("",e);
            }
        }

        /**
         * Returns true if the enclosed Source has more tokens.
         *
         * The EOF token is never returned by the iterator.
         * @throws IllegalStateException if the Source
         *		throws a LexerException or IOException
         */
        public boolean hasNext()
        {
            advance();
            return tok.getType() != Token.EOF;
        }

        /**
         * Returns the next token from the enclosed Source.
         *
         * The EOF token is never returned by the iterator.
         * @throws IllegalStateException if the Source
         *		throws a LexerException or IOException
         */
        public Token next()
        {
            if(!hasNext())
                throw new ArgumentOutOfRangeException();
            Token t = this.tok;
            this.tok = null;
            return t;
        }

        /**
         * Not supported.
         *
         * @throws UnsupportedOperationException.
         */
        public void remove()
        {
            throw new NotSupportedException();
        }
    }


}