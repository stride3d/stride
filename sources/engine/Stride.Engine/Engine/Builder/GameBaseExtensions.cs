// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;
using Stride.Core.Serialization.Contents;
using Stride.Engine.Design;
using Stride.Games;
using Stride.Rendering;

namespace Stride.Engine.Builder;

public static class GameBaseExtensions
{
    public static GameBase UseInitialSceneFromSettings(this GameBase game)
    {
        var content = game.Services.GetService<IContentManager>();
        var settings = content.Load<GameSettings>("GameSettings");
        var sceneSystem = game.Services.GetService<SceneSystem>();
        sceneSystem.InitialSceneUrl = settings.DefaultSceneUrl;
        return game;
    }

    public static GameBase UseInitialGraphicsCompositorFromSettings(this GameBase game)
    {
        var content = game.Services.GetService<IContentManager>();
        var settings = content.Load<GameSettings>("GameSettings");
        var sceneSystem = game.Services.GetService<SceneSystem>();
        sceneSystem.InitialGraphicsCompositorUrl = settings.DefaultGraphicsCompositorUrl;

        return game;
    }

    public static GameBase UseDefaultEffectCompiler(this GameBase game)
    {
        var fileProviderService = game.Services.GetService<IDatabaseFileProviderService>().FileProvider;
        game.Services.GetService<EffectSystem>()
            .CreateDefaultEffectCompiler(fileProviderService);

        return game;
    }
}
