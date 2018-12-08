// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xenko.Core;
using Xenko.Engine;
using Xenko.Games;

namespace Xenko.Physics
{
    public class Bullet2PhysicsSystem : GameSystem, IPhysicsSystem
    {
        private class PhysicsScene
        {
            public PhysicsProcessor Processor;
            public Simulation Simulation;
        }

        private readonly List<PhysicsScene> scenes = new List<PhysicsScene>();

        static Bullet2PhysicsSystem()
        {
            // Preload proper libbulletc native library (depending on CPU type)
            NativeLibrary.PreloadLibrary("libbulletc.dll", typeof(Bullet2PhysicsSystem));
        }

        public Bullet2PhysicsSystem(IServiceRegistry registry)
            : base(registry)
        {
            UpdateOrder = -1000; //make sure physics runs before everything

            Enabled = true; //enabled by default
        }

        private PhysicsSettings physicsConfiguration;

        public override void Initialize()
        {
            physicsConfiguration = Game?.Settings != null ? Game.Settings.Configurations.Get<PhysicsSettings>() : new PhysicsSettings();
        }

        protected override void Destroy()
        {
            base.Destroy();

            lock (this)
            {
                foreach (var scene in scenes)
                {
                    scene.Simulation.Dispose();
                }
            }
        }

        public Simulation Create(PhysicsProcessor sceneProcessor, PhysicsEngineFlags flags = PhysicsEngineFlags.None)
        {
            var scene = new PhysicsScene { Processor = sceneProcessor, Simulation = new Simulation(sceneProcessor, physicsConfiguration) };
            lock (this)
            {
                scenes.Add(scene);
            }
            return scene.Simulation;
        }

        public void Release(PhysicsProcessor processor)
        {
            lock (this)
            {
                var scene = scenes.SingleOrDefault(x => x.Processor == processor);
                if (scene == null) return;
                scenes.Remove(scene);
                scene.Simulation.Dispose();
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (Simulation.DisableSimulation) return;

            lock (this)
            {
                //read skinned meshes bone positions
                foreach (var physicsScene in scenes)
                {
                    //first process any needed cleanup
                    physicsScene.Processor.UpdateRemovals();

                    //read skinned meshes bone positions and write them to the physics engine
                    physicsScene.Processor.UpdateBones();
                    
                    //simulate physics
                    physicsScene.Simulation.Simulate((float)gameTime.Elapsed.TotalSeconds);

                    //update character bound entity's transforms from physics engine simulation
                    physicsScene.Processor.UpdateCharacters();

                    //Perform clean ups before test contacts in this frame
                    physicsScene.Simulation.BeginContactTesting();
                   
                    //handle frame contacts
                    physicsScene.Processor.UpdateContacts();

                    //This is the heavy contact logic
                    physicsScene.Simulation.EndContactTesting();

                    //send contact events
                    physicsScene.Simulation.SendEvents();                   
                }
            }
        }
    }
}
