// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.Games.Testing
{
    //This is how we inject the assembly to run automatically at game start, paired with Stride.targets and the msbuild property StrideAutoTesting
    internal class Module
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            //Quit after 10 seconds anyway!
            Task.Run(async () =>
            {
                await Task.Delay(20000);
                if (!GameTestingSystem.Initialized)
                {
#if STRIDE_PLATFORM_IOS || STRIDE_PLATFORM_ANDROID || STRIDE_PLATFORM_WINDOWS_DESKTOP
                    Console.WriteLine(@"FATAL: Test launch timeout. Aborting.");
#endif
                    GameTestingSystem.Quit();
                }
            });

            //quit after 10 seconds in any case
            Game.GameStarted += (sender, args) =>
            {              
                var game = (Game)sender;
                var testingSystem = new GameTestingSystem(game.Services);
                game.GameSystems.Add(testingSystem);
            };
        }
    }
}
