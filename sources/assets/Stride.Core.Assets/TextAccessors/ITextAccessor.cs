// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO;
using System.Threading.Tasks;

namespace Stride.Core.Assets.TextAccessors
{
    public interface ITextAccessor
    {
        /// <summary>
        /// Gets the underlying text.
        /// </summary>
        /// <returns></returns>
        string Get();

        /// <summary>
        /// Sets the underlying text.
        /// </summary>
        /// <param name="value"></param>
        void Set(string value);

        /// <summary>
        /// Writes the text to the given <see cref="StreamWriter"/>.
        /// </summary>
        /// <param name="streamWriter"></param>
        Task Save(Stream streamWriter);

        ISerializableTextAccessor GetSerializableVersion();
    }
}
