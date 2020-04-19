// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Windows;
using Stride.Core.Annotations;
using Stride.Core.Extensions;

namespace Stride.Core.Presentation.Interop
{
    /// <summary>
    /// Wrapper around <see cref="Clipboard"/> that catches <see cref="COMException"/> related to clipboard errors. 
    /// </summary>
    public static class SafeClipboard
    {
        // ReSharper disable InconsistentNaming
        public const int CLIPBRD_E_CANT_OPEN = unchecked((int)0x800401D0);
        public const int CLIPBRD_E_CANT_SET = unchecked((int)0x800401D2);
        // ReSharper restore InconsistentNaming

        /// <summary>
        /// Similar to <see cref="Clipboard.ContainsText()"/> but don't throw if the clipboard cannot be open.
        /// </summary>
        /// <returns><c>true</c> if the Clipboard contains data in the <see cref="DataFormats.UnicodeText"/> data format; otherwise, <c>false</c>.</returns>
        public static bool ContainsText()
        {
            try
            {
                return Clipboard.ContainsText();
            }
            catch (COMException e) when (e.HResult == CLIPBRD_E_CANT_OPEN)
            {
                return false;
            }
        }

        /// <summary>
        /// Similar to <see cref="Clipboard.GetText()"/> but don't throw if the clipboard cannot be open.
        /// </summary>
        /// <returns>A string containing the <see cref="DataFormats.UnicodeText"/> data, or an empty string if no <see cref="DataFormats.UnicodeText"/> data is available on the Clipboard.</returns>
        [NotNull]
        public static string GetText()
        {
            try
            {
                return Clipboard.GetText();
            }
            catch (Exception e) when (e.HResult == CLIPBRD_E_CANT_OPEN)
            {
                e.Ignore();
                return string.Empty;
            }
        }

        /// <summary>
        /// Similar to <see cref="Clipboard.SetDataObject(object, bool)"/> but don't throw if data cannot be set to the clipboard.
        /// </summary>
        /// <exception cref="ArgumentNullException">data is <c>null</c>.</exception>
        public static void SetDataObject([NotNull] object data, bool copy)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            try
            {
                Clipboard.SetDataObject(data, copy);
            }
            catch (Exception e) when (e.HResult == CLIPBRD_E_CANT_OPEN || e.HResult == CLIPBRD_E_CANT_SET)
            {
                e.Ignore();
            }
        }

        /// <summary>
        /// Similar to <see cref="Clipboard.SetText(string)"/> but don't throw if data cannot be set to the clipboard.
        /// </summary>
        /// <exception cref="ArgumentNullException">data is <c>null</c>.</exception>
        public static void SetText([NotNull] string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            try
            {
                Clipboard.SetText(text);
            }
            catch (Exception e) when (e.HResult == CLIPBRD_E_CANT_OPEN || e.HResult == CLIPBRD_E_CANT_SET)
            {
                e.Ignore();
            }
        }
    }
}
