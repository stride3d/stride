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
     * Warning classes which may optionally be emitted by the Preprocessor.
     */
    [Flags]
    internal enum Warning
    {
        NONE = 0,
        TRIGRAPHS = 1 << 0,
        // TRADITIONAL,
        IMPORT = 1 << 1,
        UNDEF = 1 << 2,
        UNUSED_MACROS = 1 << 3,
        ENDIF_LABELS = 1 << 4,
        ERROR = 1 << 5,
        // SYSTEM_HEADERS
    }

}