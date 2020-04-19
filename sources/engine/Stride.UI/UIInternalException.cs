// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.UI
{
    /// <summary>
    /// The exception that is thrown when an internal error happened in the UI System. That is an error that is not due to the user behavior.
    /// </summary>
    public class UIInternalException : Exception
    {
        internal UIInternalException(string msg)
            : base("An internal error happened in the UI system [details:'" + msg + "'")
        { }
    }
}
