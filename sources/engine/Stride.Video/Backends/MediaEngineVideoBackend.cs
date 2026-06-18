// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11
#pragma warning disable CA1416 // Validate platform compatibility (handled at higher level — Windows only)

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

namespace Stride.Video.Backends;

internal sealed unsafe class MediaEngineVideoBackend : VideoBackend
{
    private readonly MediaEngineVideoBackendFactory factory;
    private IMFMediaEngine* mediaEngine;
    private Texture videoOutputTexture;
    private IDXGISurface* videoOutputSurface;
    private Stream videoFileStream;
    private int videoWidth;
    private int videoHeight;
    private bool reachedEOF;
    // OnVideoStreamTick can return S_OK after LOADEDMETADATA but before the first frame is
    // actually decoded — TransferVideoFrame then produces a blank surface. Gate frame-presented
    // reporting on LOADEDDATA (engine has data at the current position) so the load-handshake
    // tick doesn't count as a real frame.
    private volatile bool firstFrameDecoded;

    // A paused MediaFoundation seek can leave its frame decoded-but-unpresented under load
    // (OnVideoStreamTick keeps returning S_FALSE even though the engine reaches HAVE_ENOUGH_DATA).
    // FrameStep forces that ready frame to present at the exact seek position; these track when to
    // apply it: only while paused, after the present had time to arrive on its own.
    private IMFMediaEngineEx* mediaEngineEx;
    private bool awaitingSeekFrame;
    private TimeSpan elapsedSinceSeek;
    private static readonly TimeSpan SeekFramePresentTimeout = TimeSpan.FromMilliseconds(500);

    public MediaEngineVideoBackend(VideoInstance instance, MediaEngineVideoBackendFactory factory) : base(instance)
    {
        this.factory = factory;
    }

    public override bool Initialize(string url, long startPosition, long length)
    {
        if (mediaEngine != null)
            throw new InvalidOperationException();

        try
        {
            CoCreateInstance(in CLSID_MFMediaEngineClassFactory, null, CLSCTX.CLSCTX_INPROC_SERVER, out IMFMediaEngineClassFactory* classFactory).ThrowOnFailure();
            using var _1 = ComHelpers.AsPtr(classFactory);

            IMFAttributes* attr;
            MFCreateAttributes(&attr, 1).ThrowOnFailure();
            using var _2 = ComHelpers.AsPtr(attr);
            attr->SetUINT32(in MF_MEDIA_ENGINE_VIDEO_OUTPUT_FORMAT, (uint)Format.FormatB8G8R8A8Unorm);
            attr->SetUnknown(in MF_MEDIA_ENGINE_DXGI_MANAGER, (IUnknown*)factory.DxgiDeviceManager);

            using var notify = ComHelpers.CreateCCW<IMFMediaEngineNotify>(new Notify(this));
            attr->SetUnknown(in MF_MEDIA_ENGINE_CALLBACK, notify);

            IMFMediaEngine* engine;
            classFactory->CreateInstance(default, attr, &engine);
            mediaEngine = engine;

            mediaEngine->QueryInterface<IMFMediaEngineEx>(out var ext).ThrowOnFailure();
            mediaEngineEx = ext;   // kept alive for FrameStep; released in ReleaseMedia

            videoFileStream = new VirtualFileStream(File.OpenRead(url), startPosition, startPosition + length);
            using var videoDataStream = ComHelpers.CreateCCW<IStream>(new ComStreamWrapper(videoFileStream));
            IMFByteStream* byteStream;
            MFCreateMFByteStreamOnStream(videoDataStream, &byteStream);
            using var _4 = ComHelpers.AsPtr(byteStream);

            var uri = new Uri(url, UriKind.RelativeOrAbsolute);
            using var uri_bstr = ComHelpers.AsBSTR(uri.AbsoluteUri);

            mediaEngineEx->SetSourceFromByteStream(byteStream, uri_bstr);
            return true;
        }
        catch
        {
            ReleaseMedia();
            return false;
        }
    }

    public override void ReleaseMedia()
    {
        if (mediaEngineEx != null)
            mediaEngineEx->Release();
        mediaEngineEx = null;

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

    public override void Play()
    {
        if (Instance.PlayRange.Start > Instance.CurrentTime)
            Instance.Seek(Instance.PlayRange.Start);
        mediaEngine->Play();
    }

    public override void Pause() => mediaEngine->Pause();

    public override void Stop()
    {
        mediaEngine->Pause();
        Instance.Seek(Instance.PlayRange.Start);
    }

    public override void Seek(TimeSpan time)
    {
        mediaEngine->SetCurrentTime(time.TotalSeconds);
        reachedEOF = false;
        awaitingSeekFrame = true;
        elapsedSinceSeek = TimeSpan.Zero;
    }

    public override void SetPlaybackSpeed(float speed) => mediaEngine->SetPlaybackRate(speed);

    public override void SetAudioVolume(float volume) => mediaEngine->SetVolume(volume);

    public override void UpdatePlayRange()
    {
        if (Instance.PlayRange.Start > Instance.CurrentTime)
            Instance.Seek(Instance.PlayRange.Start);
    }

    public override void Update(TimeSpan elapsed)
    {
        if (videoOutputSurface == null)
            return;

        // Stopped/Paused don't gate decoder delivery: a Seek that triggered MediaFoundation
        // to produce a frame should still land in the target this tick. The native engine
        // owns the play clock; we just upload whatever's ready.

        // S_OK (0) means a new frame is ready; S_FALSE (1) means none this tick (nothing to present).
        if (mediaEngine->OnVideoStreamTick(out var presentationTimeTicks).Value != 0)
        {
            // The seek's frame never made it to the present path while paused under load. The frame
            // is decoded and ready, so FrameStep forces it to present at the exact seek position.
            // Gated to the paused state since FrameStep pauses the engine.
            if (awaitingSeekFrame && firstFrameDecoded && mediaEngine->IsPaused())
            {
                elapsedSinceSeek += elapsed;
                if (elapsedSinceSeek >= SeekFramePresentTimeout)
                {
                    awaitingSeekFrame = false;
                    mediaEngineEx->FrameStep(true);
                }
            }
            return;
        }

        if (!firstFrameDecoded)
            return;

        awaitingSeekFrame = false;

        Instance.SetCurrentTime(TimeSpan.FromTicks(presentationTimeTicks));

        var endOfMedia = reachedEOF;
        if (!endOfMedia)
        {
            if (Instance.PlayRange.IsValid() && Instance.CurrentTime > Instance.PlayRange.End)
                endOfMedia = true;
            else if (Instance.IsLooping && Instance.LoopRange.IsValid() && Instance.CurrentTime > Instance.LoopRange.End)
                endOfMedia = true;
        }

        if (endOfMedia)
        {
            if (Instance.IsLooping)
            {
                Instance.Seek(Instance.LoopRange.Start);
            }
            else
            {
                Instance.Stop();
                return;
            }
        }

        var target = Instance.VideoComponent.Target;
        if (target != null && videoOutputSurface != null && videoOutputTexture != null)
        {
            Instance.VideoTexture.SetTargetContentToVideoStream(target);

            var graphicsContext = Instance.Services.GetSafeServiceAs<GraphicsContext>();

            mediaEngine->TransferVideoFrame((IUnknown*)videoOutputSurface, pSrc: null, new RECT(0, 0, videoWidth, videoHeight), pBorderClr: null);
            Instance.VideoTexture.CopyDecoderOutputToTopLevelMipmap(graphicsContext, videoOutputTexture);
            Instance.VideoTexture.GenerateMipMaps(graphicsContext);

            Instance.NotifyFramePresented();
        }
    }

    private void CompleteMediaInitialization()
    {
        mediaEngine->GetNativeVideoSize(out var width, out var height);
        videoWidth = (int)width;
        videoHeight = (int)height;

        Instance.SetDuration(TimeSpan.FromSeconds(mediaEngine->GetDuration()));

        videoOutputTexture = Texture.New2D(Instance.GraphicsDevice, videoWidth, videoHeight, 1, PixelFormat.B8G8R8A8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget);

        HResult hr = videoOutputTexture.NativeResource.QueryInterface(out ComPtr<IDXGISurface> outputSurface);
        if (hr.IsFailure)
            hr.Throw();
        videoOutputSurface = outputSurface;

        Instance.AllocateVideoTexture(videoWidth, videoHeight);

        if (Instance.VideoComponent.PlayAudio != true || Instance.VideoComponent.AudioEmitters.Any(e => e != null))
            mediaEngine->SetMuted(true);
    }

    private void OnPlaybackCallback(MF_MEDIA_ENGINE_EVENT playEvent, nuint param1, uint param2)
    {
        switch (playEvent)
        {
            case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_LOADEDMETADATA:
                CompleteMediaInitialization();
                break;
            case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_ERROR:
                VideoInstance.Logger.Error($"Failed to load the video source. The file codec or format is likely not to be supported. MediaEngine error code=[{(MF_MEDIA_ENGINE_ERR)param1}], Windows error code=[{param2}]");
                ReleaseMedia();
                break;
            case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_LOADEDDATA:
                firstFrameDecoded = true;
                break;
            case MF_MEDIA_ENGINE_EVENT.MF_MEDIA_ENGINE_EVENT_ENDED:
                reachedEOF = true;
                break;
        }
    }

    private sealed class Notify : IMFMediaEngineNotify.Interface
    {
        private readonly MediaEngineVideoBackend backend;
        public Notify(MediaEngineVideoBackend backend) => this.backend = backend;
        public HRESULT EventNotify(uint @event, nuint param1, uint param2)
        {
            backend.OnPlaybackCallback((MF_MEDIA_ENGINE_EVENT)@event, param1, param2);
            return default;
        }
    }

    private sealed class ComStreamWrapper : IStream.Interface
    {
        private readonly Stream stream;
        public ComStreamWrapper(Stream stream) => this.stream = stream;

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

        public HRESULT Write(void* pv, uint cb, [Optional] uint* pcbWritten) => throw new NotImplementedException();
        public HRESULT SetSize(ulong libNewSize) => throw new NotImplementedException();
        public HRESULT CopyTo(IStream* pstm, ulong cb, [Optional] ulong* pcbRead, [Optional] ulong* pcbWritten) => throw new NotImplementedException();
        public HRESULT Commit(uint grfCommitFlags) => throw new NotImplementedException();
        public HRESULT Revert() => throw new NotImplementedException();
        public HRESULT LockRegion(ulong libOffset, ulong cb, uint dwLockType) => throw new NotImplementedException();
        public HRESULT UnlockRegion(ulong libOffset, ulong cb, uint dwLockType) => throw new NotImplementedException();
        public HRESULT Clone(IStream** ppstm) => throw new NotImplementedException();
    }
}

#endif
