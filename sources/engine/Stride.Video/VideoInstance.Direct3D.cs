// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11
#pragma warning disable CA1416 // Validate platform compatibility (no need, we check for Windows already at a higher level)

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.DXGI;
using Silk.NET.Direct3D11;
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Graphics;
using Stride.Media;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Media.MediaFoundation;
using Windows.Win32.System.Com;
using static Windows.Win32.PInvoke;
using IUnknown = Windows.Win32.System.Com.IUnknown;

namespace Stride.Video
{
    unsafe partial class VideoInstance
    {
        private IMFMediaEngine* mediaEngine;

        private Texture videoOutputTexture;
        private IDXGISurface* videoOutputSurface;

        private Stream videoFileStream;

        private int videoWidth;
        private int videoHeight;

        private bool reachedEOF;

        partial void ReleaseMediaImpl()
        {
            if (mediaEngine != null)
            {
                mediaEngine->Shutdown();
                mediaEngine->Release();
            }
            mediaEngine = null;

            videoFileStream?.Dispose();
            videoFileStream = null;

            if (videoOutputSurface != null)
                videoOutputSurface->Release();

            videoOutputSurface = null;

            videoOutputTexture?.Dispose();
            videoOutputTexture = null;
        }

        partial void PlayImpl()
        {
            if (playRange.Start > CurrentTime)
                Seek(playRange.Start);

            mediaEngine->Play();
        }

        partial void PauseImpl()
        {
            mediaEngine->Pause();
        }

        partial void StopImpl()
        {
            mediaEngine->Pause();
            Seek(playRange.Start);
        }

        partial void SeekImpl(TimeSpan time)
        {
            mediaEngine->SetCurrentTime(time.TotalSeconds);
            reachedEOF = false;
        }

        partial void ChangePlaySpeedImpl()
        {
            mediaEngine->SetPlaybackRate(SpeedFactor);
        }

        partial void UpdatePlayRangeImpl()
        {
            if (playRange.Start > CurrentTime)
                Seek(playRange.Start);
        }

        partial void UpdateAudioVolumeImpl(float volume)
        {
            mediaEngine->SetVolume(volume);
        }

        partial void UpdateImpl(ref TimeSpan elapsed)
        {
            if (videoOutputSurface == null || PlayState == PlayState.Stopped)
                return;

            // Transfer frame if a new one is available
            if (mediaEngine->OnVideoStreamTick(out var presentationTimeTicks).Succeeded)
            {
                CurrentTime = TimeSpan.FromTicks(presentationTimeTicks);

                // Check end of media
                var endOfMedia = reachedEOF;
                if (!endOfMedia)
                {
                    // Check the video loop and play range
                    if (PlayRange.IsValid() && CurrentTime > PlayRange.End)
                    {
                        endOfMedia = true;
                    }
                    else if (IsLooping && LoopRange.IsValid() && CurrentTime > LoopRange.End)
                    {
                        endOfMedia = true;
                    }
                }

                if (endOfMedia)
                {
                    if (IsLooping)
                    {
                        // Restart the video at LoopRangeStart
                        Seek(LoopRange.Start);
                    }
                    else
                    {
                        // Stop the video
                        Stop();
                        return;
                    }
                }

                if (videoComponent.Target != null && videoOutputSurface != null && videoOutputTexture != null)
                {
                    videoTexture.SetTargetContentToVideoStream(videoComponent.Target);

                    // Now update the video texture with data of the new video frame
                    var graphicsContext = services.GetSafeServiceAs<GraphicsContext>();

                    mediaEngine->TransferVideoFrame((IUnknown*)videoOutputSurface, pSrc: null, new RECT(0, 0, videoWidth, videoHeight), pBorderClr: null);
                    videoTexture.CopyDecoderOutputToTopLevelMipmap(graphicsContext, videoOutputTexture);

                    videoTexture.GenerateMipMaps(graphicsContext);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        partial void EnsureMedia()
        {
            if (mediaEngine == null)
                throw new InvalidOperationException();
        }

        partial void InitializeMediaImpl(string url, long startPosition, long length, ref bool succeeded)
        {
            succeeded = true;

            if (mediaEngine != null)
                throw new InvalidOperationException();

            try
            {
                CoCreateInstance(in CLSID_MFMediaEngineClassFactory, null, CLSCTX.CLSCTX_INPROC_SERVER, out IMFMediaEngineClassFactory* classFactory).ThrowOnFailure();
                using var _1 = ComHelpers.AsPtr(classFactory);

                //Assign our dxgi manager, and set format to bgra
                IMFAttributes* attr;
                MFCreateAttributes(&attr, 1).ThrowOnFailure();
                using var _2 = ComHelpers.AsPtr(attr);
                attr->SetUINT32(in MF_MEDIA_ENGINE_VIDEO_OUTPUT_FORMAT, (uint)Format.FormatB8G8R8A8Unorm);
                attr->SetUnknown(in MF_MEDIA_ENGINE_DXGI_MANAGER, (IUnknown*)videoSystem.DxgiDeviceManager);

                // Register our PlayBackEvent
                using var notify = ComHelpers.CreateCCW<IMFMediaEngineNotify>(new Notify(this));
                attr->SetUnknown(in MF_MEDIA_ENGINE_CALLBACK, notify);

                IMFMediaEngine* engine;
                classFactory->CreateInstance(default, attr, &engine);
                mediaEngine = engine;

                // set the video source
                mediaEngine->QueryInterface<IMFMediaEngineEx>(out var mediaEngineEx).ThrowOnFailure();
                using var _3 = ComHelpers.AsPtr(mediaEngineEx);

                videoFileStream = new VirtualFileStream(File.OpenRead(url), startPosition, startPosition + length);
                using var videoDataStream = ComHelpers.CreateCCW<IStream>(new ComStreamWrapper(videoFileStream));
                IMFByteStream* byteStream;
                MFCreateMFByteStreamOnStream(videoDataStream, &byteStream);
                using var _4 = ComHelpers.AsPtr(byteStream);

                // Creates an URL to the file
                var uri = new Uri(url, UriKind.RelativeOrAbsolute);
                using var uri_bstr = ComHelpers.AsBSTR(uri.AbsoluteUri);

                // Set the source stream
                mediaEngineEx->SetSourceFromByteStream(byteStream, uri_bstr);
            }
            catch
            {
                succeeded = false;
                ReleaseMedia();
            }
        }

        private unsafe void CompleteMediaInitialization()
        {
            //Get our video size
            mediaEngine->GetNativeVideoSize(out var width, out var height);
            videoWidth = (int)width;
            videoHeight = (int)height;

            Duration = TimeSpan.FromSeconds(mediaEngine->GetDuration());

            //Get DXGI surface to be used by our media engine
            videoOutputTexture = Texture.New2D(GraphicsDevice, videoWidth, videoHeight, 1, PixelFormat.B8G8R8A8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget);

            HResult result = videoOutputTexture.NativeResource.QueryInterface(out ComPtr<IDXGISurface> outputSurface);

            if (result.IsFailure)
                result.Throw();

            videoOutputSurface = outputSurface;

            AllocateVideoTexture(videoWidth, videoHeight);

            if (videoComponent.PlayAudio != true || videoComponent.AudioEmitters.Any(e => e != null))
                mediaEngine->SetMuted(true);
        }

        /// <summary>
        /// Called when [playback callback].
        /// </summary>
        /// <param name="playEvent">The play event.</param>
        /// <param name="param1">The param1.</param>
        /// <param name="param2">The param2.</param>
        private void OnPlaybackCallback(MF_MEDIA_ENGINE_EVENT playEvent, nuint param1, uint param2)
        {
            switch (playEvent)
            {
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_RESOURCELOST:
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_STREAMRENDERINGERROR:
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_SUSPEND:
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_ABORT:
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_EMPTIED:
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_STALLED:
                    break;
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_LOADEDMETADATA:
                    CompleteMediaInitialization();
                    break;
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_ERROR:
                    Logger.Error($"Failed to load the video source. The file codec or format is likely not to be supported. MedieEngine error code=[{(MF_MEDIA_ENGINE_ERR)param1}], Windows error code=[{param2}]");
                    ReleaseMedia();
                    break;
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_FIRSTFRAMEREADY:
                    break;
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_LOADEDDATA:
                    break;
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_CANPLAY:
                    break;
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_SEEKED:
                    break;
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_ENDED:
                    reachedEOF = true;
                    break;
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_LOADSTART:
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_PROGRESS:
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_WAITING:
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_PLAYING:
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_CANPLAYTHROUGH:
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_SEEKING:
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_PLAY:
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_PAUSE:
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_TIMEUPDATE:
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_RATECHANGE:
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_DURATIONCHANGE:
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_VOLUMECHANGE:
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_FORMATCHANGE:
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_PURGEQUEUEDEVENTS:
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_TIMELINE_MARKER:
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_BALANCECHANGE:
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_DOWNLOADCOMPLETE:
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_BUFFERINGSTARTED:
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_BUFFERINGENDED:
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_FRAMESTEPCOMPLETED:
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_NOTIFYSTABLESTATE:
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_TRACKSCHANGE:
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_OPMINFO:
                case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_DELAYLOADEVENT_CHANGED:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(playEvent), playEvent, null);
            }
        }

        sealed class Notify : IMFMediaEngineNotify.Interface
        {
            private readonly VideoInstance instance;

            public Notify(VideoInstance instance)
            {
                this.instance = instance;
            }

            public HRESULT EventNotify(uint @event, nuint param1, uint param2)
            {
                instance.OnPlaybackCallback((MF_MEDIA_ENGINE_EVENT)@event, param1, param2);
                return default;
            }
        }

        sealed class ComStreamWrapper : IStream.Interface
        {
            private readonly Stream stream;

            public ComStreamWrapper(Stream stream)
            {
                this.stream = stream;
            }

            public HRESULT Stat(STATSTG* pstatstg, uint grfStatFlag)
            {
                pstatstg->cbSize = (ulong)stream.Length;
                return default;
            }

            public HRESULT Seek(long dlibMove, SeekOrigin dwOrigin, [Optional] ulong* plibNewPosition)
            {
                var newPosition = stream.Seek(dlibMove, dwOrigin);
                if (plibNewPosition != null)
                    *plibNewPosition = (ulong)newPosition;
                return default;
            }

            public HRESULT Read(void* pv, uint cb, [Optional] uint* pcbRead)
            {
                var buffer = new Span<byte>(pv, (int)cb);
                *pcbRead = (uint)stream.Read(buffer);
                return default;
            }

            public HRESULT Write(void* pv, uint cb, [Optional] uint* pcbWritten)
            {
                throw new NotImplementedException();
            }

            public HRESULT SetSize(ulong libNewSize)
            {
                throw new NotImplementedException();
            }

            public HRESULT CopyTo(IStream* pstm, ulong cb, [Optional] ulong* pcbRead, [Optional] ulong* pcbWritten)
            {
                throw new NotImplementedException();
            }

            public HRESULT Commit(uint grfCommitFlags)
            {
                throw new NotImplementedException();
            }

            public HRESULT Revert()
            {
                throw new NotImplementedException();
            }

            public HRESULT LockRegion(ulong libOffset, ulong cb, uint dwLockType)
            {
                throw new NotImplementedException();
            }

            public HRESULT UnlockRegion(ulong libOffset, ulong cb, uint dwLockType)
            {
                throw new NotImplementedException();
            }

            public HRESULT Clone(IStream** ppstm)
            {
                throw new NotImplementedException();
            }
        }
    }
}

namespace Windows.Win32
{
    static unsafe partial class ComHelpers
    {
        public static MFComPtr<T> CreateCCW<T>(object instance) where T : unmanaged, IVTable, IComIID
        {
            var unknown = (IUnknown*)MFComWrappers<T>.Instance.GetOrCreateComInterfaceForObject(instance, CreateComInterfaceFlags.None);
            unknown->QueryInterface<T>(out var ppv);
            unknown->Release();
            return new MFComPtr<T>(ppv);
        }

        public static unsafe MFComPtr<T> AsPtr<T>(T* ptr) where T : unmanaged, IComIID => new MFComPtr<T>(ptr);

        public static BSTRPtr AsBSTR(string str)
        {
            var bstr = Marshal.StringToBSTR(str);
            return new BSTRPtr(bstr);
        }

        // Called by CsWin32
        static partial void PopulateIUnknownImpl<TComInterface>(IUnknown.Vtbl* vtable)
            where TComInterface : unmanaged
        {
            ComWrappers.GetIUnknownImpl(out IntPtr fpQueryInterface, out IntPtr fpAddRef, out IntPtr fpRelease);
            vtable->QueryInterface_1 = (delegate* unmanaged[Stdcall]<IUnknown*, Guid*, void**, HRESULT>)fpQueryInterface;
            vtable->AddRef_2 = (delegate* unmanaged[Stdcall]<IUnknown*, uint>)fpAddRef;
            vtable->Release_3 = (delegate* unmanaged[Stdcall]<IUnknown*, uint>)fpRelease;
        }

        // https://github.com/microsoft/CsWin32/issues/751#issuecomment-1304268295
        private sealed class MFComWrappers<T> : ComWrappers
            where T : IVTable, IComIID
        {
            public static readonly ComWrappers Instance = new MFComWrappers<T>();

            private static readonly ComInterfaceEntry* s_comInterfaceEntries = CreateComInterfaceEntries();

            private static ComInterfaceEntry* CreateComInterfaceEntries()
            {
                var comInterfaceEntries = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(T), sizeof(ComInterfaceEntry));
                comInterfaceEntries->IID = T.Guid;
                comInterfaceEntries->Vtable = new IntPtr(T.VTable);
                return comInterfaceEntries;
            }

            protected override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
            {
                count = 1;
                return s_comInterfaceEntries;
            }

            protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
            {
                throw new NotImplementedException();
            }

            protected override void ReleaseObjects(IEnumerable objects)
            {
                throw new NotImplementedException();
            }
        }

        // Little helper to ensure Release is called
        public ref struct MFComPtr<T> : IDisposable where T : unmanaged, IComIID
        {
            public readonly T* Ptr;
            public MFComPtr(T* ptr)
            {
                Ptr = ptr;
            }

            public void Dispose() => ((IUnknown*)Ptr)->Release();
            public static implicit operator T*(in MFComPtr<T> comPtr) => comPtr.Ptr;
            public static implicit operator IUnknown*(in MFComPtr<T> comPtr) => (IUnknown*)comPtr.Ptr;
            public static explicit operator MFComPtr<T>(T* ptr) => new MFComPtr<T>(ptr);
        }

        // Little helper to ensure FreeBSTR is called
        public ref struct BSTRPtr : IDisposable
        {
            public readonly nint Ptr;
            public BSTRPtr(nint ptr)
            {
                Ptr = ptr;
            }
            public void Dispose() => Marshal.FreeBSTR(Ptr);
            public static implicit operator BSTR(in BSTRPtr bstrPtr) => new BSTR(bstrPtr.Ptr);
        }
    }
}

#endif
