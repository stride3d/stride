// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Xenko.Core.Yaml.Events;

namespace Xenko.Core.Yaml
{
    /// <summary>
    /// Objects that can't be loaded as valid Yaml will be converted to a proxy object implementing this interface by <see cref="ErrorRecoverySerializer"/>.
    /// </summary>
    public interface IUnloadable
    {
        string TypeName { get; }

        string AssemblyName { get; }

        string Error { get; }

        List<ParsingEvent> ParsingEvents { get; }
    }
}
