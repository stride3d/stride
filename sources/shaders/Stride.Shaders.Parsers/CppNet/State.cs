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

    /* pp */
    class State
    {
        bool _parent;
        bool _active;
        bool _sawElse;

        /* pp */
        internal State()
        {
            this._parent = true;
            this._active = true;
            this._sawElse = false;
        }

        /* pp */
        internal State(State parent)
        {
            this._parent = parent.isParentActive() && parent.isActive();
            this._active = true;
            this._sawElse = false;
        }

        /* Required for #elif */
        /* pp */
        internal void setParentActive(bool b)
        {
            this._parent = b;
        }

        /* pp */
        internal bool isParentActive()
        {
            return _parent;
        }

        /* pp */
        internal void setActive(bool b)
        {
            this._active = b;
        }

        /* pp */
        internal bool isActive()
        {
            return _active;
        }

        /* pp */
        internal void setSawElse()
        {
            _sawElse = true;
        }

        /* pp */
        internal bool sawElse()
        {
            return _sawElse;
        }

        public override String ToString()
        {
            return "parent=" + _parent +
                ", active=" + _active +
                ", sawelse=" + _sawElse;
        }
    }
}