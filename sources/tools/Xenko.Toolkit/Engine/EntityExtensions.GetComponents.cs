
using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Engine;

namespace Xenko.Toolkit.Engine
{
    public static partial class EntityExtensions
    {
        /// <summary>
        /// Gets the two specified components.
        /// </summary>
        /// <typeparam name="TComponent1">The type of the first component the method returns.</typeparam>
        /// <typeparam name="TComponent2">The type of the second component the method returns.</typeparam>
        /// <param name="entity">The <see cref="Entity"/> to get the components from.</param>
        /// <returns>The tuple of the two components or <c>null</c> if component does not exist.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static (TComponent1, TComponent2) Get<TComponent1, TComponent2>(this Entity entity)
            where TComponent1 : EntityComponent
            where TComponent2 : EntityComponent
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return (entity.Get<TComponent1>(), entity.Get<TComponent2>());
        }

        /// <summary>
        /// Gets the three specified components.
        /// </summary>
        /// <typeparam name="TComponent1">The type of the first component the method returns.</typeparam>
        /// <typeparam name="TComponent2">The type of the second component the method returns.</typeparam>
        /// <typeparam name="TComponent3">The type of the third component the method returns.</typeparam>
        /// <param name="entity">The <see cref="Entity"/> to get the components from.</param>
        /// <returns>The tuple of the three components or <c>null</c> if component does not exist.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static (TComponent1, TComponent2, TComponent3) Get<TComponent1, TComponent2, TComponent3>(this Entity entity)
            where TComponent1 : EntityComponent
            where TComponent2 : EntityComponent
            where TComponent3 : EntityComponent
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return (entity.Get<TComponent1>(), entity.Get<TComponent2>(), entity.Get<TComponent3>());
        }

        /// <summary>
        /// Gets the four specified components.
        /// </summary>
        /// <typeparam name="TComponent1">The type of the first component the method returns.</typeparam>
        /// <typeparam name="TComponent2">The type of the second component the method returns.</typeparam>
        /// <typeparam name="TComponent3">The type of the third component the method returns.</typeparam>
        /// <typeparam name="TComponent4">The type of the fourth component the method returns.</typeparam>
        /// <param name="entity">The <see cref="Entity"/> to get the components from.</param>
        /// <returns>The tuple of the four components or <c>null</c> if component does not exist.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static (TComponent1, TComponent2, TComponent3, TComponent4) Get<TComponent1, TComponent2, TComponent3, TComponent4>(this Entity entity)
            where TComponent1 : EntityComponent
            where TComponent2 : EntityComponent
            where TComponent3 : EntityComponent
            where TComponent4 : EntityComponent
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return (entity.Get<TComponent1>(), entity.Get<TComponent2>(), entity.Get<TComponent3>(), entity.Get<TComponent4>());
        }

        /// <summary>
        /// Gets the five specified components.
        /// </summary>
        /// <typeparam name="TComponent1">The type of the first component the method returns.</typeparam>
        /// <typeparam name="TComponent2">The type of the second component the method returns.</typeparam>
        /// <typeparam name="TComponent3">The type of the third component the method returns.</typeparam>
        /// <typeparam name="TComponent4">The type of the fourth component the method returns.</typeparam>
        /// <typeparam name="TComponent5">The type of the fifth component the method returns.</typeparam>
        /// <param name="entity">The <see cref="Entity"/> to get the components from.</param>
        /// <returns>The tuple of the five components or <c>null</c> if component does not exist.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static (TComponent1, TComponent2, TComponent3, TComponent4, TComponent5) Get<TComponent1, TComponent2, TComponent3, TComponent4, TComponent5>(this Entity entity)
            where TComponent1 : EntityComponent
            where TComponent2 : EntityComponent
            where TComponent3 : EntityComponent
            where TComponent4 : EntityComponent
            where TComponent5 : EntityComponent
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return (entity.Get<TComponent1>(), entity.Get<TComponent2>(), entity.Get<TComponent3>(), entity.Get<TComponent4>(), entity.Get<TComponent5>());
        }

        /// <summary>
        /// Gets the six specified components.
        /// </summary>
        /// <typeparam name="TComponent1">The type of the first component the method returns.</typeparam>
        /// <typeparam name="TComponent2">The type of the second component the method returns.</typeparam>
        /// <typeparam name="TComponent3">The type of the third component the method returns.</typeparam>
        /// <typeparam name="TComponent4">The type of the fourth component the method returns.</typeparam>
        /// <typeparam name="TComponent5">The type of the fifth component the method returns.</typeparam>
        /// <typeparam name="TComponent6">The type of the sixth component the method returns.</typeparam>
        /// <param name="entity">The <see cref="Entity"/> to get the components from.</param>
        /// <returns>The tuple of the six components or <c>null</c> if component does not exist.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static (TComponent1, TComponent2, TComponent3, TComponent4, TComponent5, TComponent6) Get<TComponent1, TComponent2, TComponent3, TComponent4, TComponent5, TComponent6>(this Entity entity)
            where TComponent1 : EntityComponent
            where TComponent2 : EntityComponent
            where TComponent3 : EntityComponent
            where TComponent4 : EntityComponent
            where TComponent5 : EntityComponent
            where TComponent6 : EntityComponent
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return (entity.Get<TComponent1>(), entity.Get<TComponent2>(), entity.Get<TComponent3>(), entity.Get<TComponent4>(), entity.Get<TComponent5>(), entity.Get<TComponent6>());
        }

        /// <summary>
        /// Gets the seven specified components.
        /// </summary>
        /// <typeparam name="TComponent1">The type of the first component the method returns.</typeparam>
        /// <typeparam name="TComponent2">The type of the second component the method returns.</typeparam>
        /// <typeparam name="TComponent3">The type of the third component the method returns.</typeparam>
        /// <typeparam name="TComponent4">The type of the fourth component the method returns.</typeparam>
        /// <typeparam name="TComponent5">The type of the fifth component the method returns.</typeparam>
        /// <typeparam name="TComponent6">The type of the sixth component the method returns.</typeparam>
        /// <typeparam name="TComponent7">The type of the seventh component the method returns.</typeparam>
        /// <param name="entity">The <see cref="Entity"/> to get the components from.</param>
        /// <returns>The tuple of the seven components or <c>null</c> if component does not exist.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static (TComponent1, TComponent2, TComponent3, TComponent4, TComponent5, TComponent6, TComponent7) Get<TComponent1, TComponent2, TComponent3, TComponent4, TComponent5, TComponent6, TComponent7>(this Entity entity)
            where TComponent1 : EntityComponent
            where TComponent2 : EntityComponent
            where TComponent3 : EntityComponent
            where TComponent4 : EntityComponent
            where TComponent5 : EntityComponent
            where TComponent6 : EntityComponent
            where TComponent7 : EntityComponent
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return (entity.Get<TComponent1>(), entity.Get<TComponent2>(), entity.Get<TComponent3>(), entity.Get<TComponent4>(), entity.Get<TComponent5>(), entity.Get<TComponent6>(), entity.Get<TComponent7>());
        }

        /// <summary>
        /// Gets the eight specified components.
        /// </summary>
        /// <typeparam name="TComponent1">The type of the first component the method returns.</typeparam>
        /// <typeparam name="TComponent2">The type of the second component the method returns.</typeparam>
        /// <typeparam name="TComponent3">The type of the third component the method returns.</typeparam>
        /// <typeparam name="TComponent4">The type of the fourth component the method returns.</typeparam>
        /// <typeparam name="TComponent5">The type of the fifth component the method returns.</typeparam>
        /// <typeparam name="TComponent6">The type of the sixth component the method returns.</typeparam>
        /// <typeparam name="TComponent7">The type of the seventh component the method returns.</typeparam>
        /// <typeparam name="TComponent8">The type of the eighth component the method returns.</typeparam>
        /// <param name="entity">The <see cref="Entity"/> to get the components from.</param>
        /// <returns>The tuple of the eight components or <c>null</c> if component does not exist.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static (TComponent1, TComponent2, TComponent3, TComponent4, TComponent5, TComponent6, TComponent7, TComponent8) Get<TComponent1, TComponent2, TComponent3, TComponent4, TComponent5, TComponent6, TComponent7, TComponent8>(this Entity entity)
            where TComponent1 : EntityComponent
            where TComponent2 : EntityComponent
            where TComponent3 : EntityComponent
            where TComponent4 : EntityComponent
            where TComponent5 : EntityComponent
            where TComponent6 : EntityComponent
            where TComponent7 : EntityComponent
            where TComponent8 : EntityComponent
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return (entity.Get<TComponent1>(), entity.Get<TComponent2>(), entity.Get<TComponent3>(), entity.Get<TComponent4>(), entity.Get<TComponent5>(), entity.Get<TComponent6>(), entity.Get<TComponent7>(), entity.Get<TComponent8>());
        }

        /// <summary>
        /// Gets the nine specified components.
        /// </summary>
        /// <typeparam name="TComponent1">The type of the first component the method returns.</typeparam>
        /// <typeparam name="TComponent2">The type of the second component the method returns.</typeparam>
        /// <typeparam name="TComponent3">The type of the third component the method returns.</typeparam>
        /// <typeparam name="TComponent4">The type of the fourth component the method returns.</typeparam>
        /// <typeparam name="TComponent5">The type of the fifth component the method returns.</typeparam>
        /// <typeparam name="TComponent6">The type of the sixth component the method returns.</typeparam>
        /// <typeparam name="TComponent7">The type of the seventh component the method returns.</typeparam>
        /// <typeparam name="TComponent8">The type of the eighth component the method returns.</typeparam>
        /// <typeparam name="TComponent9">The type of the ninth component the method returns.</typeparam>
        /// <param name="entity">The <see cref="Entity"/> to get the components from.</param>
        /// <returns>The tuple of the nine components or <c>null</c> if component does not exist.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static (TComponent1, TComponent2, TComponent3, TComponent4, TComponent5, TComponent6, TComponent7, TComponent8, TComponent9) Get<TComponent1, TComponent2, TComponent3, TComponent4, TComponent5, TComponent6, TComponent7, TComponent8, TComponent9>(this Entity entity)
            where TComponent1 : EntityComponent
            where TComponent2 : EntityComponent
            where TComponent3 : EntityComponent
            where TComponent4 : EntityComponent
            where TComponent5 : EntityComponent
            where TComponent6 : EntityComponent
            where TComponent7 : EntityComponent
            where TComponent8 : EntityComponent
            where TComponent9 : EntityComponent
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return (entity.Get<TComponent1>(), entity.Get<TComponent2>(), entity.Get<TComponent3>(), entity.Get<TComponent4>(), entity.Get<TComponent5>(), entity.Get<TComponent6>(), entity.Get<TComponent7>(), entity.Get<TComponent8>(), entity.Get<TComponent9>());
        }

        /// <summary>
        /// Gets the ten specified components.
        /// </summary>
        /// <typeparam name="TComponent1">The type of the first component the method returns.</typeparam>
        /// <typeparam name="TComponent2">The type of the second component the method returns.</typeparam>
        /// <typeparam name="TComponent3">The type of the third component the method returns.</typeparam>
        /// <typeparam name="TComponent4">The type of the fourth component the method returns.</typeparam>
        /// <typeparam name="TComponent5">The type of the fifth component the method returns.</typeparam>
        /// <typeparam name="TComponent6">The type of the sixth component the method returns.</typeparam>
        /// <typeparam name="TComponent7">The type of the seventh component the method returns.</typeparam>
        /// <typeparam name="TComponent8">The type of the eighth component the method returns.</typeparam>
        /// <typeparam name="TComponent9">The type of the ninth component the method returns.</typeparam>
        /// <typeparam name="TComponent10">The type of the tenth component the method returns.</typeparam>
        /// <param name="entity">The <see cref="Entity"/> to get the components from.</param>
        /// <returns>The tuple of the ten components or <c>null</c> if component does not exist.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static (TComponent1, TComponent2, TComponent3, TComponent4, TComponent5, TComponent6, TComponent7, TComponent8, TComponent9, TComponent10) Get<TComponent1, TComponent2, TComponent3, TComponent4, TComponent5, TComponent6, TComponent7, TComponent8, TComponent9, TComponent10>(this Entity entity)
            where TComponent1 : EntityComponent
            where TComponent2 : EntityComponent
            where TComponent3 : EntityComponent
            where TComponent4 : EntityComponent
            where TComponent5 : EntityComponent
            where TComponent6 : EntityComponent
            where TComponent7 : EntityComponent
            where TComponent8 : EntityComponent
            where TComponent9 : EntityComponent
            where TComponent10 : EntityComponent
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return (entity.Get<TComponent1>(), entity.Get<TComponent2>(), entity.Get<TComponent3>(), entity.Get<TComponent4>(), entity.Get<TComponent5>(), entity.Get<TComponent6>(), entity.Get<TComponent7>(), entity.Get<TComponent8>(), entity.Get<TComponent9>(), entity.Get<TComponent10>());
        }

        /// <summary>
        /// Gets the eleven specified components.
        /// </summary>
        /// <typeparam name="TComponent1">The type of the first component the method returns.</typeparam>
        /// <typeparam name="TComponent2">The type of the second component the method returns.</typeparam>
        /// <typeparam name="TComponent3">The type of the third component the method returns.</typeparam>
        /// <typeparam name="TComponent4">The type of the fourth component the method returns.</typeparam>
        /// <typeparam name="TComponent5">The type of the fifth component the method returns.</typeparam>
        /// <typeparam name="TComponent6">The type of the sixth component the method returns.</typeparam>
        /// <typeparam name="TComponent7">The type of the seventh component the method returns.</typeparam>
        /// <typeparam name="TComponent8">The type of the eighth component the method returns.</typeparam>
        /// <typeparam name="TComponent9">The type of the ninth component the method returns.</typeparam>
        /// <typeparam name="TComponent10">The type of the tenth component the method returns.</typeparam>
        /// <typeparam name="TComponent11">The type of the eleventh component the method returns.</typeparam>
        /// <param name="entity">The <see cref="Entity"/> to get the components from.</param>
        /// <returns>The tuple of the eleven components or <c>null</c> if component does not exist.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static (TComponent1, TComponent2, TComponent3, TComponent4, TComponent5, TComponent6, TComponent7, TComponent8, TComponent9, TComponent10, TComponent11) Get<TComponent1, TComponent2, TComponent3, TComponent4, TComponent5, TComponent6, TComponent7, TComponent8, TComponent9, TComponent10, TComponent11>(this Entity entity)
            where TComponent1 : EntityComponent
            where TComponent2 : EntityComponent
            where TComponent3 : EntityComponent
            where TComponent4 : EntityComponent
            where TComponent5 : EntityComponent
            where TComponent6 : EntityComponent
            where TComponent7 : EntityComponent
            where TComponent8 : EntityComponent
            where TComponent9 : EntityComponent
            where TComponent10 : EntityComponent
            where TComponent11 : EntityComponent
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return (entity.Get<TComponent1>(), entity.Get<TComponent2>(), entity.Get<TComponent3>(), entity.Get<TComponent4>(), entity.Get<TComponent5>(), entity.Get<TComponent6>(), entity.Get<TComponent7>(), entity.Get<TComponent8>(), entity.Get<TComponent9>(), entity.Get<TComponent10>(), entity.Get<TComponent11>());
        }

        /// <summary>
        /// Gets the twelve specified components.
        /// </summary>
        /// <typeparam name="TComponent1">The type of the first component the method returns.</typeparam>
        /// <typeparam name="TComponent2">The type of the second component the method returns.</typeparam>
        /// <typeparam name="TComponent3">The type of the third component the method returns.</typeparam>
        /// <typeparam name="TComponent4">The type of the fourth component the method returns.</typeparam>
        /// <typeparam name="TComponent5">The type of the fifth component the method returns.</typeparam>
        /// <typeparam name="TComponent6">The type of the sixth component the method returns.</typeparam>
        /// <typeparam name="TComponent7">The type of the seventh component the method returns.</typeparam>
        /// <typeparam name="TComponent8">The type of the eighth component the method returns.</typeparam>
        /// <typeparam name="TComponent9">The type of the ninth component the method returns.</typeparam>
        /// <typeparam name="TComponent10">The type of the tenth component the method returns.</typeparam>
        /// <typeparam name="TComponent11">The type of the eleventh component the method returns.</typeparam>
        /// <typeparam name="TComponent12">The type of the twelfth component the method returns.</typeparam>
        /// <param name="entity">The <see cref="Entity"/> to get the components from.</param>
        /// <returns>The tuple of the twelve components or <c>null</c> if component does not exist.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static (TComponent1, TComponent2, TComponent3, TComponent4, TComponent5, TComponent6, TComponent7, TComponent8, TComponent9, TComponent10, TComponent11, TComponent12) Get<TComponent1, TComponent2, TComponent3, TComponent4, TComponent5, TComponent6, TComponent7, TComponent8, TComponent9, TComponent10, TComponent11, TComponent12>(this Entity entity)
            where TComponent1 : EntityComponent
            where TComponent2 : EntityComponent
            where TComponent3 : EntityComponent
            where TComponent4 : EntityComponent
            where TComponent5 : EntityComponent
            where TComponent6 : EntityComponent
            where TComponent7 : EntityComponent
            where TComponent8 : EntityComponent
            where TComponent9 : EntityComponent
            where TComponent10 : EntityComponent
            where TComponent11 : EntityComponent
            where TComponent12 : EntityComponent
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return (entity.Get<TComponent1>(), entity.Get<TComponent2>(), entity.Get<TComponent3>(), entity.Get<TComponent4>(), entity.Get<TComponent5>(), entity.Get<TComponent6>(), entity.Get<TComponent7>(), entity.Get<TComponent8>(), entity.Get<TComponent9>(), entity.Get<TComponent10>(), entity.Get<TComponent11>(), entity.Get<TComponent12>());
        }

        /// <summary>
        /// Gets the thirteen specified components.
        /// </summary>
        /// <typeparam name="TComponent1">The type of the first component the method returns.</typeparam>
        /// <typeparam name="TComponent2">The type of the second component the method returns.</typeparam>
        /// <typeparam name="TComponent3">The type of the third component the method returns.</typeparam>
        /// <typeparam name="TComponent4">The type of the fourth component the method returns.</typeparam>
        /// <typeparam name="TComponent5">The type of the fifth component the method returns.</typeparam>
        /// <typeparam name="TComponent6">The type of the sixth component the method returns.</typeparam>
        /// <typeparam name="TComponent7">The type of the seventh component the method returns.</typeparam>
        /// <typeparam name="TComponent8">The type of the eighth component the method returns.</typeparam>
        /// <typeparam name="TComponent9">The type of the ninth component the method returns.</typeparam>
        /// <typeparam name="TComponent10">The type of the tenth component the method returns.</typeparam>
        /// <typeparam name="TComponent11">The type of the eleventh component the method returns.</typeparam>
        /// <typeparam name="TComponent12">The type of the twelfth component the method returns.</typeparam>
        /// <typeparam name="TComponent13">The type of the thirteenth component the method returns.</typeparam>
        /// <param name="entity">The <see cref="Entity"/> to get the components from.</param>
        /// <returns>The tuple of the thirteen components or <c>null</c> if component does not exist.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static (TComponent1, TComponent2, TComponent3, TComponent4, TComponent5, TComponent6, TComponent7, TComponent8, TComponent9, TComponent10, TComponent11, TComponent12, TComponent13) Get<TComponent1, TComponent2, TComponent3, TComponent4, TComponent5, TComponent6, TComponent7, TComponent8, TComponent9, TComponent10, TComponent11, TComponent12, TComponent13>(this Entity entity)
            where TComponent1 : EntityComponent
            where TComponent2 : EntityComponent
            where TComponent3 : EntityComponent
            where TComponent4 : EntityComponent
            where TComponent5 : EntityComponent
            where TComponent6 : EntityComponent
            where TComponent7 : EntityComponent
            where TComponent8 : EntityComponent
            where TComponent9 : EntityComponent
            where TComponent10 : EntityComponent
            where TComponent11 : EntityComponent
            where TComponent12 : EntityComponent
            where TComponent13 : EntityComponent
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return (entity.Get<TComponent1>(), entity.Get<TComponent2>(), entity.Get<TComponent3>(), entity.Get<TComponent4>(), entity.Get<TComponent5>(), entity.Get<TComponent6>(), entity.Get<TComponent7>(), entity.Get<TComponent8>(), entity.Get<TComponent9>(), entity.Get<TComponent10>(), entity.Get<TComponent11>(), entity.Get<TComponent12>(), entity.Get<TComponent13>());
        }

        /// <summary>
        /// Gets the fourteen specified components.
        /// </summary>
        /// <typeparam name="TComponent1">The type of the first component the method returns.</typeparam>
        /// <typeparam name="TComponent2">The type of the second component the method returns.</typeparam>
        /// <typeparam name="TComponent3">The type of the third component the method returns.</typeparam>
        /// <typeparam name="TComponent4">The type of the fourth component the method returns.</typeparam>
        /// <typeparam name="TComponent5">The type of the fifth component the method returns.</typeparam>
        /// <typeparam name="TComponent6">The type of the sixth component the method returns.</typeparam>
        /// <typeparam name="TComponent7">The type of the seventh component the method returns.</typeparam>
        /// <typeparam name="TComponent8">The type of the eighth component the method returns.</typeparam>
        /// <typeparam name="TComponent9">The type of the ninth component the method returns.</typeparam>
        /// <typeparam name="TComponent10">The type of the tenth component the method returns.</typeparam>
        /// <typeparam name="TComponent11">The type of the eleventh component the method returns.</typeparam>
        /// <typeparam name="TComponent12">The type of the twelfth component the method returns.</typeparam>
        /// <typeparam name="TComponent13">The type of the thirteenth component the method returns.</typeparam>
        /// <typeparam name="TComponent14">The type of the fourteenth component the method returns.</typeparam>
        /// <param name="entity">The <see cref="Entity"/> to get the components from.</param>
        /// <returns>The tuple of the fourteen components or <c>null</c> if component does not exist.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static (TComponent1, TComponent2, TComponent3, TComponent4, TComponent5, TComponent6, TComponent7, TComponent8, TComponent9, TComponent10, TComponent11, TComponent12, TComponent13, TComponent14) Get<TComponent1, TComponent2, TComponent3, TComponent4, TComponent5, TComponent6, TComponent7, TComponent8, TComponent9, TComponent10, TComponent11, TComponent12, TComponent13, TComponent14>(this Entity entity)
            where TComponent1 : EntityComponent
            where TComponent2 : EntityComponent
            where TComponent3 : EntityComponent
            where TComponent4 : EntityComponent
            where TComponent5 : EntityComponent
            where TComponent6 : EntityComponent
            where TComponent7 : EntityComponent
            where TComponent8 : EntityComponent
            where TComponent9 : EntityComponent
            where TComponent10 : EntityComponent
            where TComponent11 : EntityComponent
            where TComponent12 : EntityComponent
            where TComponent13 : EntityComponent
            where TComponent14 : EntityComponent
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return (entity.Get<TComponent1>(), entity.Get<TComponent2>(), entity.Get<TComponent3>(), entity.Get<TComponent4>(), entity.Get<TComponent5>(), entity.Get<TComponent6>(), entity.Get<TComponent7>(), entity.Get<TComponent8>(), entity.Get<TComponent9>(), entity.Get<TComponent10>(), entity.Get<TComponent11>(), entity.Get<TComponent12>(), entity.Get<TComponent13>(), entity.Get<TComponent14>());
        }

        /// <summary>
        /// Gets the fifteen specified components.
        /// </summary>
        /// <typeparam name="TComponent1">The type of the first component the method returns.</typeparam>
        /// <typeparam name="TComponent2">The type of the second component the method returns.</typeparam>
        /// <typeparam name="TComponent3">The type of the third component the method returns.</typeparam>
        /// <typeparam name="TComponent4">The type of the fourth component the method returns.</typeparam>
        /// <typeparam name="TComponent5">The type of the fifth component the method returns.</typeparam>
        /// <typeparam name="TComponent6">The type of the sixth component the method returns.</typeparam>
        /// <typeparam name="TComponent7">The type of the seventh component the method returns.</typeparam>
        /// <typeparam name="TComponent8">The type of the eighth component the method returns.</typeparam>
        /// <typeparam name="TComponent9">The type of the ninth component the method returns.</typeparam>
        /// <typeparam name="TComponent10">The type of the tenth component the method returns.</typeparam>
        /// <typeparam name="TComponent11">The type of the eleventh component the method returns.</typeparam>
        /// <typeparam name="TComponent12">The type of the twelfth component the method returns.</typeparam>
        /// <typeparam name="TComponent13">The type of the thirteenth component the method returns.</typeparam>
        /// <typeparam name="TComponent14">The type of the fourteenth component the method returns.</typeparam>
        /// <typeparam name="TComponent15">The type of the fifteenth component the method returns.</typeparam>
        /// <param name="entity">The <see cref="Entity"/> to get the components from.</param>
        /// <returns>The tuple of the fifteen components or <c>null</c> if component does not exist.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static (TComponent1, TComponent2, TComponent3, TComponent4, TComponent5, TComponent6, TComponent7, TComponent8, TComponent9, TComponent10, TComponent11, TComponent12, TComponent13, TComponent14, TComponent15) Get<TComponent1, TComponent2, TComponent3, TComponent4, TComponent5, TComponent6, TComponent7, TComponent8, TComponent9, TComponent10, TComponent11, TComponent12, TComponent13, TComponent14, TComponent15>(this Entity entity)
            where TComponent1 : EntityComponent
            where TComponent2 : EntityComponent
            where TComponent3 : EntityComponent
            where TComponent4 : EntityComponent
            where TComponent5 : EntityComponent
            where TComponent6 : EntityComponent
            where TComponent7 : EntityComponent
            where TComponent8 : EntityComponent
            where TComponent9 : EntityComponent
            where TComponent10 : EntityComponent
            where TComponent11 : EntityComponent
            where TComponent12 : EntityComponent
            where TComponent13 : EntityComponent
            where TComponent14 : EntityComponent
            where TComponent15 : EntityComponent
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return (entity.Get<TComponent1>(), entity.Get<TComponent2>(), entity.Get<TComponent3>(), entity.Get<TComponent4>(), entity.Get<TComponent5>(), entity.Get<TComponent6>(), entity.Get<TComponent7>(), entity.Get<TComponent8>(), entity.Get<TComponent9>(), entity.Get<TComponent10>(), entity.Get<TComponent11>(), entity.Get<TComponent12>(), entity.Get<TComponent13>(), entity.Get<TComponent14>(), entity.Get<TComponent15>());
        }

        /// <summary>
        /// Gets the sixteen specified components.
        /// </summary>
        /// <typeparam name="TComponent1">The type of the first component the method returns.</typeparam>
        /// <typeparam name="TComponent2">The type of the second component the method returns.</typeparam>
        /// <typeparam name="TComponent3">The type of the third component the method returns.</typeparam>
        /// <typeparam name="TComponent4">The type of the fourth component the method returns.</typeparam>
        /// <typeparam name="TComponent5">The type of the fifth component the method returns.</typeparam>
        /// <typeparam name="TComponent6">The type of the sixth component the method returns.</typeparam>
        /// <typeparam name="TComponent7">The type of the seventh component the method returns.</typeparam>
        /// <typeparam name="TComponent8">The type of the eighth component the method returns.</typeparam>
        /// <typeparam name="TComponent9">The type of the ninth component the method returns.</typeparam>
        /// <typeparam name="TComponent10">The type of the tenth component the method returns.</typeparam>
        /// <typeparam name="TComponent11">The type of the eleventh component the method returns.</typeparam>
        /// <typeparam name="TComponent12">The type of the twelfth component the method returns.</typeparam>
        /// <typeparam name="TComponent13">The type of the thirteenth component the method returns.</typeparam>
        /// <typeparam name="TComponent14">The type of the fourteenth component the method returns.</typeparam>
        /// <typeparam name="TComponent15">The type of the fifteenth component the method returns.</typeparam>
        /// <typeparam name="TComponent16">The type of the sixteenth component the method returns.</typeparam>
        /// <param name="entity">The <see cref="Entity"/> to get the components from.</param>
        /// <returns>The tuple of the sixteen components or <c>null</c> if component does not exist.</returns>
        /// <exception cref="ArgumentNullException">The entity was <c>null</c>.</exception>
        public static (TComponent1, TComponent2, TComponent3, TComponent4, TComponent5, TComponent6, TComponent7, TComponent8, TComponent9, TComponent10, TComponent11, TComponent12, TComponent13, TComponent14, TComponent15, TComponent16) Get<TComponent1, TComponent2, TComponent3, TComponent4, TComponent5, TComponent6, TComponent7, TComponent8, TComponent9, TComponent10, TComponent11, TComponent12, TComponent13, TComponent14, TComponent15, TComponent16>(this Entity entity)
            where TComponent1 : EntityComponent
            where TComponent2 : EntityComponent
            where TComponent3 : EntityComponent
            where TComponent4 : EntityComponent
            where TComponent5 : EntityComponent
            where TComponent6 : EntityComponent
            where TComponent7 : EntityComponent
            where TComponent8 : EntityComponent
            where TComponent9 : EntityComponent
            where TComponent10 : EntityComponent
            where TComponent11 : EntityComponent
            where TComponent12 : EntityComponent
            where TComponent13 : EntityComponent
            where TComponent14 : EntityComponent
            where TComponent15 : EntityComponent
            where TComponent16 : EntityComponent
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return (entity.Get<TComponent1>(), entity.Get<TComponent2>(), entity.Get<TComponent3>(), entity.Get<TComponent4>(), entity.Get<TComponent5>(), entity.Get<TComponent6>(), entity.Get<TComponent7>(), entity.Get<TComponent8>(), entity.Get<TComponent9>(), entity.Get<TComponent10>(), entity.Get<TComponent11>(), entity.Get<TComponent12>(), entity.Get<TComponent13>(), entity.Get<TComponent14>(), entity.Get<TComponent15>(), entity.Get<TComponent16>());
        }

    }
}
