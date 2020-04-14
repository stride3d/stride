// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#include "Common.h"
#include "../../../deps/NativePath/NativeMemory.h"

#if defined(ANDROID) || !defined(__clang__)

#include "../../../deps/NativePath/NativePath.h"
#include "../../../deps/NativePath/NativeDynamicLinking.h"
#include "../../../deps/NativePath/NativeThreading.h"
#include "../../../deps/NativePath/NativeMath.h"
#include "../../../deps/NativePath/TINYSTL/vector.h"
#include "../../../deps/NativePath/TINYSTL/unordered_set.h"
#include "../../../../deps/OpenSLES/OpenSLES.h"
#include "../../../../deps/OpenSLES/OpenSLES_Android.h"
#include "../../Stride.Native/StrideNative.h"

extern "C" {
	class SpinLock
	{
	public:
		SpinLock()
		{
			mLocked = false;
		}

		void Lock()
		{			
			while(!__sync_bool_compare_and_swap(&mLocked, false, true)) {}
		}

		void Unlock()
		{
			mLocked = false;
		}

	private:
		volatile bool mLocked;
	};

	namespace OpenSLES
	{
		typedef SLresult SLAPIENTRY (*slCreateEnginePtr)(SLObjectItf* pEngine, SLuint32 numOptions, const SLEngineOption* pEngineOptions, SLuint32 numInterfaces, const SLInterfaceID* pInterfaceIds, const SLboolean* pInterfaceRequired);

		void* OpenSLESLibrary = NULL;
		slCreateEnginePtr slCreateEngineFunc = NULL;
		SLInterfaceID* SL_IID_ENGINE_PTR = NULL;
		SLInterfaceID* SL_IID_BUFFERQUEUE_PTR = NULL;
		SLInterfaceID* SL_IID_VOLUME_PTR = NULL;
		SLInterfaceID* SL_IID_PLAY_PTR = NULL;
		SLInterfaceID* SL_IID_PLAYBACKRATE_PTR = NULL;

		npBool xnAudioInit()
		{
			if (OpenSLESLibrary) return true;

			OpenSLESLibrary = LoadDynamicLibrary("libOpenSLES");
			if (!OpenSLESLibrary) return false;

			SL_IID_ENGINE_PTR = (SLInterfaceID*)GetSymbolAddress(OpenSLESLibrary, "SL_IID_ENGINE");
			if (!SL_IID_ENGINE_PTR) return false;

			SL_IID_BUFFERQUEUE_PTR = (SLInterfaceID*)GetSymbolAddress(OpenSLESLibrary, "SL_IID_BUFFERQUEUE");
			if (!SL_IID_BUFFERQUEUE_PTR) return false;

			SL_IID_VOLUME_PTR = (SLInterfaceID*)GetSymbolAddress(OpenSLESLibrary, "SL_IID_VOLUME");
			if (!SL_IID_VOLUME_PTR) return false;

			SL_IID_PLAY_PTR = (SLInterfaceID*)GetSymbolAddress(OpenSLESLibrary, "SL_IID_PLAY");
			if (!SL_IID_PLAY_PTR) return false;

			SL_IID_PLAYBACKRATE_PTR = (SLInterfaceID*)GetSymbolAddress(OpenSLESLibrary, "SL_IID_PLAYBACKRATE");
			if (!SL_IID_PLAYBACKRATE_PTR) return false;

			slCreateEngineFunc = (slCreateEnginePtr)GetSymbolAddress(OpenSLESLibrary, "slCreateEngine");
			if (!slCreateEngineFunc) return false;

			return true;
		}

		struct xnAudioSource;

		struct xnAudioDevice
		{
			SLObjectItf device; 
			SLEngineItf engine;
			SLObjectItf outputMix;
			SpinLock deviceLock;
			tinystl::unordered_set<xnAudioSource*> sources;
			volatile float masterVolume = 1.0f;
		};

		struct xnAudioBuffer
		{
			int dataLength;
			char* dataPtr;
			BufferType type;
		};

		struct xnAudioListener
		{
			float4 pos;
			float4 forward;
			float4 up;
			float4 velocity;
			xnAudioDevice* audioDevice;
		};

		struct xnAudioSource
		{
			int sampleRate;
			bool mono;
			bool streamed;
			bool looped;
			volatile bool endOfStream;
			bool canRateChange;
			SLpermille minRate;
			SLpermille maxRate;
			volatile float gain = 1.0f;
			float localizationGain = 1.0f;
			volatile float pitch = 1.0f;
			volatile float doppler_pitch = 1.0f;
			volatile double streamPositionDiff = 0.0f;

			volatile char* subDataPtr;
			volatile int subLength;

			xnAudioListener* listener;
			xnAudioDevice* audioDevice;

			SLObjectItf object;
			SLPlayItf player;
			SLAndroidSimpleBufferQueueItf queue;
			SLVolumeItf volume;
			SLPlaybackRateItf playRate;

			tinystl::vector<xnAudioBuffer*> streamBuffers;
			tinystl::vector<xnAudioBuffer*> freeBuffers;
			SpinLock buffersLock;
		};

#define DEBUG_BREAK debugtrap()

		xnAudioDevice* xnAudioCreate(const char* deviceName, int flags)
		{
			auto res = new xnAudioDevice;
			
			SLEngineOption options[] = { { SL_ENGINEOPTION_THREADSAFE, SL_BOOLEAN_TRUE } };

			SLresult result;
			result = slCreateEngineFunc(&res->device, 1, options, 0, NULL, NULL);
			if(SL_RESULT_SUCCESS != result)
			{
				DEBUG_BREAK;
				delete res;
				return NULL;
			}

			result = (*res->device)->Realize(res->device, SL_BOOLEAN_FALSE);
			if (SL_RESULT_SUCCESS != result)
			{
				DEBUG_BREAK;
				delete res;
				return NULL;
			}

			result = (*res->device)->GetInterface(res->device, *SL_IID_ENGINE_PTR, &res->engine);
			if (SL_RESULT_SUCCESS != result)
			{
				DEBUG_BREAK;
				delete res;
				return NULL;
			}

			result = (*res->engine)->CreateOutputMix(res->engine, &res->outputMix, 0, NULL, NULL);
			if (SL_RESULT_SUCCESS != result)
			{
				DEBUG_BREAK;
				delete res;
				return NULL;
			}

			result = (*res->outputMix)->Realize(res->outputMix, SL_BOOLEAN_FALSE);
			if (SL_RESULT_SUCCESS != result)
			{
				DEBUG_BREAK;
				delete res;
				return NULL;
			}

			return res;
		}

		void xnAudioDestroy(xnAudioDevice* device)
		{
			(*device->outputMix)->Destroy(device->outputMix);
			(*device->device)->Destroy(device->device);
			delete device;
		}

		void xnAudioUpdate(xnAudioDevice* device)
		{
		}

		SLmillibel CalculateVolumeLevel(float sourceGain, float localizationGain, float masterVolumeGain)
		{
			auto gain = sourceGain * localizationGain * masterVolumeGain;
			float dbVolume = 20 * log10(gain) * 100;

			return dbVolume > SL_MILLIBEL_MIN ? SLmillibel(dbVolume) : SL_MILLIBEL_MIN;
		}

		void xnAudioSetMasterVolume(xnAudioDevice* device, float volume)
		{
			device->masterVolume = volume;

			device->deviceLock.Lock();
			
			for (xnAudioSource* source : device->sources)
				(*source->volume)->SetVolumeLevel(source->volume, CalculateVolumeLevel(source->gain, source->localizationGain, volume));
			
			device->deviceLock.Unlock();
		}

		xnAudioListener* xnAudioListenerCreate(xnAudioDevice* device)
		{
			auto res = static_cast<xnAudioListener*>(malloc(sizeof(xnAudioListener) + 15));
			// attempt to fix alignement
			auto bres = (uintptr_t(res) + 15) & ~uintptr_t(0x0F);
			res = reinterpret_cast<xnAudioListener*>(bres);
			memset(res, 0x0, sizeof(xnAudioListener));
			res->audioDevice = device;
			return res;
		}

		void xnAudioListenerDestroy(xnAudioListener* listener)
		{
			delete listener;
		}

		npBool xnAudioListenerEnable(xnAudioListener* listener)
		{
			(void)listener;
			return true;
		}

		void xnAudioListenerDisable(xnAudioListener* listener)
		{
			(void)listener;
		}

		void QueueCallback(SLAndroidSimpleBufferQueueItf bq, void *context) 
		{
			(void)bq;
			auto source = static_cast<xnAudioSource*>(context);
			if(!source->streamed) //looped
			{
				if (source->looped)
				{
					SLmillisecond ms;
					(*source->player)->GetPosition(source->player, &ms);
					auto time = (double)ms / 1000.0;
					time *= source->pitch * source->doppler_pitch;
					source->streamPositionDiff = time;
					(*source->queue)->Enqueue(source->queue, (void*)source->subDataPtr, source->subLength);
				}
				else
				{
					(*source->player)->SetPlayState(source->player, SL_PLAYSTATE_STOPPED);

					//re-enqueue ready to play again
					source->streamPositionDiff = 0.0;
					(*source->queue)->Clear(source->queue);
					(*source->queue)->Enqueue(source->queue, (void*)source->subDataPtr, source->subLength);
				}
			}
			else
			{
				source->buffersLock.Lock();

				//release the next buffer
				if (!source->streamBuffers.empty())
				{
					auto playedBuffer = source->streamBuffers.front();
					source->streamBuffers.erase(source->streamBuffers.begin());

					if(playedBuffer->type == EndOfStream)
					{
						if (!source->looped)
						{
							(*source->player)->SetPlayState(source->player, SL_PLAYSTATE_STOPPED);

							//flush buffers
							for (auto buffer : source->streamBuffers)
							{
								source->freeBuffers.push_back(buffer);
							}
							source->streamBuffers.clear();
						}
					}
					else if(playedBuffer->type == EndOfLoop)
					{
						SLmillisecond ms;
						(*source->player)->GetPosition(source->player, &ms);
						auto time = (double)ms / 1000.0;
						time *= source->pitch * source->doppler_pitch;
						source->streamPositionDiff = time;
					}

					source->freeBuffers.push_back(playedBuffer);
				}
				
				source->buffersLock.Unlock();				
			}
		}

		void PlayerCallback(SLPlayItf caller, void *pContext, SLuint32 event)
		{
			(void)caller;
			(void)pContext;
			(void)event;
			//auto source = static_cast<xnAudioSource*>(pContext);
		}

		xnAudioSource* xnAudioSourceCreate(xnAudioListener* listener, int sampleRate, int maxNBuffers, npBool mono, npBool spatialized, npBool streamed, npBool hrtf, float directionFactor, int environment)
		{
			(void)spatialized;

			auto res = new xnAudioSource;
			res->listener = listener;
			res->audioDevice = listener->audioDevice;
			res->sampleRate = sampleRate;
			res->mono = mono;
			res->streamed = streamed;
			res->looped = false;

			SLDataFormat_PCM format;
			format.bitsPerSample = SL_PCMSAMPLEFORMAT_FIXED_16;
			format.samplesPerSec = 1000 * SLuint32(sampleRate); //milliHz
			format.numChannels = mono ? 1 : 2;
			format.containerSize = 16;
			format.formatType = SL_DATAFORMAT_PCM;
			format.endianness = SL_BYTEORDER_LITTLEENDIAN;
			format.channelMask = mono ? SL_SPEAKER_FRONT_CENTER : SL_SPEAKER_FRONT_LEFT | SL_SPEAKER_FRONT_RIGHT;

			SLDataLocator_AndroidSimpleBufferQueue bufferQueue = { SL_DATALOCATOR_ANDROIDSIMPLEBUFFERQUEUE, (SLuint32) maxNBuffers };

			SLDataSource audioSrc = { &bufferQueue, &format };
			SLDataLocator_OutputMix outMix = { SL_DATALOCATOR_OUTPUTMIX, listener->audioDevice->outputMix };
			SLDataSink sink = { &outMix, NULL };
			const SLInterfaceID ids[3] = { *SL_IID_BUFFERQUEUE_PTR, *SL_IID_VOLUME_PTR, *SL_IID_PLAYBACKRATE_PTR };
			const SLboolean req[3] = { SL_BOOLEAN_TRUE, SL_BOOLEAN_TRUE, SL_BOOLEAN_TRUE };
			auto result = (*listener->audioDevice->engine)->CreateAudioPlayer(listener->audioDevice->engine, &res->object, &audioSrc, &sink, 3, ids, req);
			if (result != SL_RESULT_SUCCESS)
			{
				DEBUG_BREAK;
				delete res;
				return NULL;
			}

			res->canRateChange = true;
			result = (*res->object)->Realize(res->object, SL_BOOLEAN_FALSE);
			if (result != SL_RESULT_SUCCESS)
			{
				res->canRateChange = false;
				result = (*listener->audioDevice->engine)->CreateAudioPlayer(listener->audioDevice->engine, &res->object, &audioSrc, &sink, 2, ids, req);
				if (result != SL_RESULT_SUCCESS)
				{
					DEBUG_BREAK;
					delete res;
					return NULL;
				}
				result = (*res->object)->Realize(res->object, SL_BOOLEAN_FALSE);
				if (result != SL_RESULT_SUCCESS)
				{
					DEBUG_BREAK;
					delete res;
					return NULL;
				}
			}

			result = (*res->object)->GetInterface(res->object, *SL_IID_PLAY_PTR, &res->player);
			if (result != SL_RESULT_SUCCESS)
			{
				DEBUG_BREAK;
				delete res;
				return NULL;
			}

			result = (*res->object)->GetInterface(res->object, *SL_IID_BUFFERQUEUE_PTR, &res->queue);
			if (result != SL_RESULT_SUCCESS)
			{
				DEBUG_BREAK;
				delete res;
				return NULL;
			}

			result = (*res->object)->GetInterface(res->object, *SL_IID_VOLUME_PTR, &res->volume);
			if (result != SL_RESULT_SUCCESS)
			{
				DEBUG_BREAK;
				delete res;
				return NULL;
			}

			if (res->canRateChange)
			{
				//For some reason this was not working in Android N...
				result = (*res->object)->GetInterface(res->object, *SL_IID_PLAYBACKRATE_PTR, &res->playRate);
				if (result != SL_RESULT_SUCCESS)
				{
					DEBUG_BREAK;
					delete res;
					return NULL;
				}
			}

			result = (*res->volume)->EnableStereoPosition(res->volume, SL_BOOLEAN_TRUE);
			if (result != SL_RESULT_SUCCESS)
			{
				DEBUG_BREAK;
				delete res;
				return NULL;
			}

			result = (*res->queue)->RegisterCallback(res->queue, QueueCallback, res);
			if (result != SL_RESULT_SUCCESS)
			{
				DEBUG_BREAK;
				delete res;
				return NULL;
			}

			result = (*res->player)->RegisterCallback(res->player, PlayerCallback, res);
			if (result != SL_RESULT_SUCCESS)
			{
				DEBUG_BREAK;
				delete res;
				return NULL;
			}

			result = (*res->player)->SetCallbackEventsMask(res->player, SL_PLAYEVENT_HEADMOVING);
			if (result != SL_RESULT_SUCCESS)
			{
				DEBUG_BREAK;
				delete res;
				return NULL;
			}

			listener->audioDevice->deviceLock.Lock();

			listener->audioDevice->sources.insert(res);
			
			listener->audioDevice->deviceLock.Unlock();

			return res;
		}

		void xnAudioSourceDestroy(xnAudioSource* source)
		{
			source->audioDevice->deviceLock.Lock();

			source->audioDevice->sources.erase(source);

			source->audioDevice->deviceLock.Unlock();
		
			(*source->object)->Destroy(source->object);
			
			delete source;
		}

		void xnAudioSourceSetPan(xnAudioSource* source, float pan)
		{
			(*source->volume)->SetStereoPosition(source->volume, SLpermille(pan * 1000.0f));
		}

		void xnAudioSourceSetLooping(xnAudioSource* source, npBool looping)
		{
			source->looped = looping;
		}

		void xnAudioSourceSetRange(xnAudioSource* source, double startTime, double stopTime)
		{
			if (source->streamed) return;

			//OpenAL is kinda bad and offers only starting offset...
			//As result we need to rewrite the buffer
			if (startTime == 0 && stopTime == 0)
			{
				//cancel the offsetting
				source->subLength = source->streamBuffers[0]->dataLength;
				source->subDataPtr = source->streamBuffers[0]->dataPtr;

				(*source->queue)->Clear(source->queue);
				(*source->queue)->Enqueue(source->queue, (void*)source->subDataPtr, source->subLength);
			}
			else
			{
				//offset the data
				auto sampleStart = int(double(source->sampleRate) * (source->mono ? 1.0 : 2.0) * startTime);
				auto sampleStop = int(double(source->sampleRate) * (source->mono ? 1.0 : 2.0) * stopTime);

				if (sampleStart > source->streamBuffers[0]->dataLength / sizeof(short))
				{
					return; //the starting position must be less then the total length of the buffer
				}

				if (sampleStop > source->streamBuffers[0]->dataLength / sizeof(short)) //if the end point is more then the length of the buffer fix the value
				{
					sampleStop = source->streamBuffers[0]->dataLength / sizeof(short);
				}

				source->subLength = (sampleStop - sampleStart) * sizeof(short);
				source->subDataPtr = source->streamBuffers[0]->dataPtr + sampleStart * sizeof(short);

				(*source->queue)->Clear(source->queue);
				(*source->queue)->Enqueue(source->queue, (void*)source->subDataPtr, source->subLength);
			}
		}

		void xnAudioSourceSetGain(xnAudioSource* source, float gain)
		{
			source->gain = gain;
			(*source->volume)->SetVolumeLevel(source->volume, CalculateVolumeLevel(gain, source->localizationGain, source->audioDevice->masterVolume));
		}

		void xnAudioSourceSetPitch(xnAudioSource* source, float pitch)
		{
			if (!source->canRateChange) return;

			source->pitch = pitch;

			pitch *= source->doppler_pitch;
			pitch = pitch > 4.0f ? 4.0f : pitch < -4.0f ? -4.0f : pitch;
			(*source->playRate)->SetRate(source->playRate, SLpermille(pitch * 1000.0f));
		}

		void xnAudioSourceSetBuffer(xnAudioSource* source, xnAudioBuffer* buffer)
		{
			if (source->streamed) return;

			source->buffersLock.Lock();

			source->streamBuffers.clear();
			source->streamBuffers.push_back(buffer);
			source->subLength = buffer->dataLength;
			source->subDataPtr = buffer->dataPtr;		

			(*source->queue)->Enqueue(source->queue, buffer->dataPtr, buffer->dataLength);

			source->buffersLock.Unlock();
		}

		void xnAudioSourceQueueBuffer(xnAudioSource* source, xnAudioBuffer* buffer, short* pcm, int bufferSize, BufferType type)
		{
			if (!source->streamed) return;

			buffer->type = type;
			buffer->dataLength = bufferSize;
			memcpy(buffer->dataPtr, pcm, bufferSize);

			source->buffersLock.Lock();

			source->streamBuffers.push_back(buffer);
			(*source->queue)->Enqueue(source->queue, buffer->dataPtr, buffer->dataLength);

			source->buffersLock.Unlock();
		}

		xnAudioBuffer* xnAudioSourceGetFreeBuffer(xnAudioSource* source)
		{
			if (!source->streamed) return NULL;

			xnAudioBuffer* freeBuffer = NULL;

			source->buffersLock.Lock();

			if(!source->freeBuffers.empty())
			{
				freeBuffer = source->freeBuffers.back();
				source->freeBuffers.pop_back();
			}

			source->buffersLock.Unlock();

			return freeBuffer;
		}

		void xnAudioSourcePlay(xnAudioSource* source)
		{
			(*source->player)->SetPlayState(source->player, SL_PLAYSTATE_PLAYING);
		}

		void xnAudioSourcePause(xnAudioSource* source)
		{
			(*source->player)->SetPlayState(source->player, SL_PLAYSTATE_PAUSED);
		}

		void xnAudioSourceFlushBuffers(xnAudioSource* source)
		{
			//flush
			(*source->queue)->Clear(source->queue);

			if (source->streamed)
			{
				source->buffersLock.Lock();

				//flush buffers
				for (auto buffer : source->streamBuffers)
				{
					source->freeBuffers.push_back(buffer);
				}
				source->streamBuffers.clear();

				source->buffersLock.Unlock();
			}
		}

		void xnAudioSourceStop(xnAudioSource* source)
		{
			(*source->player)->SetPlayState(source->player, SL_PLAYSTATE_STOPPED);

			xnAudioSourceFlushBuffers(source);

			if (!source->streamed)
			{
				//re-enqueue ready to play again
				source->streamPositionDiff = 0.0;
				(*source->queue)->Enqueue(source->queue, (void*)source->subDataPtr, source->subLength);
			}
		}

		double xnAudioSourceGetPosition(xnAudioSource* source)
		{
			SLmillisecond ms;
			(*source->player)->GetPosition(source->player, &ms);

			auto time = (double)ms / 1000.0;
			time *= source->pitch * source->doppler_pitch;

			return time - source->streamPositionDiff;
		}

		void xnAudioListenerPush3D(xnAudioListener* listener, float* pos, float* forward, float* up, float* vel, Matrix* worldTransform)
		{
			memcpy(&listener->pos, pos, sizeof(float) * 3);
			memcpy(&listener->forward, forward, sizeof(float) * 3);
			memcpy(&listener->up, up, sizeof(float) * 3);
			memcpy(&listener->velocity, vel, sizeof(float) * 3);

#ifdef __clang__ //resharper does not know about opencl vectors
			listener->pos.w = 0;
			listener->forward.w = 0;
			listener->up.w = 0;
			listener->velocity.w = 0;
#endif
		}

		const float SoundSpeed = 343.0f;
		const float SoundFreq = 600.0f;
		const float SoundPeriod = 1 / SoundFreq;
		const float ZeroTolerance = 1e-6f;
		const float MaxValue = 3.402823E+38f;
#define E_PI 3.1415926535897932384626433832795028841971693993751058209749445923078164062

		void xnAudioSourcePush3D(xnAudioSource* source, float* ppos, float* pforward, float* pup, float* pvel, Matrix* worldTransform)
		{
			float4 pos;
			memcpy(&pos, ppos, sizeof(float) * 3);
			float4 forward;
			memcpy(&forward, pforward, sizeof(float) * 3);
			float4 up;
			memcpy(&up, pup, sizeof(float) * 3);
			float4 vel;
			memcpy(&vel, pvel, sizeof(float) * 3);

#ifdef __clang__ //resharper does not know about opencl vectors

			pos.w = 0;
			forward.w = 0;
			up.w = 0;
			vel.w = 0;

			// To evaluate the Doppler effect we calculate the distance to the listener from one wave to the next one and divide it by the sound speed
			// we use 343m/s for the sound speed which correspond to the sound speed in the air.
			// we use 600Hz for the sound frequency which correspond to the middle of the human hearable sounds frequencies.

			auto dopplerShift = 1.0f;

			auto vecListEmit = pos - source->listener->pos;
			auto distListEmit = npLengthF4(vecListEmit);

			// avoid useless calculations.
			if (!(vel.x == 0 && vel.y == 0 && vel.z == 0 && source->listener->velocity.x == 0 && source->listener->velocity.y == 0 && source->listener->velocity.z == 0))
			{
				auto vecListEmitNorm = vecListEmit;
				if (distListEmit > ZeroTolerance)
				{
					auto inv = 1.0f / distListEmit;
					vecListEmitNorm *= inv;
				}

				auto vecListEmitSpeed = vel - source->listener->velocity;
				auto speedDot = vecListEmitSpeed[0] * vecListEmitNorm[0] + vecListEmitSpeed[1] * vecListEmitNorm[1] + vecListEmitSpeed[2] * vecListEmitNorm[2];
				if (speedDot < -SoundSpeed) // emitter and listener are getting closer more quickly than the speed of the sound.
				{
					dopplerShift = MaxValue; //positive infinity
				}
				else
				{
					auto timeSinceLastWaveArrived = 0.0f; // time elapsed since the previous wave arrived to the listener.
					auto lastWaveDistToListener = 0.0f; // the distance that the last wave still have to travel to arrive to the listener.
					const auto DistLastWave = SoundPeriod * SoundSpeed; // distance traveled by the previous wave.
					if (DistLastWave > distListEmit)
						timeSinceLastWaveArrived = (DistLastWave - distListEmit) / SoundSpeed;
					else
						lastWaveDistToListener = distListEmit - DistLastWave;

					auto nextVecListEmit = vecListEmit + SoundPeriod * vecListEmitSpeed;
					auto nextWaveDistToListener = sqrtf(nextVecListEmit[0] * nextVecListEmit[0] + nextVecListEmit[1] * nextVecListEmit[1] + nextVecListEmit[2] * nextVecListEmit[2]);
					auto timeBetweenTwoWaves = timeSinceLastWaveArrived + (nextWaveDistToListener - lastWaveDistToListener) / SoundSpeed;
					auto apparentFrequency = 1 / timeBetweenTwoWaves;
					dopplerShift = apparentFrequency / SoundFreq;
				}
			}

			source->doppler_pitch = dopplerShift;
			auto pitch = source->pitch * dopplerShift;
			pitch = pitch > 4.0f ? 4.0f : pitch < -4.0f ? -4.0f : pitch;
			(*source->playRate)->SetRate(source->playRate, SLpermille(pitch * 1000.0f));

			// After an analysis of the XAudio2 left/right stereo balance with respect to 3D world position, 
			// it could be found the volume repartition is symmetric to the Up/Down and Front/Back planes.
			// Moreover the left/right repartition seems to follow a third degree polynomial function:
			// Volume_left(a) = 2(c-1)*a^3 - 3(c-1)*a^2 + c*a , where c is a constant close to c = 1.45f and a is the angle normalized bwt [0,1]
			// Volume_right(a) = 1-Volume_left(a)

			// As for signal attenuation wrt distance the model follows a simple inverse square law function as explained in XAudio2 documentation 
			// ( http://msdn.microsoft.com/en-us/library/windows/desktop/microsoft.directx_sdk.x3daudio.x3daudio_emitter(v=vs.85).aspx )
			// Volume(d) = 1                    , if d <= ScaleDistance where d is the distance to the listener
			// Volume(d) = ScaleDistance / d    , if d >= ScaleDistance where d is the distance to the listener

			auto attenuationFactor = distListEmit <= 1.0f ? 1.0f : 1.0f / distListEmit;

			// 2. Left/Right balance.
			auto repartRight = 0.5f;
			float4 rightVec = npCrossProductF4(source->listener->forward, source->listener->up);

			float4 worldToList[4];
			npMatrixIdentityF4(worldToList);
			worldToList[0].x = rightVec.x;
			worldToList[0].y = source->listener->forward.x;
			worldToList[0].z = source->listener->up.x;
			worldToList[1].x = rightVec.y;
			worldToList[1].y = source->listener->forward.y;
			worldToList[1].z = source->listener->up.y;
			worldToList[2].x = rightVec.z;
			worldToList[2].y = source->listener->forward.z;
			worldToList[2].z = source->listener->up.z;

			auto vecListEmitListBase = npTransformNormalF4(vecListEmit, worldToList);
			auto vecListEmitListBaseLen = npLengthF4(vecListEmitListBase);
			if(vecListEmitListBaseLen > 0.0f)
			{
				const auto c = 1.45f;
				auto absAlpha = fabsf(atan2f(vecListEmitListBase.y, vecListEmitListBase.x));
				auto normAlpha = absAlpha / (E_PI / 2.0f);
				if (absAlpha > E_PI / 2.0f) normAlpha = 2.0f - normAlpha;
				repartRight = 0.5f * (2 * (c - 1) * normAlpha * normAlpha * normAlpha - 3 * (c - 1) * normAlpha * normAlpha * normAlpha + c * normAlpha);
				if (absAlpha > E_PI / 2.0f) repartRight = 1 - repartRight;
			}

			xnAudioSourceSetPan(source, repartRight - 0.5);
			source->localizationGain = attenuationFactor;
			xnAudioSourceSetGain(source, source->gain);
#endif
		}

		npBool xnAudioSourceIsPlaying(xnAudioSource* source)
		{
			SLuint32 res;
			(*source->player)->GetPlayState(source->player, &res);
			return res == SL_PLAYSTATE_PLAYING || res == SL_PLAYSTATE_PAUSED;
		}

		xnAudioBuffer* xnAudioBufferCreate(int maxBufferSize)
		{
			auto res = new xnAudioBuffer;
			res->dataPtr = new char[maxBufferSize];
			res->dataLength = maxBufferSize;
			return res;
		}

		void xnAudioBufferDestroy(xnAudioBuffer* buffer)
		{
			delete[] buffer->dataPtr;
			delete buffer;
		}

		void xnAudioBufferFill(xnAudioBuffer* buffer, short* pcm, int bufferSize, int sampleRate, npBool mono)
		{
			(void)sampleRate;
			(void)mono;
			buffer->type = EndOfStream;
			buffer->dataLength = bufferSize;
			memcpy(buffer->dataPtr, pcm, bufferSize);
		}
	}
}

#endif
