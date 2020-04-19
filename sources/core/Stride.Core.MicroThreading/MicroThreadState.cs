// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.MicroThreading
{
    public enum MicroThreadState : int
    {
        None,
        Starting,
        Running,
        Completed,
        Canceled,
        Failed,
    }
}
