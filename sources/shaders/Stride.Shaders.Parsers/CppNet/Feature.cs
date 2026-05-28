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

namespace CppNet
{
    /**
     * Features of the Preprocessor, which may be enabled or disabled.
     */
    [Flags]
    internal enum Feature
    {
        NONE = 0,
        /** Supports ANSI digraphs. */
        DIGRAPHS = 1 << 0,
        /** Supports ANSI trigraphs. */
        TRIGRAPHS = 1 << 1,
        /** Outputs linemarker tokens. */
        LINEMARKERS = 1 << 2,
        /** Reports tokens of type INVALID as errors. */
        CSYNTAX = 1 << 3,
        /** Preserves comments in the lexed output. */
        KEEPCOMMENTS = 1 << 4,
        /** Preserves comments in the lexed output, even when inactive. */
        KEEPALLCOMMENTS = 1 << 5,
        VERBOSE = 1 << 6,
        DEBUG = 1 << 7,

        /** Supports lexing of objective-C. */
        OBJCSYNTAX = 1 << 8,
        INCLUDENEXT = 1 << 9
    }

}