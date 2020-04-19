// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Annotations;

namespace Stride.Core.Quantum
{
    /// <summary>
    /// An exception that occurs during consistency checks of Quantum objects, indicating that a <see cref="IGraphNode"/> is un an unexpected state.
    /// </summary>
    public class QuantumConsistencyException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the QuantumConsistencyException class.
        /// </summary>
        /// <param name="expected">A string representing the expected result.</param>
        /// <param name="observed">A string representing the observed result.</param>
        /// <param name="node">The node that is related to this error.</param>
        public QuantumConsistencyException(string expected, string observed, IGraphNode node)
            : base(GetMessage(expected, observed))
        {
            Expected = expected ?? "(NullMessage)";
            Observed = observed ?? "(NullMessage)";
            Node = node;
        }

        /// <summary>
        /// Initializes a new instance of the QuantumConsistencyException class, with advanced string formatting.
        /// </summary>
        /// <param name="expected">A string representing the expected result. This string must contains a </param>
        /// <param name="expectedArg"></param>
        /// <param name="observed">A string representing the observed result.</param>
        /// <param name="observedArg"></param>
        /// <param name="node">The node that is related to this error.</param>
        public QuantumConsistencyException(string expected, string expectedArg, string observed, string observedArg, IGraphNode node)
            : base(GetMessage(expected, expectedArg, observed, observedArg))
        {
            try
            {
                Expected = string.Format(expected ?? "(NullMessage) [{0}]", expectedArg ?? "(NullArgument)");
            }
            catch (Exception)
            {
                Expected = expected ?? "(NullMessage) [{0}]";
            }
            try
            {
                Observed = string.Format(observed ?? "(NullMessage) [{0}]", observedArg ?? "(NullArgument)");
            }
            catch (Exception)
            {
                Observed = observed ?? "(NullMessage) [{0}]";
            }

            Node = node;
        }

        /// <summary>
        /// Gets a string representing the expected result.
        /// </summary>
        public string Expected { get; }

        /// <summary>
        /// Gets a string representing the observed result.
        /// </summary>
        public string Observed { get; }

        /// <summary>
        /// Gets the <see cref="IGraphNode"/> that triggered this exception.
        /// </summary>
        public IGraphNode Node { get; }

        ///// <inheritdoc/>
        //public override string ToString()
        //{
        //    return GetMessage(Expected, Observed);
        //}

        [NotNull]
        private static string Format(string message, string argument)
        {
            try
            {
                return string.Format(message ?? "(NullMessage) [{0}]", argument ?? "(NullArgument)");
            }
            catch (Exception)
            {
                return message ?? "(NullMessage) [(NullArgument)]";
            }

        }

        [NotNull]
        private static string Format(string message)
        {
            return message ?? "(NullMessage)";

        }

        [NotNull]
        private static string GetMessage(string expected, string observed)
        {
            return $"Quantum consistency exception. Expected: {Format(expected)} - Observed: {Format(observed)}";
        }

        [NotNull]
        private static string GetMessage(string expected, string expectedArg, string observed, string observedArg)
        {
            return $"Quantum consistency exception. Expected: {Format(expected, expectedArg)} - Observed: {Format(observed, observedArg)}";
        }
    }
}
