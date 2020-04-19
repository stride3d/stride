// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.TextureConverter.Backend.Requests
{
    class InvertYUpdateRequest : IRequest
    {
        public override RequestType Type { get { return RequestType.InvertYUpdate; } }

        public TexImage NormalMap { get; set; }
    }
}
