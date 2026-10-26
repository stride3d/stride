// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Annotations;
using Stride.Editor.Tests.Helpers;
using Xunit;

// The tests share the static SessionViewModel.Instance and the builder's object database
[assembly: CollectionBehavior(DisableTestParallelization = true)]

// The static thumbnail of the test asset type, as a plugin declares it
[assembly: StaticThumbnail(typeof(StaticThumbnailAsset), "Stride.Editor.Tests.Resources.StaticThumbnail.png")]
