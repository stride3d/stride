// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Threading
{
    /// <summary>
    /// Interface implemented by pooled closure types through the AssemblyProcessor.
    /// Enables <see cref="PooledDelegateHelper"/> to keep closures and delegates alive.
    /// </summary>
    public interface IPooledClosure
    {
        void AddReference();

        void Release();
    }
}
