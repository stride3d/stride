/*
 * Copyright (C) 2011 The Android Open Source Project
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

#ifndef OPENSL_ES_ANDROIDMETADATA_H_
#define OPENSL_ES_ANDROIDMETADATA_H_

#ifdef __cplusplus
extern "C" {
#endif

/*---------------------------------------------------------------------------*/
/* Android metadata keys                                                     */
/*---------------------------------------------------------------------------*/

/**
 * Additional metadata keys to be used in SLMetadataExtractionItf:
 *   the ANDROID_KEY_PCMFORMAT_* keys follow the fields of the SLDataFormat_PCM struct, and as such
 *   all values corresponding to these keys are of SLuint32 type, and are defined as the fields
 *   of the same name in SLDataFormat_PCM.  The exception is that sample rate is expressed here
 *   in Hz units, rather than in milliHz units.
 */
#define ANDROID_KEY_PCMFORMAT_NUMCHANNELS   "AndroidPcmFormatNumChannels"
#define ANDROID_KEY_PCMFORMAT_SAMPLERATE    "AndroidPcmFormatSampleRate"
#define ANDROID_KEY_PCMFORMAT_BITSPERSAMPLE "AndroidPcmFormatBitsPerSample"
#define ANDROID_KEY_PCMFORMAT_CONTAINERSIZE "AndroidPcmFormatContainerSize"
#define ANDROID_KEY_PCMFORMAT_CHANNELMASK   "AndroidPcmFormatChannelMask"
#define ANDROID_KEY_PCMFORMAT_ENDIANNESS    "AndroidPcmFormatEndianness"


#ifdef __cplusplus
}
#endif /* __cplusplus */

#endif /* OPENSL_ES_ANDROIDMETADATA_H_ */
