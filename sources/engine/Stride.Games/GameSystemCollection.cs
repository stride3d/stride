// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Stride.Core;
using Stride.Core.Collections;
using Stride.Core.Diagnostics;

namespace Stride.Games
{
    /// <summary>A collection of game components.</summary>
    public class GameSystemCollection : TrackingCollection<IGameSystemBase>, IGameSystemCollection, IDisposable
    {
        private readonly List<IGameSystemBase> pendingGameSystems;
        private readonly List<KeyValuePair<IDrawable, ProfilingKey>> drawableGameSystems;
        private readonly List<KeyValuePair<IUpdateable, ProfilingKey>> updateableGameSystems;
        private readonly List<IContentable> contentableGameSystems;
        private readonly List<IContentable> currentlyContentGameSystems;
        private readonly List<KeyValuePair<IDrawable, ProfilingKey>> currentlyDrawingGameSystems;
        private readonly List<KeyValuePair<IUpdateable, ProfilingKey>> currentlyUpdatingGameSystems;
        private bool isFirstUpdateDone = false;

        public GameSystemCollection(IServiceRegistry registry)
        {
            drawableGameSystems = new List<KeyValuePair<IDrawable, ProfilingKey>>();
            currentlyContentGameSystems = new List<IContentable>();
            currentlyDrawingGameSystems = new List<KeyValuePair<IDrawable, ProfilingKey>>();
            pendingGameSystems = new List<IGameSystemBase>();
            updateableGameSystems = new List<KeyValuePair<IUpdateable, ProfilingKey>>();
            currentlyUpdatingGameSystems = new List<KeyValuePair<IUpdateable, ProfilingKey>>();
            contentableGameSystems = new List<IContentable>();

            // Register events on GameSystems.
            CollectionChanged += GameSystems_CollectionChanged;
        }

        /// <summary>
        /// Gets the state of this game system collection.
        /// </summary>
        /// <value>
        /// The state of this game system collection.
        /// </value>
        public GameSystemState State { get; private set; }

        /// <summary>
        /// Gets a value indicating whether first update has been done.
        /// </summary>
        /// <value>
        ///   <c>true</c> if first update has been done; otherwise, <c>false</c>.
        /// </value>
        public bool IsFirstUpdateDone { get { return isFirstUpdateDone; } }

        /// <summary>
        /// Reference page contains links to related conceptual articles.
        /// </summary>
        /// <param name="gameTime">
        /// Time passed since the last call to Update.
        /// </param>
        public virtual void Update(GameTime gameTime)
        {
            lock (updateableGameSystems)
            {
                foreach (var updateable in updateableGameSystems)
                {
                    currentlyUpdatingGameSystems.Add(updateable);
                }
            }

            foreach (var updateable in currentlyUpdatingGameSystems)
            {
                if (updateable.Key.Enabled)
                {
                    using (Profiler.Begin(updateable.Value))
                    {
                        updateable.Key.Update(gameTime);
                    }
                }
            }

            currentlyUpdatingGameSystems.Clear();
            isFirstUpdateDone = true;
        }

        /// <summary>
        /// Reference page contains code sample.
        /// </summary>
        /// <param name="gameTime">
        /// Time passed since the last call to Draw.
        /// </param>
        public virtual void Draw(GameTime gameTime)
        {
            // Just lock current drawable game systems to grab them in a temporary list.
            lock (drawableGameSystems)
            {
                for (int i = 0; i < drawableGameSystems.Count; i++)
                {
                    currentlyDrawingGameSystems.Add(drawableGameSystems[i]);
                }
            }

            for (int i = 0; i < currentlyDrawingGameSystems.Count; i++)
            {
                var drawable = currentlyDrawingGameSystems[i];
                if (drawable.Key.Visible)
                {
                    using (Profiler.Begin(drawable.Value))
                    {
                        if (drawable.Key.BeginDraw())
                        {
                            drawable.Key.Draw(gameTime);
                            drawable.Key.EndDraw();
                        }
                    }
                }
            }

            currentlyDrawingGameSystems.Clear();
        }

        /// <summary>
        /// Loads the content.
        /// </summary>
        public virtual void LoadContent()
        {
            if (State != GameSystemState.Initialized)
            {
                throw new InvalidOperationException("Not initialized.");
            }

            State = GameSystemState.ContentLoaded;

            lock (contentableGameSystems)
            {
                foreach (var contentable in contentableGameSystems)
                {
                    currentlyContentGameSystems.Add(contentable);
                }
            }

            foreach (var contentable in currentlyContentGameSystems)
            {
                using (var profile = Profiler.Begin(GameProfilingKeys.GameSystemLoadContent, GetGameSystemName(contentable)))
                    contentable.LoadContent();
            }

            currentlyContentGameSystems.Clear();
        }

        /// <summary>
        /// Called when graphics resources need to be unloaded. Override this method to unload any game-specific graphics resources.
        /// </summary>
        public virtual void UnloadContent()
        {
            if (State != GameSystemState.ContentLoaded)
            {
                throw new InvalidOperationException("Not running.");
            } 
            
            State = GameSystemState.Initialized;

            lock (contentableGameSystems)
            {
                foreach (var contentable in contentableGameSystems)
                {
                    currentlyContentGameSystems.Add(contentable);
                }
            }

            foreach (var contentable in currentlyContentGameSystems)
            {
                contentable.UnloadContent();
            }

            currentlyContentGameSystems.Clear();
        }

        public void Initialize()
        {
            if (State != GameSystemState.None)
            {
                throw new InvalidOperationException("Already initialized.");
            }

            State = GameSystemState.Initialized;

            InitializePendingGameSystems();
        }

        private void InitializePendingGameSystems()
        {
            // Add all game systems that were added to this game instance before the game started.
            while (pendingGameSystems.Count != 0)
            {
                var gameSystemName = GetGameSystemName(pendingGameSystems[0]);
                using (var profile = Profiler.Begin(GameProfilingKeys.GameSystemInitialize, gameSystemName))
                    pendingGameSystems[0].Initialize();
                if (State == GameSystemState.ContentLoaded && pendingGameSystems[0] is IContentable)
                {
                    var contentable = (IContentable)pendingGameSystems[0];
                    using (var profile = Profiler.Begin(GameProfilingKeys.GameSystemLoadContent, gameSystemName))
                        contentable.LoadContent();
                }

                pendingGameSystems.RemoveAt(0);
            }
        }

        public void Dispose()
        {
            var array = new IGameSystemBase[Count];
            CopyTo(array, 0);
            for (int i = array.Length - 1; i >= 0; i--)
            {
                var disposable = array[i] as IDisposable;
                disposable?.Dispose();
            }
        }

        private void GameSystems_CollectionChanged(object sender, TrackingCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                GameSystems_ItemAdded(sender, e);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                GameSystems_ItemRemoved(sender, e);
            }
        }

        private void GameSystems_ItemAdded(object sender, TrackingCollectionChangedEventArgs e)
        {
            var gameSystem = (IGameSystemBase)e.Item;

            // If the game is already running, then we can initialize the game system now
            if (State >= GameSystemState.Initialized)
            {
                gameSystem.Initialize();
            }
            else
            {
                // else we need to initialize it later
                pendingGameSystems.Add(gameSystem);
            }

            // Add a contentable system to a separate list
            var contentableSystem = gameSystem as IContentable;
            if (contentableSystem != null)
            {
                lock (contentableGameSystems)
                {
                    if (!contentableGameSystems.Contains(contentableSystem))
                    {
                        contentableGameSystems.Add(contentableSystem);
                    }
                }
                // Load the content of IContentable when running
                if (State >= GameSystemState.ContentLoaded)
                {
                    contentableSystem.LoadContent();
                }
            }

            // Add an updateable system to the separate list
            var updateableGameSystem = gameSystem as IUpdateable;
            if (updateableGameSystem != null && AddGameSystem(updateableGameSystem, updateableGameSystems, UpdateableComparer.Default, GameProfilingKeys.GameUpdate))
            {
                updateableGameSystem.UpdateOrderChanged += UpdateableGameSystem_UpdateOrderChanged;
            }

            // Add a drawable system to the separate list
            var drawableGameSystem = gameSystem as IDrawable;
            if (drawableGameSystem != null && AddGameSystem(drawableGameSystem, drawableGameSystems, DrawableComparer.Default, GameProfilingKeys.GameDraw))
            {
                drawableGameSystem.DrawOrderChanged += DrawableGameSystem_DrawOrderChanged;
            }
        }

        private void GameSystems_ItemRemoved(object sender, TrackingCollectionChangedEventArgs e)
        {
            var gameSystem = (IGameSystemBase)e.Item;

            if (State == GameSystemState.None)
            {
                pendingGameSystems.Remove(gameSystem);
            }

            var contentableSystem = gameSystem as IContentable;
            if (contentableSystem != null)
            {
                lock (contentableGameSystems)
                {
                    contentableGameSystems.Remove(contentableSystem);
                }

                // UnLoads the content of IContentable when running
                if (State == GameSystemState.ContentLoaded)
                {
                    contentableSystem.UnloadContent();
                }
            }

            var updateableSystem = gameSystem as IUpdateable;
            if (updateableSystem != null)
            {
                lock (updateableGameSystems)
                {
                    for(int i = 0; i < updateableGameSystems.Count; i++)
                    {
                        if(ReferenceEquals(updateableGameSystems[i].Key, updateableSystem))
                        {
                            updateableGameSystems.RemoveAt(i);
                            break;
                        }
                    }
                }

                updateableSystem.UpdateOrderChanged -= UpdateableGameSystem_UpdateOrderChanged;
            }

            var drawableSystem = gameSystem as IDrawable;
            if (drawableSystem != null)
            {
                lock (drawableGameSystems)
                {
                    for (int i = 0; i < drawableGameSystems.Count; i++)
                    {
                        if (ReferenceEquals(drawableGameSystems[i].Key, drawableSystem))
                        {
                            drawableGameSystems.RemoveAt(i);
                            break;
                        }
                    }
                }

                drawableSystem.DrawOrderChanged -= DrawableGameSystem_DrawOrderChanged;
            }
        }

        private void UpdateableGameSystem_UpdateOrderChanged(object sender, EventArgs e)
        {
            AddGameSystem((IUpdateable)sender, updateableGameSystems, UpdateableComparer.Default, GameProfilingKeys.GameUpdate, true);
        }

        private void DrawableGameSystem_DrawOrderChanged(object sender, EventArgs e)
        {
            AddGameSystem((IDrawable)sender, drawableGameSystems, DrawableComparer.Default, GameProfilingKeys.GameDraw, true);
        }

        private static bool AddGameSystem<T>(T gameSystem, List<KeyValuePair<T, ProfilingKey>> gameSystems, IComparer<KeyValuePair<T, ProfilingKey>> comparer, ProfilingKey parentProfilingKey, bool removePreviousSystem = false)
            where T : class
        {
            lock (gameSystems)
            {
                var gameSystemKey = new KeyValuePair<T, ProfilingKey>(gameSystem, null);

                // Find this gameSystem
                int index = -1;
                for (int i = 0; i < gameSystems.Count; ++i)
                {
                    if (gameSystem == gameSystems[i].Key)
                    {
                        index = i;
                    }
                }

                // If we are updating the order
                if (index >= 0)
                {
                    if (removePreviousSystem)
                    {
                        gameSystemKey = gameSystems[index];
                        gameSystems.RemoveAt(index);
                        index = -1;
                    }
                }
                else
                {
                    gameSystemKey = new KeyValuePair<T, ProfilingKey>(gameSystemKey.Key, new ProfilingKey(parentProfilingKey, gameSystem.GetType().Name));
                }

                if (index == -1)
                {
                    // we want to insert right after all other systems with same draw/update order
                    index = gameSystems.UpperBound(gameSystemKey, comparer, 0, gameSystems.Count);

                    gameSystems.Insert(index, gameSystemKey);

                    // True, the system was inserted
                    return true;
                }
            }

            // False, it is already in the list
            return false;
        }

        private static string GetGameSystemName(object gameSystem)
        {
            return gameSystem is IGameSystemBase ? ((IGameSystemBase)gameSystem).Name : gameSystem != null ? gameSystem.GetType().Name : "null";
        }

        /// <summary>
        /// The comparer used to order <see cref="IDrawable"/> objects.
        /// </summary>
        internal struct DrawableComparer : IComparer<KeyValuePair<IDrawable, ProfilingKey>>
        {
            public static readonly DrawableComparer Default = new DrawableComparer();

            public int Compare(KeyValuePair<IDrawable, ProfilingKey> leftValue, KeyValuePair<IDrawable, ProfilingKey> rightValue)
            {
                var left = leftValue.Key;
                var right = rightValue.Key;

                return left.DrawOrder.CompareTo(right.DrawOrder);
            }
        }

        /// <summary>
        /// The comparer used to order <see cref="IUpdateable"/> objects.
        /// </summary>
        internal struct UpdateableComparer : IComparer<KeyValuePair<IUpdateable, ProfilingKey>>
        {
            public static readonly UpdateableComparer Default = new UpdateableComparer();

            public int Compare(KeyValuePair<IUpdateable, ProfilingKey> leftValue, KeyValuePair<IUpdateable, ProfilingKey> rightValue)
            {
                var left = leftValue.Key;
                var right = rightValue.Key;

                return left.UpdateOrder.CompareTo(right.UpdateOrder);
            }
        }
    }
}
