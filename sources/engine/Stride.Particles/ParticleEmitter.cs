// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Particles.Initializers;
using Stride.Particles.Materials;
using Stride.Particles.Modules;
using Stride.Particles.ShapeBuilders;
using Stride.Particles.Sorters;
using Stride.Particles.Spawners;
using Stride.Particles.VertexLayouts;
using Stride.Rendering;

namespace Stride.Particles
{
    public enum EmitterRandomSeedMethod : byte
    {
        Time = 0,
        Fixed = 1,
        Position = 2,        
    }

    public enum EmitterSimulationSpace : byte
    {
        World = 0,
        Local = 1,
    }

    public enum EmitterSortingPolicy : byte
    {
        None = 0,
        ByDepth = 1,
        ByAge = 2,
        ByOrder = 3,
    }

    /// <summary>
    /// The <see cref="ParticleEmitter"/> is the base manager for any given pool of particles, holding all particles and
    /// initializers, updaters, spawners, materials and shape builders associated with the particles.
    /// </summary>
    [DataContract("ParticleEmitter")]
    public class ParticleEmitter : IDisposable
    {
        /// <summary>
        /// Used to indicate if Dispose(...) has been called already
        /// </summary>
        private bool disposed;

        /// <summary>
        /// The sorting policy we used for the <see cref="ParticleSorter"/>
        /// </summary>
        private EmitterSortingPolicy sortingPolicy = EmitterSortingPolicy.None;

        /// <summary>
        /// Number of particles waiting to be spawned
        /// </summary>
        [DataMemberIgnore]
        private int particlesToSpawn;

        /// <summary>
        /// The pool contains all particles in the current <see cref="ParticleEmitter"/>
        /// </summary>
        [DataMemberIgnore]
        private readonly ParticlePool pool;

        /// <summary>
        /// Enumerator which accesses all relevant particles in a sorted manner
        /// </summary>
        [DataMemberIgnore]
        internal IParticleSorter ParticleSorter;
        
        /// <summary>
        /// The RNG provides an easy seed-based random numbers
        /// </summary>
        [DataMemberIgnore]
        internal ParticleRandomSeedGenerator RandomSeedGenerator;

        /// <summary>
        /// RNG based on time uses the clock ticks and is almost always guaranteed to use different generators
        /// </summary>
        [DataMemberIgnore]
        private EmitterRandomSeedMethod randomSeedMethod = EmitterRandomSeedMethod.Time;

        [DataMemberIgnore]
        private readonly InitialDefaultFields initialDefaultFields;

        /// <summary>
        /// The default simulation space is World.
        /// </summary>
        [DataMemberIgnore]
        private EmitterSimulationSpace simulationSpace = EmitterSimulationSpace.World;

        /// <summary>
        /// Some parameters should be initialized when the emitter first runs, rather than in the constructor
        /// </summary>
        [DataMemberIgnore]
        private bool hasBeenInitialized;

        /// <summary>
        /// Particles will live for a number of seconds between these two values
        /// </summary>
        [DataMemberIgnore]
        private Vector2 particleLifetime = Vector2.One;

        /// <summary>
        /// If positive, forces particles to stay one frame more when they are about ot expire
        /// </summary>
        [DataMemberIgnore]
        public int DelayParticleDeath { get; set; } = 0;

        // Draw location can be different than the particle position if we are using local coordinate system
        private readonly ParticleTransform drawTransform = new ParticleTransform();
        private readonly ParticleTransform identityTransform = new ParticleTransform();

        /// <summary>
        /// A list of the required particle fields for the <see cref="ParticlePool"/>
        /// </summary>
        private readonly Dictionary<ParticleFieldDescription, int> requiredFields;

        /// <summary>
        /// If more than 0, the maxParticlesOverride will override the estimate for <see cref="MaxParticles"/>
        /// </summary>
        private int maxParticlesOverride;

        /// <summary>
        /// The vertex builder is used for rendering, and it builds the actual vertex buffer stream from particle data
        /// </summary>
        internal readonly ParticleVertexBuilder VertexBuilder = new ParticleVertexBuilder();

        /// <summary>
        /// Optional name for the emitter, so that it can be referenced from other emitters
        /// </summary>
        /// <userdoc>
        /// Optional name for the emitter, so that it can be referenced from other emitters
        /// </userdoc>
        private string emitterName;

        /// <summary>
        /// Cached parent particle system, used for notifications
        /// </summary>
        private ParticleSystem cachedParentSystem = null;

        /// <summary>
        /// Cached parent particle system, used for notifications
        /// </summary>
        internal ParticleSystem CachedParticleSystem => cachedParentSystem;
        
        /// <summary>
        /// Default constructor. Initializes the pool and all collections contained in the <see cref="ParticleEmitter"/>
        /// </summary>
        public ParticleEmitter()
        {
            pool = new ParticlePool(0, 0);
            PoolChangedNotification();
            requiredFields = new Dictionary<ParticleFieldDescription, int>();

            // For now all particles require Life and RandomSeed fields, always
            AddRequiredField(ParticleFields.RemainingLife);
            AddRequiredField(ParticleFields.RandomSeed);
            AddRequiredField(ParticleFields.Position);

            initialDefaultFields = new InitialDefaultFields();

            Initializers = new FastTrackingCollection<ParticleInitializer>();
            Initializers.CollectionChanged += ModulesChanged;

            Updaters = new FastTrackingCollection<ParticleUpdater>();
            Updaters.CollectionChanged += ModulesChanged;

            Spawners = new FastTrackingCollection<ParticleSpawner>();
            Spawners.CollectionChanged += SpawnersChanged;        
        }

        /// <summary>
        /// Gets the current living particles from this emitter's pool
        /// </summary>
        [DataMemberIgnore]
        public int LivingParticles => pool.LivingParticles;

        [DataMemberIgnore]
        internal ParticlePool Pool => pool;

        /// <summary>
        /// Maximum number of particles this <see cref="ParticleEmitter"/> can have at any given time
        /// </summary>
        [DataMemberIgnore]
        public int MaxParticles { get; private set; }

        [DataMemberIgnore]
        internal bool DirtyParticlePool { get; set; }

        /// <summary>
        /// Indicates if the emitter is allowed to emit new particles or not.
        /// </summary>
        [DataMemberIgnore]
        public bool CanEmitParticles { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ParticleEmitter"/> is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        [DataMember(-10)]
        [DefaultValue(true)]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// The emitter name is used to uniquely identify this emitter within the same particle system
        /// </summary>
        /// <userdoc>
        /// The emitter name is used to uniquely identify this emitter within the same particle system
        /// </userdoc>
        [DataMember(1)]
        [Display("Emitter Name")]
        [DefaultValue(null)]
        public string EmitterName
        {
            get { return emitterName; }
            set
            {
                emitterName = value;

                // The emitter's name is used for creating child-parent relations between emitters and changing it should invalidate those relations
                cachedParentSystem?.InvalidateRelations();
            }
        }

        /// <summary>
        /// Maximum particles (if positive) overrides the maximum particle count limitation
        /// </summary>
        /// <userdoc>
        /// Leave it 0 for unlimited (automatic) pool size. If positive, it limits the maximum number of living particles this Emitter can have at any given time.
        /// </userdoc>
        [DataMember(5)]
        [Display("Max particles")]
        [DefaultValue(0)]
        public int MaxParticlesOverride
        {
            get { return maxParticlesOverride; }
            set
            {
                DirtyParticlePool = true;
                maxParticlesOverride = value;
            }
        }

        [DataMember(7)]
        [Display("Lifespan")]
        public Vector2 ParticleLifetime
        {
            get { return particleLifetime; }
            set
            {
                if (value.X <= 0 || value.Y < value.X)
                    return;

                DirtyParticlePool = true;
                particleLifetime = value;
            }
        }

        /// <summary>
        /// Simulation space defines if the particles should be born in world space, or local to the emitter
        /// </summary>
        /// <userdoc>
        /// World space particles persist in world space after they are born and do not automatically change with the Emitter. Local space particles persist in the Emitter's local space and follow it whenever the Emitter's locator changes.
        /// </userdoc>
        [DataMember(11)]
        [Display("Space")]
        [DefaultValue(EmitterSimulationSpace.World)]
        public EmitterSimulationSpace SimulationSpace
        {
            get { return simulationSpace; }
            set
            {
                if (value == simulationSpace)
                    return;

                simulationSpace = value;

                SimulationSpaceChanged();
            }
        }

        /// <summary>
        /// Random numbers in the <see cref="ParticleSystem"/> are generated based on a seed, which in turn can be generated using several methods.
        /// </summary>
        /// <userdoc>
        /// All random numbers in the particle system are based on a seed. If you use deterministic seeds, your particles will always behave the same way every time you start the simulation.
        /// </userdoc>
        [DataMember(12)]
        [Display("Randomize")]
        [DefaultValue(EmitterRandomSeedMethod.Time)]
        public EmitterRandomSeedMethod RandomSeedMethod
        {
            get
            {
                return randomSeedMethod;
            }

            set
            {
                randomSeedMethod = value;
                hasBeenInitialized = false;
            }
        }

        /// <summary>
        /// DrawPriority is used to sort emitters at the same position based on a user defined key and determine the order in which they will be drawn 
        /// </summary>
        /// <userdoc>
        /// DrawPriority determines draw order within the same particle system. Higher priority appears on the top.
        /// </userdoc>
        [DataMember(33)]
        [Display("Draw Priority")]
        [DefaultValue(0)]
        public byte DrawPriority { get; set; }

        /// <summary>
        /// How and if particles are sorted, and how they are access during rendering
        /// </summary>
        /// <userdoc>
        /// Choose if the particles should be soretd by depth (visually correct), age or not at all (fastest, good for additive blending)
        /// </userdoc>
        [DataMember(35)]
        [Display("Sorting")]
        [DefaultValue(EmitterSortingPolicy.None)]
        public EmitterSortingPolicy SortingPolicy
        {
            get { return sortingPolicy; }
            set
            {
                sortingPolicy = value;
                PoolChangedNotification();
            }
        }

        /// <summary>
        /// The <see cref="ShapeBuilders.ShapeBuilder"/> expands all living particles to vertex buffers for rendering
        /// </summary>
        /// <userdoc>
        /// The shape defines how each particle is expanded when rendered (camera-facing billboards, oriented quads, ribbons, etc.)
        /// </userdoc>
        [DataMember(40)]
        [Display("Shape")]
        [NotNull]
        public ShapeBuilder ShapeBuilder { get; set; } = new ShapeBuilderBillboard();

        /// <summary>
        /// The <see cref="ParticleMaterial"/> may update the vertex buffer, and it also applies the <see cref="Effect"/> required for rendering
        /// </summary>
        /// <userdoc>
        /// Material defines what effects, textures, coloring and other techniques are used when rendering the particles.
        /// </userdoc>
        [DataMember(50)]
        [Display("Material")]
        [NotNull]
        public ParticleMaterial Material { get; set; } = new ParticleMaterialComputeColor();

        /// <summary>
        /// List of <see cref="ParticleSpawner"/> to spawn particles in this <see cref="ParticleEmitter"/>
        /// </summary>
        /// <userdoc>
        /// Spawners define when, how, and how many particles are spawned. There can be several spawners.
        /// </userdoc>
        [DataMember(55)]
        [Display("Spawners")]
        [MemberCollection(CanReorderItems = true, NotNullItems = true)]
        public readonly FastTrackingCollection<ParticleSpawner> Spawners;

        /// <summary>
        /// List of <see cref="ParticleInitializer"/> within thie <see cref="ParticleEmitter"/>. Adjust <see cref="requiredFields"/> automatically
        /// </summary>
        /// <userdoc>
        /// Initializers set initial values for fields of particles which just spawned. Have no effect on already-spawned particles.
        /// </userdoc>
        [DataMember(200)]
        [Display("Initializers")]
        [MemberCollection(CanReorderItems = true, NotNullItems = true)]
        public readonly FastTrackingCollection<ParticleInitializer> Initializers;

        /// <summary>
        /// List of <see cref="ParticleUpdater"/> within thie <see cref="ParticleEmitter"/>. Adjust <see cref="requiredFields"/> automatically
        /// </summary>
        /// <userdoc>
        /// Updaters change the fields of all living particles every frame, like position, velocity, color, size etc.
        /// </userdoc>
        [DataMember(300)]
        [Display("Updaters")]
        [MemberCollection(CanReorderItems = true, NotNullItems = true)]
        public readonly FastTrackingCollection<ParticleUpdater> Updaters;

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;

            // Dispose unmanaged resources
            ResetSimulation(); // Will set particle count = 0, freeing unmanaged and graphics memory

            // Dispose managed resources
            pool?.Dispose();
        }

        /// <summary>
        /// If the particle pool has changed the sorter must also be updated to reflect those changes
        /// </summary>
        private void PoolChangedNotification()
        {
            if (SortingPolicy == EmitterSortingPolicy.None || pool.ParticleCapacity <= 0)
            {
                ParticleSorter = new ParticleSorterDefault(pool);
                return;
            }

            if (SortingPolicy == EmitterSortingPolicy.ByDepth)
            {
                ParticleSorter = new ParticleSorterDepth(pool);
                return;
            }

            if (SortingPolicy == EmitterSortingPolicy.ByAge)
            {
                ParticleSorter = new ParticleSorterAge(pool);
                return;
            }

            if (SortingPolicy == EmitterSortingPolicy.ByOrder)
            {
                ParticleSorter = new ParticleSorterOrder(pool);
                return;
            }

            // Default - no sorting
            ParticleSorter = new ParticleSorterDefault(pool);
        }

        #region Modules

        /// <summary>
        /// Notification that the modules (plugins) have changed.
        /// Pool's fields may need to be updated if new are required or old ones are no longer needed.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event arguments</param>
        private void ModulesChanged(object sender, ref FastTrackingCollectionChangedEventArgs e)
        {
            var module = e.Item as ParticleModule;
            if (module == null)
                return;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    module.AddFieldDescription = AddRequiredField;
                    module.RemoveFieldDescription = RemoveRequiredField;

                    if (module.Enabled)
                    {
                        for (int i = 0; i < module.RequiredFields.Count; i++)
                        {
                            AddRequiredField(module.RequiredFields[i]);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    module.AddFieldDescription = null;
                    module.RemoveFieldDescription = null;

                    if (module.Enabled)
                    {
                        for (int i = 0; i < module.RequiredFields.Count; i++)
                        {
                            RemoveRequiredField(module.RequiredFields[i]);
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Spawners have changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SpawnersChanged(object sender, ref FastTrackingCollectionChangedEventArgs e)
        {
            DirtyParticlePool = true;
        }
        #endregion

        #region Update

        /// <summary>
        /// Call this update when the <see cref="ParticleSystem"/> is paused to only update the renreding information
        /// </summary>
        /// <param name="parentSystem">The parent <see cref="ParticleSystem"/> containing this emitter</param>
        public void UpdatePaused(ParticleSystem parentSystem)
        {
            cachedParentSystem = parentSystem;

            UpdateLocations(parentSystem);
        }

        /// <summary>
        /// Updates all data changes before the emitter is updated this frame.
        /// This method is not thread-safe!
        /// </summary>
        public void PreUpdate()
        {
            ShapeBuilder.PreUpdate();

            foreach (var updater in Updaters)
            {
                updater.PreUpdate();
            }

            foreach (var initializer in Initializers)
            {
                initializer.PreUpdate();
            }
        }

        /// <summary>
        /// Updates the emitter and all its particles, and applies all updaters and spawners.
        /// </summary>
        /// <param name="dt">Delta time, elapsed time since the last call, in seconds</param>
        /// <param name="parentSystem">The parent <see cref="ParticleSystem"/> containing this emitter</param>
        public void Update(float dt, ParticleSystem parentSystem)
        {
            cachedParentSystem = parentSystem;

            if (!hasBeenInitialized)
            {
                DelayedInitialization(parentSystem);
            }

            UpdateLocations(parentSystem);

            EnsurePoolCapacity();

            MoveAndDeleteParticles(dt);

            ApplyParticleUpdaters(dt);

            SpawnNewParticles(dt);

            ApplyParticlePostUpdaters(dt);
        }

        /// <summary>
        /// Some parameters should be initialized when the emitter first runs, rather than in the constructor
        /// </summary>
        protected unsafe void DelayedInitialization(ParticleSystem parentSystem)
        {
            if (hasBeenInitialized)
                return;

            hasBeenInitialized = true;

            // RandomNumberGenerator creation
            {
                uint rngSeed = 0; // EmitterRandomSeedMethod.Fixed

                if (randomSeedMethod == EmitterRandomSeedMethod.Time)
                {
                    // Stopwatch has maximum possible frequency, so rngSeeds initialized at different times will be different
                    rngSeed = unchecked((uint)Stopwatch.GetTimestamp());
                }
                else if (randomSeedMethod == EmitterRandomSeedMethod.Position)
                {
                    // Different float have different uint representation so randomness should be good
                    // The only problem occurs when the three position components are the same
                    var posX = parentSystem.Translation.X;
                    var posY = parentSystem.Translation.Y;
                    var posZ = parentSystem.Translation.Z;

                    var uintX = *((uint*)(&posX));
                    var uintY = *((uint*)(&posY));
                    var uintZ = *((uint*)(&posZ));

                    // Add some randomness to prevent glitches when positions are the same (diagonal)
                    uintX ^= (uintX >> 19);
                    uintY ^= (uintY >> 8);

                    rngSeed = uintX ^ uintY ^ uintZ;
                }

                RandomSeedGenerator = new ParticleRandomSeedGenerator(rngSeed);
            }

        }

        /// <summary>
        /// Resets the simulation, deleting all particles and starting from Time = 0
        /// </summary>
        public void ResetSimulation()
        {
            // Reset the particle pool which allocates unmanaged memory
            DirtyParticlePool = true;
            pool.SetCapacity(0);

            // Reset the vertex builder which allocates graphics memory
            VertexBuilder.Reset();

            // Restart all spawners
            foreach (var spawner in Spawners)
            {
                spawner.ResetSimulation();
            }

            // Restart all updaters
            foreach (var updater in Updaters)
            {
                updater.ResetSimulation();
            }

            hasBeenInitialized = false;
        }

        /// <summary>
        /// Updates the location matrices of all elements. Needs to be called even when the particle system is paused for updating the render positions
        /// </summary>
        /// <param name="parentSystem"><see cref="ParticleSystem"/> containing this emitter</param>
        private void UpdateLocations(ParticleSystem parentSystem)
        {
            drawTransform.Position = parentSystem.Translation;
            drawTransform.Rotation = parentSystem.Rotation;
            drawTransform.ScaleUniform = parentSystem.UniformScale;
            drawTransform.SetParentTransform(null);

            if (simulationSpace == EmitterSimulationSpace.World)
            {
                // Update sub-systems
                initialDefaultFields.SetParentTRS(ref parentSystem.Translation, ref parentSystem.Rotation, parentSystem.UniformScale);

                foreach (var initializer in Initializers)
                {
                    initializer.SetParentTRS(drawTransform, parentSystem);
                }

                foreach (var updater in Updaters)
                {
                    updater.SetParentTRS(drawTransform, parentSystem);
                }
            }
            else
            {
                var posIdentity = Vector3.Zero;
                var rotIdentity = Quaternion.Identity;

                // Update sub-systems
                initialDefaultFields.SetParentTRS(ref posIdentity, ref rotIdentity, 1f);

                foreach (var initializer in Initializers)
                {
                    initializer.SetParentTRS(identityTransform, parentSystem);
                }

                foreach (var updater in Updaters)
                {
                    updater.SetParentTRS(identityTransform, parentSystem);
                }
            }
        }

        /// <summary>
        /// Should be called before the other methods from <see cref="Update"/> to ensure the pool has sufficient capacity to handle all particles.
        /// </summary>
        private void EnsurePoolCapacity()
        {
            if (!DirtyParticlePool)
                return;

            DirtyParticlePool = false;

            if (MaxParticlesOverride > 0)
            {
                MaxParticles = MaxParticlesOverride;
                pool.SetCapacity(MaxParticles);
                PoolChangedNotification();
                return;
            }

            var particlesPerSecond = 0;

            foreach (var spawnerBase in Spawners)
            {
                particlesPerSecond += spawnerBase.GetMaxParticlesPerSecond();
            }

            MaxParticles = (int)Math.Ceiling(particleLifetime.Y * particlesPerSecond);

            pool.SetCapacity(MaxParticles);
            PoolChangedNotification();
        }

        /// <summary>
        /// Should be called before <see cref="ApplyParticleUpdaters"/> to ensure dead particles are removed before they are updated
        /// </summary>
        /// <param name="dt">Delta time, elapsed time since the last call, in seconds</param>
        private unsafe void MoveAndDeleteParticles(float dt)
        {
            // Hardcoded life update
            if (pool.FieldExists(ParticleFields.RemainingLife) && pool.FieldExists(ParticleFields.RandomSeed))
            {
                var lifeField = pool.GetField(ParticleFields.RemainingLife);
                var randField = pool.GetField(ParticleFields.RandomSeed);
                var lifeStep = particleLifetime.Y - particleLifetime.X;

                var particleEnumerator = pool.GetEnumerator();
                while (particleEnumerator.MoveNext())
                {
                    var particle = particleEnumerator.Current;

                    var randSeed = *(RandomSeed*)(particle[randField]);
                    var life = (float*)particle[lifeField];

                    if (*life > 1)
                        *life = 1;

                    var startingLife = particleLifetime.X + lifeStep * randSeed.GetFloat(0);

                    if (*life <= MathUtil.ZeroTolerance)
                    {
                        particleEnumerator.RemoveCurrent(ref particle);
                    }
                    else
                    if ((*life -= (dt / startingLife)) <= MathUtil.ZeroTolerance)
                    {
                        if (DelayParticleDeath > 0)
                        {
                            *life = MathUtil.ZeroTolerance;
                        }
                        else
                        {
                            particleEnumerator.RemoveCurrent(ref particle);
                        }
                    }
                }
            }

            // Hardcoded position and old position updates
            // If we have to preserve the particle's old position, do it before updating the position for the first time
            if (pool.FieldExists(ParticleFields.Position) && pool.FieldExists(ParticleFields.OldPosition))
            {
                var posField = pool.GetField(ParticleFields.Position);
                var oldField = pool.GetField(ParticleFields.OldPosition);

                foreach (var particle in pool)
                {
                    (*((Vector3*)particle[oldField])) = (*((Vector3*)particle[posField]));
                }
            }

            // Hardcoded position and velocity update
            if (pool.FieldExists(ParticleFields.Position) && pool.FieldExists(ParticleFields.Velocity))
            {
                var posField = pool.GetField(ParticleFields.Position);
                var velField = pool.GetField(ParticleFields.Velocity);

                foreach (var particle in pool)
                {
                    var pos = ((Vector3*)particle[posField]);
                    var vel = ((Vector3*)particle[velField]);

                    *pos += *vel*dt;
                }
            }
        }

        /// <summary>
        /// Should be called before <see cref="SpawnNewParticles"/> to ensure new particles are not moved the frame they spawn
        /// </summary>
        /// <param name="dt">Delta time, elapsed time since the last call, in seconds</param>
        private void ApplyParticleUpdaters(float dt)
        {
            foreach (var updater in Updaters)
            {
                if (updater.Enabled && !updater.IsPostUpdater)
                    updater.Update(dt, pool);
            }
        }

        /// <summary>
        /// Should be called after <see cref="SpawnNewParticles"/> to ensure ALL particles are updated, not only the old ones
        /// </summary>
        /// <param name="dt">Delta time, elapsed time since the last call, in seconds</param>
        private void ApplyParticlePostUpdaters(float dt)
        {
            foreach (var updater in Updaters)
            {
                if (updater.Enabled && updater.IsPostUpdater)
                    updater.Update(dt, pool);
            }
        }

        /// <summary>
        /// Spawns new particles and in general should be one of the last methods to call from the <see cref="Update"/> method
        /// </summary>
        /// <param name="dt">Delta time, elapsed time since the last call, in seconds</param>
        private unsafe void SpawnNewParticles(float dt)
        {
            foreach (var spawner in Spawners)
            {
                if (spawner.Enabled)
                    spawner.SpawnNew(dt, this);
            }

            var capacity = pool.ParticleCapacity;
            if (capacity <= 0)
            {
                particlesToSpawn = 0;
                return;
            }

            // Sometimes particles will be spawned when there is no available space
            // In such occasions we have to buffer them and spawn them when space becomes available
            particlesToSpawn = Math.Min(pool.AvailableParticles, particlesToSpawn);

            if (particlesToSpawn <= 0)
            {
                particlesToSpawn = 0;
                return;
            }

            var lifeField = pool.GetField(ParticleFields.RemainingLife);
            var randField = pool.GetField(ParticleFields.RandomSeed);

            var startIndex = pool.NextFreeIndex % capacity;

            for (var i = 0; i < particlesToSpawn; i++)
            {
                var particle = pool.AddParticle();

                var randSeed = RandomSeedGenerator.GetNextSeed();

                *((RandomSeed*)particle[randField]) = randSeed;

                *((float*)particle[lifeField]) = 1; // Start at 100% normalized lifetime                
            }

            var endIndex = pool.NextFreeIndex % capacity;

            if (startIndex == endIndex)
            {
                // All particles are spawned in the same frame so change indices to 0 .. MAX
                startIndex = 0;
                endIndex = capacity;
                capacity++; // Prevent looping
            }

            particlesToSpawn = 0;

            initialDefaultFields.Initialize(pool, startIndex, endIndex, capacity);

            foreach (var initializer in Initializers)
            {
                if (initializer.Enabled)
                    initializer.Initialize(pool, startIndex, endIndex, capacity);
            }
        }

        #endregion

        #region ParticleFields

        /// <summary>
        /// Add a particle field required by some dependent module. If the module already exists in the pool, only its reference counter is increased.
        /// </summary>
        /// <param name="description"></param>
        internal void AddRequiredField(ParticleFieldDescription description)
        {
            int fieldReferences;
            if (requiredFields.TryGetValue(description, out fieldReferences))
            {
                // Field already exists. Increase the reference counter by 1
                requiredFields[description] = fieldReferences + 1;
                return;
            }

            // Check if the pool doesn't already have too many fields
            if (requiredFields.Count >= ParticlePool.DefaultMaxFielsPerPool)
                return;

            if (!pool.FieldExists(description, forceCreate: true))
                return;

            requiredFields.Add(description, 1);
        }

        /// <summary>
        /// Remove a particle field no longer required by a dependent module. It only gets removed from the pool if it reaches 0 reference counters.
        /// </summary>
        /// <param name="description"></param>
        internal void RemoveRequiredField(ParticleFieldDescription description)
        {
            int fieldReferences;
            if (requiredFields.TryGetValue(description, out fieldReferences))
            {
                requiredFields[description] = fieldReferences - 1;

                // If this was not the last field, other Updaters are still using it so don't remove it from the pool
                if (fieldReferences > 1)
                    return;

                pool.RemoveField(description);

                requiredFields.Remove(description);
            }

            // This line can be reached when a AddModule was unsuccessful and the required fields should be cleaned up
        }

        #endregion

        /// <summary>
        /// <see cref="PrepareForDraw"/> prepares and updates the Material, ShapeBuilder and VertexBuilder if necessary
        /// </summary>
        public void PrepareForDraw(out bool vertexBufferHasChanged, out int vertexSize, out int vertexCount)
        {
            var fieldsList = new ParticlePoolFieldsList(pool);

            // User changes to materials might cause vertex layout change
            Material.PrepareVertexLayout(fieldsList);

            // User changes to shape builders might cause vertex layout change
            ShapeBuilder.PrepareVertexLayout(fieldsList);

            // Recalculate the required vertex count and stride
            VertexBuilder.SetRequiredQuads(ShapeBuilder.QuadsPerParticle, pool.LivingParticles, pool.ParticleCapacity);

            vertexBufferHasChanged = (Material.HasVertexLayoutChanged || ShapeBuilder.VertexLayoutHasChanged || VertexBuilder.IsBufferDirty);

            // Update the vertex builder and the vertex layout if needed
            if (vertexBufferHasChanged)
            {
                VertexBuilder.ResetVertexElementList();

                Material.UpdateVertexBuilder(VertexBuilder);

                ShapeBuilder.UpdateVertexBuilder(VertexBuilder);

                VertexBuilder.UpdateVertexLayout();
            }

            vertexSize = VertexBuilder.VertexDeclaration.CalculateSize();
            vertexCount = VertexBuilder.VertexCount;
            VertexBuilder.SetDirty(false);
        }

        /// <summary>
        /// Build the vertex buffer from particle data
        /// </summary>
        /// <param name="sharedBufferPtr">The shared vertex buffer position where the particle data should be output</param>
        /// <param name="invViewMatrix">The current camera's inverse view matrix</param>
        public void BuildVertexBuffer(IntPtr sharedBufferPtr, ref Matrix invViewMatrix, ref Matrix viewProj)
        {
            // Get camera-space X and Y axes for billboard expansion and sort the particles if needed
            var unitX = new Vector3(invViewMatrix.M11, invViewMatrix.M12, invViewMatrix.M13);
            var unitY = new Vector3(invViewMatrix.M21, invViewMatrix.M22, invViewMatrix.M23);

            // Not the best solution, might want to improve
            var depthVector = Vector3.Cross(unitX, unitY);
            if (simulationSpace == EmitterSimulationSpace.Local)
            {
                var inverseRotation = drawTransform.WorldRotation;
                inverseRotation.W *= -1;
                inverseRotation.Rotate(ref depthVector);
            }
            var sortedList = ParticleSorter.GetSortedList(depthVector);

            // If the particles are in world space they don't need to be fixed as their coordinates are already in world space
            // If the particles are in local space they need to be drawn in world space using the emitter's current location matrix
            var posIdentity = Vector3.Zero;
            var rotIdentity = Quaternion.Identity;
            var scaleIdentity = 1f;
            if (simulationSpace == EmitterSimulationSpace.Local)
            {
                posIdentity = drawTransform.WorldPosition;
                rotIdentity = drawTransform.WorldRotation;
                scaleIdentity = drawTransform.WorldScale.X;
            }

            ParticleBufferState bufferState = new ParticleBufferState(sharedBufferPtr, VertexBuilder);

            ShapeBuilder.SetRequiredQuads(ShapeBuilder.QuadsPerParticle, pool.LivingParticles, pool.ParticleCapacity);
            ShapeBuilder.BuildVertexBuffer(ref bufferState, unitX, unitY, ref posIdentity, ref rotIdentity, scaleIdentity, ref sortedList, ref viewProj);

            bufferState.StartOver();
            Material.PatchVertexBuffer(ref bufferState, unitX, unitY, ref sortedList);

            ParticleSorter.FreeSortedList(ref sortedList);
        }

        #region Particles

        /// <summary>
        /// Requests the emitter to spawn several new particles.
        /// The particles are buffered and will be spawned during the <see cref="Update"/> step
        /// </summary>
        /// <param name="count"></param>
        public void EmitParticles(int count)
        {
            if (!CanEmitParticles)
                return;

            particlesToSpawn += count;
        }

        /// <summary>
        /// Changes the particle fields whenever the simulation space changes (World to Local or Local to World)
        /// This is a strictly debug feature so it (probably) won't be invoked during the game (unless changing the simulation space is intended?)
        /// </summary>
        private void SimulationSpaceChanged()
        {
            if (simulationSpace == EmitterSimulationSpace.Local)
            {
                // World -> Local

                var negativeTranslation = -drawTransform.WorldPosition;
                var negativeScale = (drawTransform.WorldScale.X > 0) ? 1f/ drawTransform.WorldScale.X : 1f;
                var negativeRotation = drawTransform.WorldRotation;
                negativeRotation.Conjugate();

                if (pool.FieldExists(ParticleFields.Position))
                {
                    var posField = pool.GetField(ParticleFields.Position);

                    foreach (var particle in pool)
                    {
                        var position = particle.Get(posField);

                        position = position + negativeTranslation;
                        position = position * negativeScale;

                        negativeRotation.Rotate(ref position);

                        particle.Set(posField, position);
                    }
                }

                if (pool.FieldExists(ParticleFields.OldPosition))
                {
                    var posField = pool.GetField(ParticleFields.OldPosition);

                    foreach (var particle in pool)
                    {
                        var position = particle.Get(posField);

                        position = position + negativeTranslation;
                        position = position * negativeScale;

                        negativeRotation.Rotate(ref position);

                        particle.Set(posField, position);
                    }
                }

                if (pool.FieldExists(ParticleFields.Velocity))
                {
                    var velField = pool.GetField(ParticleFields.Velocity);

                    foreach (var particle in pool)
                    {
                        var velocity = particle.Get(velField);

                        velocity = velocity * negativeScale;

                        negativeRotation.Rotate(ref velocity);

                        particle.Set(velField, velocity);
                    }
                }

                if (pool.FieldExists(ParticleFields.Size))
                {
                    var sizeField = pool.GetField(ParticleFields.Size);

                    foreach (var particle in pool)
                    {
                        var size = particle.Get(sizeField);

                        size = size * negativeScale;

                        particle.Set(sizeField, size);
                    }
                }

                // TODO Rotation

            }
            else
            {
                // Local -> World

                if (pool.FieldExists(ParticleFields.Position))
                {
                    var posField = pool.GetField(ParticleFields.Position);

                    foreach (var particle in pool)
                    {
                        var position = particle.Get(posField);

                        drawTransform.WorldRotation.Rotate( ref position );

                        position = position * drawTransform.WorldScale.X + drawTransform.WorldPosition;

                        particle.Set(posField, position);
                    }
                }

                if (pool.FieldExists(ParticleFields.OldPosition))
                {
                    var posField = pool.GetField(ParticleFields.OldPosition);

                    foreach (var particle in pool)
                    {
                        var position = particle.Get(posField);

                        drawTransform.WorldRotation.Rotate(ref position);

                        position = position * drawTransform.WorldScale.X + drawTransform.WorldPosition;

                        particle.Set(posField, position);
                    }
                }

                if (pool.FieldExists(ParticleFields.Velocity))
                {
                    var velField = pool.GetField(ParticleFields.Velocity);

                    foreach (var particle in pool)
                    {
                        var velocity = particle.Get(velField);

                        drawTransform.WorldRotation.Rotate(ref velocity);

                        velocity = velocity * drawTransform.WorldScale.X;

                        particle.Set(velField, velocity);
                    }
                }

                if (pool.FieldExists(ParticleFields.Size))
                {
                    var sizeField = pool.GetField(ParticleFields.Size);

                    foreach (var particle in pool)
                    {
                        var size = particle.Get(sizeField);

                        size = size * drawTransform.WorldScale.X;

                        particle.Set(sizeField, size);
                    }
                }

                // TODO Rotation

            }
        }

        #endregion

        /// <summary>
        /// Invalidates relation of this emitter to any other emitters that might be referenced
        /// </summary>
        public void InvalidateRelations()
        {
            foreach (var particleSpawner in Spawners)
            {
                particleSpawner.InvalidateRelations();
            }

            foreach (var initializer in Initializers)
            {
                initializer.InvalidateRelations();
            }

            foreach (var updater in Updaters)
            {
                updater.InvalidateRelations();
            }
        }
    }
}
