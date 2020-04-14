// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Input;

namespace Xenko.Games.Testing.Requests
{
    [DataContract]
    internal class KeySimulationRequest : TestRequestBase
    {
        public Keys Key;
        public bool Down;
    }
}
