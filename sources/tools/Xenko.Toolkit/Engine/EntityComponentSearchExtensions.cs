using Xenko.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xenko.Toolkit.Collections;

namespace Xenko.Toolkit.Engine
{
    /// <summary>
    /// Extensions for searching for an <see cref="EntityComponent"/> in an <see cref="Entity"/> hierarchy.
    /// </summary>
    public static class EntityComponentSearchExtensions
    {

        /// <summary>
        /// Performs a breadth first search of the entities children for a component of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of component.</typeparam>
        /// <param name="entity">The entity.</param>
       /// <returns>The component or null if does no exist.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static T GetComponentInChildren<T>(this Entity entity) 
            where T : EntityComponent
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            //breadth first
            var queue = new Queue<Entity>();
            queue.EnqueueRange(entity.GetChildren());

            return GetComponentInChildrenCore<T>(queue);
        }

        /// <summary>
        /// Performs a breadth first search of the entity and it's children for a component of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of component.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <returns>The component or null if does no exist.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static T GetComponentInChildrenAndSelf<T>(this Entity entity) 
            where T : EntityComponent
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            //breadth first
            var queue = new Queue<Entity>();
            queue.Enqueue(entity);

            return GetComponentInChildrenCore<T>(queue);
        }

        private static T GetComponentInChildrenCore<T>(Queue<Entity> queue) 
            where T : EntityComponent
        {
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();


                var component = current.Get<T>();

                if (component != null)
                {
                    return component;
                }

                var children = current.Transform.Children;

                for (int i = 0; i < children.Count; i++)
                {
                    queue.Enqueue(children[i].Entity);
                }
            }

            return null;
        }

        /// <summary>
        /// Performs a breadth first search of the entities children for a component of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of component.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="includeDisabled">Should search include <see cref="ActivableEntityComponent"/> where <see cref="ActivableEntityComponent.Enabled"/> is <c>false</c>.</param>
        /// <returns>The component or null if does no exist.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static T GetComponentInChildren<T>(this Entity entity, bool includeDisabled = false) 
            where T : ActivableEntityComponent
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            //breadth first
            var queue = new Queue<Entity>();
            queue.EnqueueRange(entity.GetChildren());
            return GetComponentInChildrenCore<T>(includeDisabled, queue);
        }

        /// <summary>
        /// Performs a breadth first search of the entity and it's children for a component of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of component.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="includeDisabled">Should search include <see cref="ActivableEntityComponent"/> where <see cref="ActivableEntityComponent.Enabled"/> is <c>false</c>.</param>
        /// <returns>The component or null if does no exist.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static T GetComponentInChildrenAndSelf<T>(this Entity entity, bool includeDisabled = false) 
            where T : ActivableEntityComponent
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            //breadth first
            var queue = new Queue<Entity>();
            queue.Enqueue(entity);
            return GetComponentInChildrenCore<T>(includeDisabled, queue);
        }

        private static T GetComponentInChildrenCore<T>(bool includeDisabled, Queue<Entity> queue) 
            where T : ActivableEntityComponent
        {
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();


                var component = current.Get<T>();

                if (component != null && (component.Enabled || includeDisabled))
                {
                    return component;
                }

                var children = current.Transform.Children;

                for (int i = 0; i < children.Count; i++)
                {
                    queue.Enqueue(children[i].Entity);
                }
            }

            return null;
        }

        /// <summary>
        /// Performs a depth first search of the entities children for all components of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of component.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <returns>An iteration on the components.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static IEnumerable<T> GetComponentsInChildren<T>(this Entity entity) 
            where T : EntityComponent
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            //breadth first
            var queue = new Queue<Entity>();
            queue.EnqueueRange(entity.GetChildren());

            return GetComponentsInChildrenCore<T>(queue);
        }

        /// <summary>
        /// Performs a depth first search of the entity and it's children for all components of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of component.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <returns>An iteration on the components.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static IEnumerable<T> GetComponentsInChildrenAndSelf<T>(this Entity entity)
            where T : EntityComponent
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            //breadth first
            var queue = new Queue<Entity>();
            queue.Enqueue(entity);
            queue.EnqueueRange(entity.GetChildren());

            return GetComponentsInChildrenCore<T>(queue);
        }

        private static IEnumerable<T> GetComponentsInChildrenCore<T>(Queue<Entity> queue) 
            where T : EntityComponent
        {
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                foreach (var component in current.GetAll<T>())
                {
                    yield return component;
                }
            }
        }

        /// <summary>
        /// Performs a depth first search of the entities children for all components of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of component.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="includeDisabled">Should search include <see cref="ActivableEntityComponent"/> where <see cref="ActivableEntityComponent.Enabled"/> is <c>false</c>.</param>
        /// <returns>An iteration on the components.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static IEnumerable<T> GetComponentsInChildren<T>(this Entity entity, bool includeDisabled = false) 
            where T : ActivableEntityComponent
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));


            //breadth first
            var queue = new Queue<Entity>();
            queue.EnqueueRange(entity.GetChildren());

            return GetComponentsInChildrenCore<T>(queue);
        }

        /// <summary>
        /// Performs a depth first search of the entity and it's children for all components of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of component.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="includeDisabled">Should search include <see cref="ActivableEntityComponent"/> where <see cref="ActivableEntityComponent.Enabled"/> is <c>false</c>.</param>
        /// <returns>An iteration on the components.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static IEnumerable<T> GetComponentsInChildrenAndSelf<T>(this Entity entity, bool includeDisabled = false) 
            where T : ActivableEntityComponent
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));


            //breadth first
            var queue = new Queue<Entity>();
            queue.Enqueue(entity);
            queue.EnqueueRange(entity.GetChildren());

            return GetComponentsInChildrenCore<T>(queue);
        }

        private static IEnumerable<T> GetComponentsInChildrenCore<T>(Queue<Entity> queue, bool includeDisabled) 
            where T : ActivableEntityComponent
        {
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                foreach (var component in current.GetAll<T>())
                {
                    if (component.Enabled || includeDisabled)
                    {
                        yield return component;
                    }
                }
            }
        }


        /// <summary>
        /// Performs a depth first search of the entities decendants for all components of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of component.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <returns>An iteration on the components.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static IEnumerable<T> GetComponentsInDecendants<T>(this Entity entity) 
            where T : EntityComponent
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            //depth first
            var stack = new Stack<Entity>();
            stack.Push(entity);

            return GetComponentsInDecendantsCore<T>(stack, false);
        }

        /// <summary>
        /// Performs a depth first search of the entity and it's decendants for all components of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of component.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <returns>An iteration on the components.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static IEnumerable<T> GetComponentsInDecendantsAndSelf<T>(this Entity entity) 
            where T : EntityComponent
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            //depth first
            var stack = new Stack<Entity>();
            stack.Push(entity);

            return GetComponentsInDecendantsCore<T>(stack,true);
        }

        private static IEnumerable<T> GetComponentsInDecendantsCore<T>(Stack<Entity> stack, bool includeSelf) 
            where T : EntityComponent
        {
            var includeComponents = includeSelf;

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (includeComponents)
                {
                    foreach (var component in current.GetAll<T>())
                    {
                        yield return component;
                    } 
                }

                var children = current.Transform.Children;

                for (int i = children.Count - 1; i >= 0; i--)
                {
                    stack.Push(children[i].Entity);
                }
                includeComponents = true;
            }
        }

        /// <summary>
        /// Performs a depth first search of the entity and it's decendants for all components of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of component.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="includeDisabled">Should search include <see cref="ActivableEntityComponent"/> where <see cref="ActivableEntityComponent.Enabled"/> is <c>false</c>.</param>
        /// <returns>An iteration on the components.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static IEnumerable<T> GetComponentsInDecendants<T>(this Entity entity, bool includeDisabled = false) 
            where T : ActivableEntityComponent
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            //depth first
            var stack = new Stack<Entity>();
            stack.Push(entity);

            return GetComponentsInDecendantsCore<T>(stack, includeDisabled, false);
        }

        /// <summary>
        /// Performs a depth first search of the entity and it's decendants for all components of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of component.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="includeDisabled">Should search include <see cref="ActivableEntityComponent"/> where <see cref="ActivableEntityComponent.Enabled"/> is <c>false</c>.</param>
        /// <returns>An iteration on the components.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static IEnumerable<T> GetComponentsInDecendantsAndSelf<T>(this Entity entity, bool includeDisabled = false)
            where T : ActivableEntityComponent
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            //depth first
            var stack = new Stack<Entity>();
            stack.Push(entity);

            return GetComponentsInDecendantsCore<T>(stack, includeDisabled, true);
        }

        private static IEnumerable<T> GetComponentsInDecendantsCore<T>(Stack<Entity> stack, bool includeDisabled, bool includeSelf) 
            where T : ActivableEntityComponent
        {
            var includeComponents = includeSelf;

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (includeComponents)
                {
                    foreach (var component in current.GetAll<T>())
                    {
                        if (component.Enabled || includeDisabled)
                        {
                            yield return component;
                        }
                    } 
                }

                var children = current.Transform.Children;

                for (int i = children.Count - 1; i >= 0; i--)
                {
                    stack.Push(children[i].Entity);
                }
                includeComponents = true;
            }
        }

        /// <summary>
        /// Performs a search of the entity and it's ancestors for a component of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of component.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <returns>The component or <c>null</c> if does no exist.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static T GetComponentInParent<T>(this Entity entity) where T : EntityComponent
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var current = entity;

            do
            {
                var component = current.Get<T>();

                if (component != null)
                {
                    return component;
                }

            } while ((current = current.GetParent()) != null);

            return null;
        }

        /// <summary>
        /// Performs a search of the entity and it's ancestors for a component of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of component.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="includeDisabled">Should search include <see cref="ActivableEntityComponent"/> where <see cref="ActivableEntityComponent.Enabled"/> is <c>false</c>.</param>
        /// <returns>The component or <c>null</c> if does no exist.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static T GetComponentInParent<T>(this Entity entity, bool includeDisabled = false) where T : ActivableEntityComponent
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var current = entity;

            do
            {
                var component = current.Get<T>();

                if (component != null && (component.Enabled || includeDisabled))
                {
                    return component;
                }

            } while ((current = current.GetParent()) != null);

            return null;
        }

        /// <summary>
        /// Performs a search of the entity and it's ancestors for all components of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of component.</typeparam>
        /// <param name="entity">The entity.</param>
       /// <returns>An iteration on the components.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static IEnumerable<T> GetComponentsInParent<T>(this Entity entity) where T : EntityComponent
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var current = entity;

            do
            {
                foreach (var component in current.GetAll<T>())
                {
                    yield return component;
                }


            } while ((current = current.GetParent()) != null);
        }

        /// <summary>
        /// Performs a search of the entity and it's ancestors for all components of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of component.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="includeDisabled">Should search include <see cref="ActivableEntityComponent"/> where <see cref="ActivableEntityComponent.Enabled"/> is <c>false</c>.</param>
        /// <returns>An iteration on the components.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static IEnumerable<T> GetComponentsInParent<T>(this Entity entity, bool includeDisabled = false) where T : ActivableEntityComponent
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var current = entity;

            do
            {
                foreach (var component in current.GetAll<T>())
                {
                    if (component.Enabled || includeDisabled)
                    {
                        yield return component;
                    }
                }


            } while ((current = current.GetParent()) != null);
        }
    }

}
