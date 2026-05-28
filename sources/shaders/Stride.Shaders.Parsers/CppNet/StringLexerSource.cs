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
using System.IO;

namespace CppNet {

/**
 * A Source for lexing a String.
 *
 * This class is used by token pasting, but can be used by user
 * code.
 */
internal class StringLexerSource : LexerSource
{
    private readonly string filename;
	/**
	 * Creates a new Source for lexing the given String.
	 *
	 * @param ppvalid true if preprocessor directives are to be
	 *	honoured within the string.
	 */
    public StringLexerSource(String str, bool ppvalid, string fileName = null) : 
		base(new StringReader(str), ppvalid)
    {
        this.filename = fileName ?? string.Empty; // Always use empty otherwise cpp.token() will fail on NullReferenceException
    }

	/**
	 * Creates a new Source for lexing the given String.
	 *
	 * By default, preprocessor directives are not honoured within
	 * the string.
	 */
	public StringLexerSource(String str, string fileName = null) :
		this(str, false, fileName) {
	}

    override internal String getPath()
    {
        return filename;
    }

    override internal String getName()
    {
        return getPath();
    }


    override public String ToString() {
		return "string literal";
	}
}

}