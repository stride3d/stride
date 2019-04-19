using System;
using Xenko.Engine;
using Xenko.Rendering;
using Xenko.Toolkit.Rendering;

namespace Xenko.Toolkit.Engine
{
    /// <summary>
    /// Extension methods for <see cref="ModelComponent"/>.
    /// </summary>
    public static class ModelComponentExtensions
    {
        /// <summary>
        /// Sets an object of the material pass parameter. Cloning the <see cref="Material"/> if required.
        /// </summary>
        /// <typeparam name="T">The type of value.</typeparam>
        /// <param name="modelComponent">The <see cref="ModelComponent"/> to update material parameter on.</param>
        /// <param name="parameterAccessor">The parameter to update.</param>
        /// <param name="value">The value.</param>
        /// <param name="materialIndex">The index of the material to update. Default is 0.</param>
        /// <param name="passIndex">The index of the pass of the material to update. Default is 0.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="modelComponent"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="materialIndex"/> is less than 0 or greater than <see cref="ModelComponent.GetMaterialCount"/> and not in <see cref="ModelComponent.Materials"/>.
        /// Or if <paramref name="passIndex"/> is less than 0 or greater than or equal to the mu,ber of passes the material has.
        /// </exception>
        public static void SetMaterialParameter<T>(this ModelComponent modelComponent, ObjectParameterAccessor<T> parameterAccessor, T value, int materialIndex = 0, int passIndex = 0)
        {
            modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameterAccessor, value);
        }

        /// <summary>
        /// Sets a blittable value of the material pass parameter. Cloning the <see cref="Material"/> if required.
        /// </summary>
        /// <typeparam name="T">The type of value.</typeparam>
        /// <param name="modelComponent">The <see cref="ModelComponent"/> to update material parameter on.</param>
        /// <param name="parameter">The parameter to update.</param>
        /// <param name="value">The value.</param>
        /// <param name="materialIndex">The index of the material to update. Default is 0.</param>
        /// <param name="passIndex">The index of the pass of the material to update. Default is 0.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="modelComponent"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="materialIndex"/> is less than 0 or greater than <see cref="ModelComponent.GetMaterialCount"/> and not in <see cref="ModelComponent.Materials"/>.
        /// Or if <paramref name="passIndex"/> is less than 0 or greater than or equal to the mu,ber of passes the material has.
        /// </exception>
        public static void SetMaterialParameter<T>(this ModelComponent modelComponent, ValueParameter<T> parameter, T value, int materialIndex = 0, int passIndex = 0) where T : struct
        {
            modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameter, value);
        }

        /// <summary>
        /// Sets blittable values of the material pass parameter. Cloning the <see cref="Material"/> if required.
        /// </summary>
        /// <typeparam name="T">The type of value.</typeparam>
        /// <param name="modelComponent">The <see cref="ModelComponent"/> to update material parameter on.</param>
        /// <param name="parameter">The parameter to update.</param>
        /// <param name="count">Number of values.</param>
        /// <param name="firstValue">The values.</param>
        /// <param name="materialIndex">The index of the material to update. Default is 0.</param>
        /// <param name="passIndex">The index of the pass of the material to update. Default is 0.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="modelComponent"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="materialIndex"/> is less than 0 or greater than <see cref="ModelComponent.GetMaterialCount"/> and not in <see cref="ModelComponent.Materials"/>.
        /// Or if <paramref name="passIndex"/> is less than 0 or greater than or equal to the mu,ber of passes the material has.
        /// </exception>
        public static void SetMaterialParameter<T>(this ModelComponent modelComponent, ValueParameter<T> parameter, int count, ref T firstValue, int materialIndex = 0, int passIndex = 0) where T : struct
        {
            modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameter, count, ref firstValue);
        }

        /// <summary>
        /// Sets blittable value of the material pass parameter. Cloning the <see cref="Material"/> if required.
        /// </summary>
        /// <typeparam name="T">The type of value.</typeparam>
        /// <param name="modelComponent">The <see cref="ModelComponent"/> to update material parameter on.</param>
        /// <param name="parameter">The parameter to update.</param>
        /// <param name="value">The value.</param>
        /// <param name="materialIndex">The index of the material to update. Default is 0.</param>
        /// <param name="passIndex">The index of the pass of the material to update. Default is 0.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="modelComponent"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="materialIndex"/> is less than 0 or greater than <see cref="ModelComponent.GetMaterialCount"/> and not in <see cref="ModelComponent.Materials"/>.
        /// Or if <paramref name="passIndex"/> is less than 0 or greater than or equal to the mu,ber of passes the material has.
        /// </exception>
        public static void SetMaterialParameter<T>(this ModelComponent modelComponent, ValueParameter<T> parameter, ref T value, int materialIndex = 0, int passIndex = 0) where T : struct
        {
            modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameter, ref value);
        }

        /// <summary>
        /// Sets blittable values of the material pass parameter. Cloning the <see cref="Material"/> if required.
        /// </summary>
        /// <typeparam name="T">The type of value.</typeparam>
        /// <param name="modelComponent">The <see cref="ModelComponent"/> to update material parameter on.</param>
        /// <param name="parameter">The parameter to update.</param>
        /// <param name="count">Number of values.</param>
        /// <param name="firstValue">The values.</param>
        /// <param name="materialIndex">The index of the material to update. Default is 0.</param>
        /// <param name="passIndex">The index of the pass of the material to update. Default is 0.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="modelComponent"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="materialIndex"/> is less than 0 or greater than <see cref="ModelComponent.GetMaterialCount"/> and not in <see cref="ModelComponent.Materials"/>.
        /// Or if <paramref name="passIndex"/> is less than 0 or greater than or equal to the mu,ber of passes the material has.
        /// </exception>
        public static void SetMaterialParameter<T>(this ModelComponent modelComponent, ValueParameterKey<T> parameter, int count, ref T firstValue, int materialIndex = 0, int passIndex = 0) where T : struct
        {
            modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameter, count, ref firstValue);
        }

        /// <summary>
        /// Sets blittable values of the material pass parameter. Cloning the <see cref="Material"/> if required.
        /// </summary>
        /// <typeparam name="T">The type of value.</typeparam>
        /// <param name="modelComponent">The <see cref="ModelComponent"/> to update material parameter on.</param>
        /// <param name="parameter">The parameter to update.</param>
        /// <param name="values">The values.</param>
        /// <param name="materialIndex">The index of the material to update. Default is 0.</param>
        /// <param name="passIndex">The index of the pass of the material to update. Default is 0.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="modelComponent"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="materialIndex"/> is less than 0 or greater than <see cref="ModelComponent.GetMaterialCount"/> and not in <see cref="ModelComponent.Materials"/>.
        /// Or if <paramref name="passIndex"/> is less than 0 or greater than or equal to the mu,ber of passes the material has.
        /// </exception>
        public static void SetMaterialParameter<T>(this ModelComponent modelComponent, ValueParameterKey<T> parameter, T[] values, int materialIndex = 0, int passIndex = 0) where T : struct
        {
            modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameter, values);
        }

        /// <summary>
        /// Sets a blittable value of the material pass parameter. Cloning the <see cref="Material"/> if required.
        /// </summary>
        /// <typeparam name="T">The type of value.</typeparam>
        /// <param name="modelComponent">The <see cref="ModelComponent"/> to update material parameter on.</param>
        /// <param name="parameter">The parameter to update.</param>
        /// <param name="value">The value.</param>
        /// <param name="materialIndex">The index of the material to update. Default is 0.</param>
        /// <param name="passIndex">The index of the pass of the material to update. Default is 0.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="modelComponent"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="materialIndex"/> is less than 0 or greater than <see cref="ModelComponent.GetMaterialCount"/> and not in <see cref="ModelComponent.Materials"/>.
        /// Or if <paramref name="passIndex"/> is less than 0 or greater than or equal to the mu,ber of passes the material has.
        /// </exception>
        public static void SetMaterialParameter<T>(this ModelComponent modelComponent, ValueParameterKey<T> parameter, ref T value, int materialIndex = 0, int passIndex = 0) where T : struct
        {
            modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameter, ref value);
        }

        /// <summary>
        /// Sets a blittable of the material pass parameter. Cloning the <see cref="Material"/> if required.
        /// </summary>
        /// <typeparam name="T">The type of value.</typeparam>
        /// <param name="modelComponent">The <see cref="ModelComponent"/> to update material parameter on.</param>
        /// <param name="parameter">The parameter to update.</param>
        /// <param name="value">The value.</param>
        /// <param name="materialIndex">The index of the material to update. Default is 0.</param>
        /// <param name="passIndex">The index of the pass of the material to update. Default is 0.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="modelComponent"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="materialIndex"/> is less than 0 or greater than <see cref="ModelComponent.GetMaterialCount"/> and not in <see cref="ModelComponent.Materials"/>.
        /// Or if <paramref name="passIndex"/> is less than 0 or greater than or equal to the mu,ber of passes the material has.
        /// </exception>
        public static void SetMaterialParameter<T>(this ModelComponent modelComponent, ValueParameterKey<T> parameter, T value, int materialIndex = 0, int passIndex = 0) where T : struct
        {
            modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameter, value);
        }

        /// <summary>
        /// Sets a permutation of the material pass parameter. Cloning the <see cref="Material"/> if required.
        /// </summary>
        /// <typeparam name="T">The type of value.</typeparam>
        /// <param name="modelComponent">The <see cref="ModelComponent"/> to update material parameter on.</param>
        /// <param name="parameter">The parameter to update.</param>
        /// <param name="value">The value.</param>
        /// <param name="materialIndex">The index of the material to update. Default is 0.</param>
        /// <param name="passIndex">The index of the pass of the material to update. Default is 0.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="modelComponent"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="materialIndex"/> is less than 0 or greater than <see cref="ModelComponent.GetMaterialCount"/> and not in <see cref="ModelComponent.Materials"/>.
        /// Or if <paramref name="passIndex"/> is less than 0 or greater than or equal to the mu,ber of passes the material has.
        /// </exception>
        public static void SetMaterialParameter<T>(this ModelComponent modelComponent, PermutationParameter<T> parameter, T value, int materialIndex = 0, int passIndex = 0)
        {
            modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameter, value);
        }

        /// <summary>
        /// Sets an object of the material pass parameter. Cloning the <see cref="Material"/> if required.
        /// </summary>
        /// <typeparam name="T">The type of value.</typeparam>
        /// <param name="modelComponent">The <see cref="ModelComponent"/> to update material parameter on.</param>
        /// <param name="parameter">The parameter to update.</param>
        /// <param name="value">The value.</param>
        /// <param name="materialIndex">The index of the material to update. Default is 0.</param>
        /// <param name="passIndex">The index of the pass of the material to update. Default is 0.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="modelComponent"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="materialIndex"/> is less than 0 or greater than <see cref="ModelComponent.GetMaterialCount"/> and not in <see cref="ModelComponent.Materials"/>.
        /// Or if <paramref name="passIndex"/> is less than 0 or greater than or equal to the mu,ber of passes the material has.
        /// </exception>
        public static void SetMaterialParameter<T>(this ModelComponent modelComponent, ObjectParameterKey<T> parameter, T value, int materialIndex = 0, int passIndex = 0)
        {
            modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameter, value);
        }

        /// <summary>
        /// Sets a permutation of the material pass parameter. Cloning the <see cref="Material"/> if required.
        /// </summary>
        /// <typeparam name="T">The type of value.</typeparam>
        /// <param name="modelComponent">The <see cref="ModelComponent"/> to update material parameter on.</param>
        /// <param name="parameter">The parameter to update.</param>
        /// <param name="value">The value.</param>
        /// <param name="materialIndex">The index of the material to update. Default is 0.</param>
        /// <param name="passIndex">The index of the pass of the material to update. Default is 0.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="modelComponent"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="materialIndex"/> is less than 0 or greater than <see cref="ModelComponent.GetMaterialCount"/> and not in <see cref="ModelComponent.Materials"/>.
        /// Or if <paramref name="passIndex"/> is less than 0 or greater than or equal to the mu,ber of passes the material has.
        /// </exception>
        public static void SetMaterialParameter<T>(this ModelComponent modelComponent, PermutationParameterKey<T> parameter, T value, int materialIndex = 0, int passIndex = 0)
        {
            modelComponent.GetMaterialPassParameters(materialIndex, passIndex).Set(parameter, value);
        }



        /// <summary>
        /// Clones a <see cref="ModelComponent"/>s <see cref="Material"/> if required;
        /// </summary>
        /// <param name="modelComponent"></param>
        /// <param name="materialIndex"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">If <paramref name="modelComponent"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="materialIndex"/> is less than 0 or greater than <see cref="ModelComponent.GetMaterialCount"/> and not in <see cref="ModelComponent.Materials"/>.</exception>
        private static Material GetMaterialCopy(this ModelComponent modelComponent, int materialIndex)
        {
            if (modelComponent == null)
            {
                throw new ArgumentNullException(nameof(modelComponent));
            }

            if (!IsValidMaterialIndex(modelComponent, materialIndex))
            {
                throw new ArgumentOutOfRangeException(nameof(materialIndex));
            }

            var material = modelComponent.GetMaterial(materialIndex);

            if (material is ModelComponentMaterialCopy copy && copy.ModelComponent == modelComponent)
            {
                return material;
            }

            var materialCopy = new ModelComponentMaterialCopy()
            {
                ModelComponent = modelComponent,
            };

            MaterialExtensions.CopyProperties(material, materialCopy);

            modelComponent.Materials[materialIndex] = materialCopy;

            return materialCopy;
        }

        private static bool IsValidMaterialIndex(ModelComponent modelComponent, int materialIndex)
        {
            if (materialIndex < 0) return false;

            int materialCount = modelComponent.GetMaterialCount();

            if (materialCount > 0)
            {
                return materialIndex < materialCount;
            }
            else
            {
                return modelComponent.Materials.ContainsKey(materialIndex);
            }            
        }

        private static ParameterCollection GetMaterialPassParameters(this ModelComponent modelComponent, int materialIndex, int passIndex)
        {
            var material = modelComponent.GetMaterialCopy(materialIndex);

            if (passIndex < 0 || passIndex >= material.Passes.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(passIndex));
            }

            return material.Passes[passIndex].Parameters;
        }

        private class ModelComponentMaterialCopy : Material
        {
            public ModelComponent ModelComponent { get; set; }
        }
    }
}
