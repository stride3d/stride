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

namespace CppNet {
/**
 * A preprocessor exception.
 *
 * Note to users: I don't really like the name of this class. S.
 */
internal class LexerException : Exception {
    public LexerException(String msg) : base(msg) { }

	public LexerException(Exception cause) : base(cause.Message, cause) {}
}

}