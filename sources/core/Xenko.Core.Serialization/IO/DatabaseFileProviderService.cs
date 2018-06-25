// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Xenko.Core.IO
{
    public class DatabaseFileProviderService : IDatabaseFileProviderService
    {
        public DatabaseFileProviderService(DatabaseFileProvider fileProvider)
        {
            FileProvider = fileProvider;
        }

        /// <inheritdoc />
        public DatabaseFileProvider FileProvider { get; set; }
    }
}
