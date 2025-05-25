// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See acompanying acclaimed file or LICENSE.md for details.
//
// Adapted from Stride's FirstPersonShooter template

using Stride.Engine;

namespace MySurvivalGame
{
    class MySurvivalGameApp
    {
        static void Main(string[] args)
        {
            using (var game = new Game())
            {
                game.Run();
            }
        }
    }
}
