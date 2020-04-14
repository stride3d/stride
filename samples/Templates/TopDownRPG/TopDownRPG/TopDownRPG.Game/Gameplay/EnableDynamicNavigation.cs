// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using System.Linq;
using Stride.Core.Collections;
using Stride.Engine;
using Stride.Navigation;

namespace Gameplay
{
    public class EnableDynamicNavigation : StartupScript
    {
        public override void Start()
        {
            var dynamicNavigationMeshSystem = Game.GameSystems.OfType<DynamicNavigationMeshSystem>().FirstOrDefault();

            // Wait for the dynamic navigation to be registered
            if (dynamicNavigationMeshSystem == null)
                Game.GameSystems.CollectionChanged += GameSystemsOnCollectionChanged;
            else
                dynamicNavigationMeshSystem.Enabled = true;
        }

        public override void Cancel()
        {
            Game.GameSystems.CollectionChanged -= GameSystemsOnCollectionChanged;
        }

        private void GameSystemsOnCollectionChanged(object sender, TrackingCollectionChangedEventArgs trackingCollectionChangedEventArgs)
        {
            if (trackingCollectionChangedEventArgs.Action == NotifyCollectionChangedAction.Add)
            {
                var dynamicNavigationMeshSystem = trackingCollectionChangedEventArgs.Item as DynamicNavigationMeshSystem;
                if (dynamicNavigationMeshSystem != null)
                {
                    dynamicNavigationMeshSystem.Enabled = true;

                    // No longer need to listen to changes
                    Game.GameSystems.CollectionChanged -= GameSystemsOnCollectionChanged;
                }
            }
        }
    }
}
