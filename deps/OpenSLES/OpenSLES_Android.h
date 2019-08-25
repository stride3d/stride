/*
 * Copyright (C) 2010 The Android Open Source Project
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#ifndef OPENSL_ES_ANDROID_H_
#define OPENSL_ES_ANDROID_H_

#include "OpenSLES.h"
#include "OpenSLES_AndroidConfiguration.h"
#include "OpenSLES_AndroidMetadata.h"
// Xenko: change from original file
//#include <jni.h>
typedef void* jobject;

#ifdef __cplusplus
extern "C" {
#endif

/*---------------------------------------------------------------------------*/
/* Android common types                                                      */
/*---------------------------------------------------------------------------*/

typedef sl_int64_t             SLAint64;          /* 64 bit signed integer   */

typedef sl_uint64_t            SLAuint64;         /* 64 bit unsigned integer */

/*---------------------------------------------------------------------------*/
/* Android PCM Data Format                                                   */
/*---------------------------------------------------------------------------*/

/* The following pcm representations and data formats map to those in OpenSLES 1.1 */
#define SL_ANDROID_PCM_REPRESENTATION_SIGNED_INT       ((SLuint32) 0x00000001)
#define SL_ANDROID_PCM_REPRESENTATION_UNSIGNED_INT     ((SLuint32) 0x00000002)
#define SL_ANDROID_PCM_REPRESENTATION_FLOAT            ((SLuint32) 0x00000003)

#define SL_ANDROID_DATAFORMAT_PCM_EX    ((SLuint32) 0x00000004)

typedef struct SLAndroidDataFormat_PCM_EX_ {
    SLuint32         formatType;
    SLuint32         numChannels;
    SLuint32         sampleRate;
    SLuint32         bitsPerSample;
    SLuint32         containerSize;
    SLuint32         channelMask;
    SLuint32         endianness;
    SLuint32         representation;
} SLAndroidDataFormat_PCM_EX;

#define SL_ANDROID_SPEAKER_NON_POSITIONAL       ((SLuint32) 0x80000000)

// Make an indexed channel mask from a bitfield.
//
// Each bit in the bitfield corresponds to a channel index,
// from least-significant bit (channel 0) up to the bit
// corresponding to the maximum channel count (currently FCC_8).
// A '1' in the bitfield indicates that the channel should be
// included in the stream, while a '0' indicates that it
// should be excluded. For instance, a bitfield of 0x0A (binary 00001010)
// would define a stream that contains channels 1 and 3. (The corresponding
// indexed mask, after setting the SL_ANDROID_NON_POSITIONAL bit,
// would be 0x8000000A.)
#define SL_ANDROID_MAKE_INDEXED_CHANNEL_MASK(bitfield) \
        ((bitfield) | SL_ANDROID_SPEAKER_NON_POSITIONAL)

// Specifying SL_ANDROID_SPEAKER_USE_DEFAULT as a channel mask in
// SLAndroidDataFormat_PCM_EX causes OpenSL ES to assign a default
// channel mask based on the number of channels requested. This
// value cannot be combined with SL_ANDROID_SPEAKER_NON_POSITIONAL.
#define SL_ANDROID_SPEAKER_USE_DEFAULT ((SLuint32)0)

/*---------------------------------------------------------------------------*/
/* Android Effect interface                                                  */
/*---------------------------------------------------------------------------*/

extern SL_API const SLInterfaceID SL_IID_ANDROIDEFFECT;

/** Android Effect interface methods */

struct SLAndroidEffectItf_;
typedef const struct SLAndroidEffectItf_ * const * SLAndroidEffectItf;

struct SLAndroidEffectItf_ {

    SLresult (*CreateEffect) (SLAndroidEffectItf self,
            SLInterfaceID effectImplementationId);

    SLresult (*ReleaseEffect) (SLAndroidEffectItf self,
            SLInterfaceID effectImplementationId);

    SLresult (*SetEnabled) (SLAndroidEffectItf self,
            SLInterfaceID effectImplementationId,
            SLboolean enabled);

    SLresult (*IsEnabled) (SLAndroidEffectItf self,
            SLInterfaceID effectImplementationId,
            SLboolean *pEnabled);

    SLresult (*SendCommand) (SLAndroidEffectItf self,
            SLInterfaceID effectImplementationId,
            SLuint32 command,
            SLuint32 commandSize,
            void *pCommandData,
            SLuint32 *replySize,
            void *pReplyData);
};


/*---------------------------------------------------------------------------*/
/* Android Effect Send interface                                             */
/*---------------------------------------------------------------------------*/

extern SL_API const SLInterfaceID SL_IID_ANDROIDEFFECTSEND;

/** Android Effect Send interface methods */

struct SLAndroidEffectSendItf_;
typedef const struct SLAndroidEffectSendItf_ * const * SLAndroidEffectSendItf;

struct SLAndroidEffectSendItf_ {
    SLresult (*EnableEffectSend) (
        SLAndroidEffectSendItf self,
        SLInterfaceID effectImplementationId,
        SLboolean enable,
        SLmillibel initialLevel
    );
    SLresult (*IsEnabled) (
        SLAndroidEffectSendItf self,
        SLInterfaceID effectImplementationId,
        SLboolean *pEnable
    );
    SLresult (*SetDirectLevel) (
        SLAndroidEffectSendItf self,
        SLmillibel directLevel
    );
    SLresult (*GetDirectLevel) (
        SLAndroidEffectSendItf self,
        SLmillibel *pDirectLevel
    );
    SLresult (*SetSendLevel) (
        SLAndroidEffectSendItf self,
        SLInterfaceID effectImplementationId,
        SLmillibel sendLevel
    );
    SLresult (*GetSendLevel)(
        SLAndroidEffectSendItf self,
        SLInterfaceID effectImplementationId,
        SLmillibel *pSendLevel
    );
};


/*---------------------------------------------------------------------------*/
/* Android Effect Capabilities interface                                     */
/*---------------------------------------------------------------------------*/

extern SL_API const SLInterfaceID SL_IID_ANDROIDEFFECTCAPABILITIES;

/** Android Effect Capabilities interface methods */

struct SLAndroidEffectCapabilitiesItf_;
typedef const struct SLAndroidEffectCapabilitiesItf_ * const * SLAndroidEffectCapabilitiesItf;

struct SLAndroidEffectCapabilitiesItf_ {

    SLresult (*QueryNumEffects) (SLAndroidEffectCapabilitiesItf self,
            SLuint32 *pNumSupportedEffects);


    SLresult (*QueryEffect) (SLAndroidEffectCapabilitiesItf self,
            SLuint32 index,
            SLInterfaceID *pEffectType,
            SLInterfaceID *pEffectImplementation,
            SLchar *pName,
            SLuint16 *pNameSize);
};


/*---------------------------------------------------------------------------*/
/* Android Configuration interface                                           */
/*---------------------------------------------------------------------------*/
extern SL_API const SLInterfaceID SL_IID_ANDROIDCONFIGURATION;

/** Android Configuration interface methods */

struct SLAndroidConfigurationItf_;
typedef const struct SLAndroidConfigurationItf_ * const * SLAndroidConfigurationItf;

/*
 * Java Proxy Type IDs
 */
#define SL_ANDROID_JAVA_PROXY_ROUTING   0x0001

struct SLAndroidConfigurationItf_ {

    SLresult (*SetConfiguration) (SLAndroidConfigurationItf self,
            const SLchar *configKey,
            const void *pConfigValue,
            SLuint32 valueSize);

    SLresult (*GetConfiguration) (SLAndroidConfigurationItf self,
           const SLchar *configKey,
           SLuint32 *pValueSize,
           void *pConfigValue
       );

    SLresult (*AcquireJavaProxy) (SLAndroidConfigurationItf self,
            SLuint32 proxyType,
            jobject *pProxyObj);

    SLresult (*ReleaseJavaProxy) (SLAndroidConfigurationItf self,
            SLuint32 proxyType);
};


/*---------------------------------------------------------------------------*/
/* Android Simple Buffer Queue Interface                                     */
/*---------------------------------------------------------------------------*/

extern SL_API const SLInterfaceID SL_IID_ANDROIDSIMPLEBUFFERQUEUE;

struct SLAndroidSimpleBufferQueueItf_;
typedef const struct SLAndroidSimpleBufferQueueItf_ * const * SLAndroidSimpleBufferQueueItf;

typedef void (SLAPIENTRY *slAndroidSimpleBufferQueueCallback)(
	SLAndroidSimpleBufferQueueItf caller,
	void *pContext
);

/** Android simple buffer queue state **/

typedef struct SLAndroidSimpleBufferQueueState_ {
	SLuint32	count;
	SLuint32	index;
} SLAndroidSimpleBufferQueueState;


struct SLAndroidSimpleBufferQueueItf_ {
	SLresult (*Enqueue) (
		SLAndroidSimpleBufferQueueItf self,
		const void *pBuffer,
		SLuint32 size
	);
	SLresult (*Clear) (
		SLAndroidSimpleBufferQueueItf self
	);
	SLresult (*GetState) (
		SLAndroidSimpleBufferQueueItf self,
		SLAndroidSimpleBufferQueueState *pState
	);
	SLresult (*RegisterCallback) (
		SLAndroidSimpleBufferQueueItf self,
		slAndroidSimpleBufferQueueCallback callback,
		void* pContext
	);
};


/*---------------------------------------------------------------------------*/
/* Android Buffer Queue Interface                                            */
/*---------------------------------------------------------------------------*/

extern SL_API const SLInterfaceID SL_IID_ANDROIDBUFFERQUEUESOURCE;

struct SLAndroidBufferQueueItf_;
typedef const struct SLAndroidBufferQueueItf_ * const * SLAndroidBufferQueueItf;

#define SL_ANDROID_ITEMKEY_NONE             ((SLuint32) 0x00000000)
#define SL_ANDROID_ITEMKEY_EOS              ((SLuint32) 0x00000001)
#define SL_ANDROID_ITEMKEY_DISCONTINUITY    ((SLuint32) 0x00000002)
#define SL_ANDROID_ITEMKEY_BUFFERQUEUEEVENT ((SLuint32) 0x00000003)
#define SL_ANDROID_ITEMKEY_FORMAT_CHANGE    ((SLuint32) 0x00000004)

#define SL_ANDROIDBUFFERQUEUEEVENT_NONE        ((SLuint32) 0x00000000)
#define SL_ANDROIDBUFFERQUEUEEVENT_PROCESSED   ((SLuint32) 0x00000001)
#if 0   // reserved for future use
#define SL_ANDROIDBUFFERQUEUEEVENT_UNREALIZED  ((SLuint32) 0x00000002)
#define SL_ANDROIDBUFFERQUEUEEVENT_CLEARED     ((SLuint32) 0x00000004)
#define SL_ANDROIDBUFFERQUEUEEVENT_STOPPED     ((SLuint32) 0x00000008)
#define SL_ANDROIDBUFFERQUEUEEVENT_ERROR       ((SLuint32) 0x00000010)
#define SL_ANDROIDBUFFERQUEUEEVENT_CONTENT_END ((SLuint32) 0x00000020)
#endif

typedef struct SLAndroidBufferItem_ {
    SLuint32 itemKey;  // identifies the item
    SLuint32 itemSize;
    SLuint8  itemData[0];
} SLAndroidBufferItem;

typedef SLresult (SLAPIENTRY *slAndroidBufferQueueCallback)(
    SLAndroidBufferQueueItf caller,/* input */
    void *pCallbackContext,        /* input */
    void *pBufferContext,          /* input */
    void *pBufferData,             /* input */
    SLuint32 dataSize,             /* input */
    SLuint32 dataUsed,             /* input */
    const SLAndroidBufferItem *pItems,/* input */
    SLuint32 itemsLength           /* input */
);

typedef struct SLAndroidBufferQueueState_ {
    SLuint32    count;
    SLuint32    index;
} SLAndroidBufferQueueState;

struct SLAndroidBufferQueueItf_ {
    SLresult (*RegisterCallback) (
        SLAndroidBufferQueueItf self,
        slAndroidBufferQueueCallback callback,
        void* pCallbackContext
    );

    SLresult (*Clear) (
        SLAndroidBufferQueueItf self
    );

    SLresult (*Enqueue) (
        SLAndroidBufferQueueItf self,
        void *pBufferContext,
        void *pData,
        SLuint32 dataLength,
        const SLAndroidBufferItem *pItems,
        SLuint32 itemsLength
    );

    SLresult (*GetState) (
        SLAndroidBufferQueueItf self,
        SLAndroidBufferQueueState *pState
    );

    SLresult (*SetCallbackEventsMask) (
            SLAndroidBufferQueueItf self,
            SLuint32 eventFlags
    );

    SLresult (*GetCallbackEventsMask) (
            SLAndroidBufferQueueItf self,
            SLuint32 *pEventFlags
    );
};


/*---------------------------------------------------------------------------*/
/* Android File Descriptor Data Locator                                      */
/*---------------------------------------------------------------------------*/

/** Addendum to Data locator macros  */
#define SL_DATALOCATOR_ANDROIDFD                ((SLuint32) 0x800007BC)

#define SL_DATALOCATOR_ANDROIDFD_USE_FILE_SIZE ((SLAint64) 0xFFFFFFFFFFFFFFFFll)

/** File Descriptor-based data locator definition, locatorType must be SL_DATALOCATOR_ANDROIDFD */
typedef struct SLDataLocator_AndroidFD_ {
    SLuint32        locatorType;
    SLint32         fd;
    SLAint64        offset;
    SLAint64        length;
} SLDataLocator_AndroidFD;


/*---------------------------------------------------------------------------*/
/* Android Android Simple Buffer Queue Data Locator                          */
/*---------------------------------------------------------------------------*/

/** Addendum to Data locator macros  */
#define SL_DATALOCATOR_ANDROIDSIMPLEBUFFERQUEUE ((SLuint32) 0x800007BD)

/** BufferQueue-based data locator definition where locatorType must
 *  be SL_DATALOCATOR_ANDROIDSIMPLEBUFFERQUEUE
 */
typedef struct SLDataLocator_AndroidSimpleBufferQueue {
	SLuint32	locatorType;
	SLuint32	numBuffers;
} SLDataLocator_AndroidSimpleBufferQueue;


/*---------------------------------------------------------------------------*/
/* Android Buffer Queue Data Locator                                         */
/*---------------------------------------------------------------------------*/

/** Addendum to Data locator macros  */
#define SL_DATALOCATOR_ANDROIDBUFFERQUEUE       ((SLuint32) 0x800007BE)

/** Android Buffer Queue-based data locator definition,
 *  locatorType must be SL_DATALOCATOR_ANDROIDBUFFERQUEUE */
typedef struct SLDataLocator_AndroidBufferQueue_ {
    SLuint32    locatorType;
    SLuint32    numBuffers;
} SLDataLocator_AndroidBufferQueue;

/**
 * MIME types required for data in Android Buffer Queues
 */
#define SL_ANDROID_MIME_AACADTS            ((SLchar *) "audio/vnd.android.aac-adts")

/*---------------------------------------------------------------------------*/
/* Acoustic Echo Cancellation (AEC) Interface                                */
/* --------------------------------------------------------------------------*/
extern SL_API const SLInterfaceID SL_IID_ANDROIDACOUSTICECHOCANCELLATION;

struct SLAndroidAcousticEchoCancellationItf_;
typedef const struct SLAndroidAcousticEchoCancellationItf_ * const *
        SLAndroidAcousticEchoCancellationItf;

struct SLAndroidAcousticEchoCancellationItf_ {
    SLresult (*SetEnabled)(
        SLAndroidAcousticEchoCancellationItf self,
        SLboolean enabled
    );
    SLresult (*IsEnabled)(
        SLAndroidAcousticEchoCancellationItf self,
        SLboolean *pEnabled
    );
};

/*---------------------------------------------------------------------------*/
/* Automatic Gain Control (ACC) Interface                                    */
/* --------------------------------------------------------------------------*/
extern SL_API const SLInterfaceID SL_IID_ANDROIDAUTOMATICGAINCONTROL;

struct SLAndroidAutomaticGainControlItf_;
typedef const struct SLAndroidAutomaticGainControlItf_ * const * SLAndroidAutomaticGainControlItf;

struct SLAndroidAutomaticGainControlItf_ {
    SLresult (*SetEnabled)(
        SLAndroidAutomaticGainControlItf self,
        SLboolean enabled
    );
    SLresult (*IsEnabled)(
        SLAndroidAutomaticGainControlItf self,
        SLboolean *pEnabled
    );
};

/*---------------------------------------------------------------------------*/
/* Noise Suppression Interface                                               */
/* --------------------------------------------------------------------------*/
extern SL_API const SLInterfaceID SL_IID_ANDROIDNOISESUPPRESSION;

struct SLAndroidNoiseSuppressionItf_;
typedef const struct SLAndroidNoiseSuppressionItf_ * const * SLAndroidNoiseSuppressionItf;

struct SLAndroidNoiseSuppressionItf_ {
    SLresult (*SetEnabled)(
        SLAndroidNoiseSuppressionItf self,
        SLboolean enabled
    );
    SLresult (*IsEnabled)(
        SLAndroidNoiseSuppressionItf self,
        SLboolean *pEnabled
    );
};

#ifdef __cplusplus
}
#endif /* __cplusplus */

#endif /* OPENSL_ES_ANDROID_H_ */
