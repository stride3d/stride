using System;
#region License
/*
    MIT License
    Copyright(c) 2017-2018 Mattias Edlund
    Copyright(c) 2021 Stefan Boronczyk
*/
#endregion
namespace Stride.Rendering.MeshDecimator.Collections
{
    /// <summary>
    /// A collection of UV channels.
    /// </summary>
    /// <typeparam name="TVec">The UV vector type.</typeparam>
    internal sealed class UVChannels<TVec>
    {
        #region Fields
        private ResizableArray<TVec>[] channels = null;
        private TVec[][] channelsData = null;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the channel collection data.
        /// </summary>
        public TVec[][] Data
        {
            get
            {
                for (int i = 0; i < MeshDecimatorData.UVChannelCount; i++)
                {
                    if (channels[i] != null)
                    {
                        channelsData[i] = channels[i].Data;
                    }
                    else
                    {
                        channelsData[i] = null;
                    }
                }
                return channelsData;
            }
        }

        /// <summary>
        /// Gets or sets a specific channel by index.
        /// </summary>
        /// <param name="index">The channel index.</param>
        public ResizableArray<TVec> this[int index]
        {
            get { return channels[index]; }
            set { channels[index] = value; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new collection of UV channels.
        /// </summary>
        public UVChannels()
        {
            channels = new ResizableArray<TVec>[MeshDecimatorData.UVChannelCount];
            channelsData = new TVec[MeshDecimatorData.UVChannelCount][];
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Resizes all channels at once.
        /// </summary>
        /// <param name="capacity">The new capacity.</param>
        /// <param name="trimExess">If exess memory should be trimmed.</param>
        public void Resize(int capacity, bool trimExess = false)
        {
            for (int i = 0; i < MeshDecimatorData.UVChannelCount; i++)
            {
                if (channels[i] != null)
                {
                    channels[i].Resize(capacity, trimExess);
                }
            }
        }
        #endregion
    }
}
