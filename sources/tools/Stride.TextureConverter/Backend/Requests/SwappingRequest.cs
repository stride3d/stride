// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.TextureConverter.Requests
{
    internal class SwappingRequest : IRequest
    {
        public override RequestType Type { get { return RequestType.Swapping; } }

        /// <summary>
        /// The first face.
        /// </summary>
        public int FirstSubImageIndex { get; set; }

        /// <summary>
        /// The second face.
        /// </summary>
        public int SecondSubImageIndex { get; set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="SwappingRequest"/> class.
        /// </summary>
        public SwappingRequest(int i, int j)
        {
            FirstSubImageIndex = i;
            SecondSubImageIndex = j;
        }
    }
}
