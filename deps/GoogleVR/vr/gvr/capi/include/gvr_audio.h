/* Copyright 2016 Google Inc. All rights reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#ifndef VR_GVR_CAPI_INCLUDE_GVR_AUDIO_H_
#define VR_GVR_CAPI_INCLUDE_GVR_AUDIO_H_

#if __ANDROID__
#include <jni.h>
#endif  // __ANDROID__

#include <stdint.h>

#include "vr/gvr/capi/include/gvr.h"
#include "vr/gvr/capi/include/gvr_types.h"

#ifdef __cplusplus
extern "C" {
#endif  // __cplusplus

/// @defgroup Audio Spatial Audio API
/// @brief This is the GVR Audio C API, a spatial audio rendering engine,
/// optimized for mobile VR.
///
/// It allows the user to spatialize sound sources in 3D space, including
/// distance and elevation cues. Specifically, the API is capable of playing
/// back spatial sound in three ways:
///
/// - **Sound object rendering**: This allows the user to create a virtual sound
///   source in 3D space. These sources, while spatialized, are fed with mono
///   audio data.
///
/// - **Ambisonic soundfields**: Ambisonic recordings are multi-channel audio
///   files which are spatialized all around the listener in 360 degrees. These
///   can be thought of as recorded or pre-baked soundfields. They can be of
///   great use for background effects which sound perfectly spatial.  Examples
///   include rain noise, crowd noise or even the sound of the ocean off to one
///   side.
///
/// - **Stereo Sounds**: This allows the user to directly play back
///   non-spatialized mono or stereo audio files. This is useful for music and
///   other such audio.
///
/// **Initialization**
///
///     gvr_audio_context* gvr_audio_create(int32_t rendering_mode);
///
/// The rendering_mode argument corresponds to a `gvr_audio_rendering_mode` enum
/// value, which specifies a rendering configuration setting:
///
/// - `GVR_AUDIO_RENDERING_STEREO_PANNING`:
///   Stereo panning of all sound objects. This disables HRTF-based rendering.
/// - `GVR_AUDIO_RENDERING_BINAURAL_LOW_QUALITY`:
///   This renders sound objects over a virtual array of 8 loudspeakers arranged
///   in a cube configuration around the listener’s head. HRTF-based rendering
///   is enabled.
/// - `GVR_AUDIO_RENDERING_BINAURAL_HIGH_QUALITY`:
///   This renders sound objects over a virtual array of 16 loudspeakers
///   arranged in an approximate equidistribution about the listener’s
///   head. HRTF-based rendering is enabled.
///
/// For most modern phones, the high quality mode offers a good balance between
/// performance and audio quality. To optimize the rendering performance for
/// headphones *and* speaker playback, the stereo speaker mode can be enabled
/// which automatically switches to stereo panning when no headphone is plugin.
/// Note that this can lead to varying CPU usage based on headphone and speaker
/// playback.
///
/// **Sound engine control**
///
/// Audio playback on the default audio device can be started and stopped by
/// calling the following two methods:
///
///     void gvr_audio_pause(gvr_audio_context* api);
///     void gvr_audio_resume(gvr_audio_context* api);
///
/// Note that:
///
///     void gvr_audio_update(gvr_audio_context* api);
///
/// must be called from the main thread at a regular rate. It is used to execute
/// background operations outside of the audio thread.
///
/// **Listener position and rotation**
///
/// To ensure that the audio in your application reacts to listener head
/// movement, it is important to update the listener's head orientation in the
/// graphics callback using the head orientation matrix.
///
/// The following methods can be used to control the listener’s head position
/// and orientation with the audio engine:
///
///     void gvr_audio_set_head_position(gvr_audio_context* api, float x,
///                                      float y, float z);
/// or
///
///     void gvr_audio_set_head_position_gvr(gvr_audio_context* api,
///                                          const gvr_vec3f& position);
///
/// and
///
///     void gvr_audio_set_head_rotation(gvr_audio_context* api,
///                                      float x, float y, float z, float w);
/// or
///
///     void gvr_audio_set_head_rotation_gvr(gvr_audio_context* api,
///                                          const gvr_quatf& rotation);
///
/// **Preloading Sounds**
///
/// Both mono sound files for use with Sound Objects and multi-channel Ambisonic
/// soundfield files can be preloaded into memory before playback or
/// alternatively streamed during playback. Preloading can be useful to reduce
/// CPU usage especially if the same audio clip is likely to be played back many
/// times. In this case playback latency is also reduced.
///
/// Sound files can be preloaded into memory by calling:
///
///     bool gvr_audio_preload_soundfile(gvr_audio_context* api,
///                                      const char* filename);
///
/// Unused sound files can be unloaded with a call to:
///
///     void gvr_audio_unload_soundfile(gvr_audio_context* api,
///                                     const char* filename);
///
/// NOTE: If a sound object, soundfield or stereo sound is created with a file
/// that has not been preloaded, that audio will be streamed.
///
/// **Spatializtion of sound objects**
///
/// The GVR Audio System allows the user to create virtual sound objects which
/// can be placed anywhere in space around the listener.
///
/// To create a new sound object, call:
///
///     gvr_audio_source_id
///     gvr_audio_create_sound_object(gvr_audio_context* api,
///                                   const char* filename);
///
/// This returns a handle that can be used to set properties such as the
/// position and the volume of the sound object via calls to the following two
/// functions:
///
///     void
///     gvr_audio_set_sound_object_position(gvr_audio_context* api,
///                                         gvr_audio_source_id sound_object_id,
///                                         float x, float y, float z);
///
///     void
///     gvr_audio_set_sound_volume(gvr_audio_context* api,
///                                gvr_audio_source_id source_id, float volume);
///
/// The behavior of Sound Objects with respect to their distance from the
/// listener can be controlled via calls to the following method:
///
///     void gvr_audio_set_sound_object_distance_rolloff_model(
///         gvr_audio_context* api, gvr_audio_source_id sound_object_id,
///         int32_t rolloff_model, float min_distance, float max_distance);
///
/// This enables a user to choose between logarithmic and linear distance
/// rolloff methods, or to completely disable distance rolloff effects.
///
///
/// The spatialized playback of a sound object can be triggered with a call to:
///
///     void gvr_audio_play_sound(gvr_audio_context* api,
///                               gvr_audio_source_id source_id,
///                               bool looping_enabled);
///
/// and stopped with a call to:
///
///     void gvr_audio_stop_sound(gvr_audio_context* api,
///                               gvr_audio_source_id source_id);
///
/// Note that the sound object handle destroys itself at the moment the sound
/// playback has stopped. This way, no clean up of sound object handles is
/// needed. On subsequent calls to this function the corresponding
/// gvr_audio_source_id no longer refers to a valid sound object.
///
/// The following function can be used to check if a sound object is currently
/// active:
///
///     bool gvr_audio_is_sound_playing(const gvr_audio_context* api,
///                                     gvr_audio_source_id source_id);
///
/// **Rendering of ambisonic soundfields**
///
/// The GVR Audio System also provides the user with the ability to play back
/// ambisonic soundfields. Ambisonic soundfields are captured or pre-rendered
/// 360 degree recordings. It is best to think of them as equivalent to 360
/// degree video. While they envelop and surround the listener, they only react
/// to the listener's rotational movement. That is, one cannot walk towards
/// features in the soundfield. Soundfields are ideal for accompanying 360
/// degree video playback, for introducing background and environmental effects
/// such as rain or crowd noise, or even for pre baking 3D audio to reduce
/// rendering costs.  The GVR Audio System supports full 3D First Order
/// Ambisonic recordings using ACN channel ordering and SN3D normalization. For
/// more information please see our Spatial Audio specification at:
/// https://github.com/google/spatial-media/blob/master/docs/spatial-audio-rfc.md#semantics
///
/// Note that Soundfield playback is directly streamed from the sound file and
/// no sound file preloading is needed.
///
/// To obtain a soundfield handler, call:
///
///     gvr_audio_source_id gvr_audio_create_soundfield(gvr_audio_context* api,
///                                                    const char* filename);
///
/// This returns a gvr_audio_source_id handle that allows the user to begin
/// playback of the soundfield, to alter the soundfield’s volume or to stop
/// soundfield playback and as such destroy the object. These actions can be
/// achieved with calls to the following functions:
///
///     void gvr_audio_play_sound(gvr_audio_context* api,
///                               gvr_audio_source_id source_id,
///                               bool looping_enabled);
///
///     void gvr_audio_set_sound_volume(gvr_audio_context* api,
///                                     gvr_audio_source_id source_id,
///                                     float volume);
///
///     void gvr_audio_stop_sound(gvr_audio_context* api,
///                               gvr_audio_source_id source_id);
///
/// Ambisonic soundfields can also be rotated about the listener's head in order
/// to align the components of the soundfield with the visuals of the game/app.
///
/// void gvr_audio_set_soundfield_rotation(gvr_audio_context* api,
///                                        gvr_audio_source_id soundfield_id,
///                                        const gvr_quatf&
///                                        soundfield_rotation);
///
/// **Direct Playback of Stereo or Mono Sounds**
///
/// The GVR Audio System allows the direct non-spatialized playback of both
/// stereo and mono audio. Such audio is often used for music or sound effects
/// that should not be spatialized.
///
/// A stereo sound can be created with a call to:
///
/// gvr_audio_source_id gvr_audio_create_stereo_sound(gvr_audio_context* api,
///                                                  const char* filename);
///
/// **Room effects**
///
/// The GVR Audio System provides a powerful reverb engine which can be used to
/// create customized room effects by specifying the size of a room and a
/// material for each surface of the room from the gvr_audio_material_name enum.
/// Each of these surface materials has unique absorption properties which
/// differ with frequency. The room created will be centered around the
/// listener. Note that the Google VR Audio System uses meters as the unit of
/// distance throughout.
///
/// The following methods are used to control room effects:
///
///     void gvr_audio_enable_room(gvr_audio_context* api, bool enable);
///
/// enables or disables room effects with smooth transitions.
///
/// and
///
///     void
///     gvr_audio_set_room_properties(gvr_audio_context* api, float size_x,
///                                   float size_y, float size_z,
///                                   gvr_audio_material_name wall_material,
///                                   gvr_audio_material_name ceiling_material,
///                                   gvr_audio_material_name floor_material);
///
/// allows the user to describe the room based on its dimensions and its surface
/// properties. For example, one can expect very large rooms to be more
/// reverberant than smaller rooms, and a room with with hard surface materials
/// such as brick to be more reverberant than one with soft absorbent materials
/// such as heavy curtains on every surface.
///
/// Note that when a sound source is located outside of the listener's room,
/// it will sound different from sources located within the room due to
/// attenuation of both the direct sound and the reverb on that source. Sources
/// located far outside of the listener's room will not be audible to the
/// listener.
///
/// The following method can be used to subtly adjust the reverb in a room by
/// changing the gain/attenuation on the reverb, setting a multiplier on the
/// reverberation time to control the reverb's length, or adjusting the balance
/// between the low and high frequency components of the reverb.
///
/// void gvr_audio_set_room_reverb_adjustments(gvr_audio_context* api,
///                                            float gain,
///                                            float time_adjust,
///                                            float brightness_adjust);
///
/// If you are writing C++ code, you might prefer to use the C++ wrapper
/// rather than implement this C API directly.
///
/// **Example usage (C++ API)**
///
/// Construction:
///
///     std::unique_ptr<gvr::AudioApi> gvr_audio_api(new gvr::AudioApi);
///     gvr_audio_api->Init(GVR_AUDIO_RENDERING_BINAURAL_HIGH_QUALITY);
///
/// Update head rotation in DrawFrame():
///
///     head_pose_ = gvr_api_->GetHeadSpaceFromStartSpaceRotation(target_time);
///     gvr_audio_api_->SetHeadPose(head_pose_);
///     gvr_audio_api_->Update();
///
/// Preload sound file, create sound handle and start playback:
///
///     gvr_audio_api->PreloadSoundfile(kSoundFile);
///     AudioSourceId source_id =
///                   gvr_audio_api_->CreateSoundObject("sound.wav");
///     gvr_audio_api->SetSoundObjectPosition(source_id,
///                                           position_x,
///                                           position_y,
///                                           position_z);
///     gvr_audio_api->PlaySound(source_id, true /* looped playback */);
///

/// @{

typedef struct gvr_audio_context_ gvr_audio_context;

/// Creates and initializes a gvr_audio_context. This call also initializes
/// the audio interface and starts the audio engine. Note that the returned
/// instance must be deleted with gvr_audio_destroy.
///
#ifdef __ANDROID__
/// @param env The JNI Env associated with the current thread.
/// @param android_context The Android application context. This must be the
///     application context, NOT an Activity context (Note: from any Android
///     Activity in your app, you can call getApplicationContext() to
///     retrieve the application context).
/// @param class_loader The class loader to use when loading Java
///     classes. This must be your app's main class loader (usually
///     accessible through activity.getClassLoader() on any of your Activities).
/// @param rendering_mode The gvr_audio_rendering_mode value which determines
///     the rendering configuration preset. This is passed as an int32_t to
///     ensure API compatibility.
/// @return gvr_audio_context instance.
gvr_audio_context* gvr_audio_create(JNIEnv* env, jobject android_context,
                                    jobject class_loader,
                                    int32_t rendering_mode);
#else
/// @param rendering_mode The gvr_audio_rendering_mode value which determines
///     the rendering configuration preset. This is passed as an int32_t to
///     ensure API compatibility.
/// @return gvr_audio_context instance.
gvr_audio_context* gvr_audio_create(int32_t rendering_mode);
#endif  // #ifdef __ANDROID__

/// Destroys a gvr_audio_context that was previously created with
/// gvr_audio_create or gvr_audio_create_android.
///
/// @param api Pointer to a pointer to a gvr_audio_context. The pointer
///     will be set to NULL after destruction.
void gvr_audio_destroy(gvr_audio_context** api);

/// Resumes the VR Audio system.
/// Call this when your app/game loses focus.
/// Calling this when not paused is a no-op.
/// Thread-safe (call from any thread).
///
/// @param api Pointer to a gvr_audio_context.
void gvr_audio_resume(gvr_audio_context* api);

/// Pauses the VR Audio system.
/// Calling this when already paused is a no-op.
/// Thread-safe (call from any thread).
///
/// @param api Pointer to a gvr_audio_context.
void gvr_audio_pause(gvr_audio_context* api);

/// This method must be called from the main thread at a regular rate. It is
/// used to execute background operations outside of the audio thread.
///
/// @param api Pointer to a gvr_audio_context.
void gvr_audio_update(gvr_audio_context* api);

/// Preloads a local sound file. Note that the local file access method
/// depends on the target platform.
///
/// @param api Pointer to a gvr_audio_context.
/// @param filename Name of the file, used as identifier.
/// @return True on success or if file has already been preloaded.
bool gvr_audio_preload_soundfile(gvr_audio_context* api, const char* filename);

/// Unloads a previously preloaded sample from memory. Note that if the sample
/// is currently used, the memory is freed at the moment playback stops.
///
/// @param api Pointer to a gvr_audio_context.
/// @param filename Name of the file, used as identifier.
void gvr_audio_unload_soundfile(gvr_audio_context* api, const char* filename);

/// Returns a new sound object. Note that the sample referred to needs to be
/// preloaded and may only contain a single audio channel (mono).  The handle
/// automatically destroys itself at the moment the sound playback has stopped.
///
/// @param api Pointer to a gvr_audio_context.
/// @param filename The path/name of the file to be played.
/// @return Id of new sound object. Returns kInvalidId if the sound file has not
///     been preloaded or if the number of input channels is > 1.
gvr_audio_source_id gvr_audio_create_sound_object(gvr_audio_context* api,
                                                 const char* filename);

/// Returns a new ambisonic sound field. Note that the sample needs to be
/// preloaded and must have 4 separate audio channels. The handle automatically
/// destroys itself at the moment the sound playback has stopped.
///
/// @param api Pointer to a gvr_audio_context.
/// @param filename The path/name of the file to be played.
/// @return Id of new soundfield. Returns kInvalidId if the sound file has not
///     been preloaded or if the number of input channels does not match that
///     required.
gvr_audio_source_id gvr_audio_create_soundfield(gvr_audio_context* api,
                                               const char* filename);

/// Returns a new stereo non-spatialized source, which directly plays back mono
/// or stereo audio. Note the sample needs to be preloaded and may contain only
/// one (mono) or two (stereo) audio channels.
///
/// @param api Pointer to a gvr_audio_context.
/// @param filename The path/name of the file to be played..
/// @return Id of new stereo non-spatialized source. Returns kInvalidId if the
///     sound file has not been preloaded or if the number of input channels is
///     > 2;
gvr_audio_source_id gvr_audio_create_stereo_sound(gvr_audio_context* api,
                                                 const char* filename);

/// Starts the playback of a sound.
///
/// @param api Pointer to a gvr_audio_context.
/// @param source_id Id of the audio source to be stopped.
/// @param looping_enabled Enables looped audio playback.
void gvr_audio_play_sound(gvr_audio_context* api, gvr_audio_source_id source_id,
                          bool looping_enabled);

/// Pauses the playback of a sound.
///
/// @param api Pointer to a gvr_audio_context.
/// @param source_id Id of the audio source to be paused.
void gvr_audio_pause_sound(gvr_audio_context* api,
                           gvr_audio_source_id source_id);

/// Resumes the playback of a sound.
///
/// @param api Pointer to a gvr_audio_context.
/// @param source_id Id of the audio source to be resumed.
void gvr_audio_resume_sound(gvr_audio_context* api,
                            gvr_audio_source_id source_id);

/// Stops the playback of a sound and destroys the corresponding sound object
/// or Soundfield.
///
/// @param api Pointer to a gvr_audio_context.
/// @param source_id Id of the audio source to be stopped.
void gvr_audio_stop_sound(gvr_audio_context* api,
                          gvr_audio_source_id source_id);

/// Repositions an existing sound object.
///
/// @param api Pointer to a gvr_audio_context.
/// @param sound_object_id Id of the sound object to be moved.
/// @param x X coordinate the sound will be placed at.
/// @param y Y coordinate the sound will be placed at.
/// @param z Z coordinate the sound will be placed at.
void gvr_audio_set_sound_object_position(gvr_audio_context* api,
                                         gvr_audio_source_id sound_object_id,
                                         float x, float y, float z);

/// Sets the given ambisonic soundfields's rotation.
///
/// @param api Pointer to a gvr_audio_context.
/// @param soundfield_id Id of the soundfield source to be rotated.
/// @param soundfield_rotation Quaternion representing the soundfield rotation.
void gvr_audio_set_soundfield_rotation(gvr_audio_context* api,
                                       gvr_audio_source_id soundfield_id,
                                       const gvr_quatf& soundfield_rotation);

/// Sets the given sound object source's distance attenuation method with
/// minimum and maximum distances. Maximum distance must be greater than the
/// minimum distance for the method to be set.
///
/// @param api Pointer to a gvr_audio_context.
/// @param sound_object_id Id of sound object source.
/// @param rolloff_model Linear or logarithmic distance rolloff models. Note
///     setting the rolloff model to |GVR_AUDIO_ROLLOFF_NONE| will allow
///     distance attenuation values to be set manually.
/// @param min_distance Minimum distance to apply distance attenuation method.
/// @param max_distance Maximum distance to apply distance attenuation method.
void gvr_audio_set_sound_object_distance_rolloff_model(
    gvr_audio_context* api, gvr_audio_source_id sound_object_id,
    int32_t rolloff_model, float min_distance, float max_distance);

/// Changes the volume of an existing sound.
///
/// @param api Pointer to a gvr_audio_context.
/// @param source_id Id of the audio source to be modified.
/// @param volume Volume value. Should range from 0 (mute) to 1 (max).
void gvr_audio_set_sound_volume(gvr_audio_context* api,
                                gvr_audio_source_id source_id, float volume);

/// Checks if a sound is playing.
///
/// @param api Pointer to a gvr_audio_context.
/// @param source_id Id of the audio source to be checked.
/// @return True if the sound is being played.
bool gvr_audio_is_sound_playing(const gvr_audio_context* api,
                                gvr_audio_source_id source_id);

/// Sets the head pose from a matrix representation of the same.
///
/// @param api Pointer to a gvr_audio_context on which to set the pose.
/// @param head_pose_matrix Matrix representing the head transform to be set.
void gvr_audio_set_head_pose(gvr_audio_context* api,
                             const gvr_mat4f& head_pose_matrix);

/// Turns on/off the room reverberation effect.
///
/// @param api Pointer to a gvr_audio_context.
/// @param enable True to enable room effect.
void gvr_audio_enable_room(gvr_audio_context* api, bool enable);

/// Sets the room properties describing the dimensions and surface materials of
/// a given room.
///
/// @param api Pointer to a gvr_audio_context.
/// @param size_x Dimension along X axis.
/// @param size_y Dimension along Y axis.
/// @param size_z Dimension along Z axis.
/// @param wall_material Surface gvr_audio_material_type for the four walls.
/// @param ceiling_material Surface gvr_audio_material_type for the ceiling.
/// @param floor_material Surface gvr_audio_material_type for the floor.
void gvr_audio_set_room_properties(gvr_audio_context* api, float size_x,
                                   float size_y, float size_z,
                                   int32_t wall_material,
                                   int32_t ceiling_material,
                                   int32_t floor_material);

/// Adjusts the properties of the current reverb, allowing changes to the
/// reverb's gain, duration and low/high frequency balance.
///
/// @param api Pointer to a gvr_audio_context.
/// @param gain Reverb volume (linear) adjustment in range [0, 1] for
///     attenuation, range [1, inf) for gain boost.
/// @param time_adjust Reverb time adjustment multiplier to scale the
///     reverberation tail length. This value should be >= 0.
/// @param brightness_adjust Reverb brightness adjustment that controls the
///     reverberation ratio across low and high frequency bands.
void gvr_audio_set_room_reverb_adjustments(gvr_audio_context* api, float gain,
                                           float time_adjust,
                                           float brightness_adjust);

/// Enables the stereo speaker mode. It enforces stereo-panning when headphones
/// are *not* plugged into the phone. This helps to avoid HRTF-based coloring
/// effects and reduces computational complexity when speaker playback is
/// active. By default the stereo speaker mode optimization is disabled.
///
/// @param api Pointer to a gvr_audio_context.
/// @param enable True to enable the stereo speaker mode.
void gvr_audio_enable_stereo_speaker_mode(gvr_audio_context* api, bool enable);

/// @}

#ifdef __cplusplus
}  // extern "C"
#endif

// Convenience C++ wrapper.
#if defined(__cplusplus) && !defined(GVR_NO_CPP_WRAPPER)

#include <memory>
#include <string>

namespace gvr {
/// This is a convenience C++ wrapper for the Audio C API.
///
/// This wrapper strategy prevents ABI compatibility issues between compilers
/// by ensuring that the interface between client code and the implementation
/// code in libgvr.so is a pure C interface. The translation from C++ calls
/// to C calls provided by this wrapper runs entirely in the client's binary
/// and is compiled by the client's compiler.
///
/// Methods in this class are only documented insofar as the C++ wrapping logic
/// is concerned; for information about the method itself, please refer to the
/// corresponding function in the C API.
///
///
/// THREADING: this class is thread-safe and reentrant after initialized
/// with Init().
class AudioApi {
 public:
  /// Creates an (uninitialized) ControllerApi object. You must initialize
  /// it by calling Init() before interacting with it.
  AudioApi() : context_(nullptr) {}

  ~AudioApi() {
    if (context_) {
      gvr_audio_destroy(&context_);
    }
  }

/// Creates and initializes a gvr_audio_context.
/// For more information, see gvr_audio_create().
#ifdef __ANDROID__
  bool Init(JNIEnv* env, jobject android_context, jobject class_loader,
            AudioRenderingMode rendering_mode) {
    context_ =
        gvr_audio_create(env, android_context, class_loader, rendering_mode);
    return context_ != nullptr;
  }
#else
  bool Init(AudioRenderingMode rendering_mode) {
    context_ = gvr_audio_create(rendering_mode);
    return context_ != nullptr;
  }
#endif  // #ifdef __ANDROID__

  /// Pauses the audio engine.
  /// For more information, see gvr_audio_pause().
  void Pause() { gvr_audio_pause(context_); }

  /// Resumes the audio engine.
  /// For more information, see gvr_audio_resume().
  void Resume() { gvr_audio_resume(context_); }

  /// For more information, see gvr_audio_update().
  void Update() { gvr_audio_update(context_); }

  /// Preloads a local sound file.
  /// For more information, see gvr_audio_preload_soundfile().
  bool PreloadSoundfile(const std::string& filename) {
    return gvr_audio_preload_soundfile(context_, filename.c_str());
  }

  /// Unloads a previously preloaded sample from memory.
  /// For more information, see gvr_audio_preload_soundfile().
  void UnloadSoundfile(const std::string& filename) {
    gvr_audio_unload_soundfile(context_, filename.c_str());
  }

  /// Returns a new sound object.
  /// For more information, see gvr_audio_create_sound_object().
  AudioSourceId CreateSoundObject(const std::string& filename) {
    return gvr_audio_create_sound_object(context_, filename.c_str());
  }

  /// Returns a new sound field.
  /// For more information, see gvr_audio_create_soundfield().
  AudioSourceId CreateSoundfield(const std::string& filename) {
    return gvr_audio_create_soundfield(context_, filename.c_str());
  }

  /// Returns a new stereo soound.
  /// For more information, see gvr_audio_create_stereo_sound().
  AudioSourceId CreateStereoSound(const std::string& filename) {
    return gvr_audio_create_stereo_sound(context_, filename.c_str());
  }

  /// Starts the playback of a sound.
  /// For more information, see gvr_audio_play_sound().
  void PlaySound(AudioSourceId source_id, bool looping_enabled) {
    gvr_audio_play_sound(context_, source_id, looping_enabled);
  }

  /// Pauses the playback of a sound.
  /// For more information, see gvr_audio_pause_sound().
  void PauseSound(AudioSourceId source_id) {
    gvr_audio_pause_sound(context_, source_id);
  }

  /// Resumes the playback of a sound.
  /// For more information, see gvr_audio_resume_sound().
  void ResumeSound(AudioSourceId source_id) {
    gvr_audio_resume_sound(context_, source_id);
  }

  /// Stops the playback of a sound.
  /// For more information, see gvr_audio_stop_sound().
  void StopSound(AudioSourceId source_id) {
    gvr_audio_stop_sound(context_, source_id);
  }

  /// Repositions an existing sound object.
  /// For more information, see gvr_audio_set_sound_object_position().
  void SetSoundObjectPosition(AudioSourceId sound_object_id, float x, float y,
                              float z) {
    gvr_audio_set_sound_object_position(context_, sound_object_id, x, y, z);
  }

  void SetSoundObjectDistanceRolloffModel(
      AudioSourceId sound_object_id,
      gvr_audio_distance_rolloff_type rolloff_model, float min_distance,
      float max_distance) {
    gvr_audio_set_sound_object_distance_rolloff_model(
        context_, sound_object_id, rolloff_model, min_distance, max_distance);
  }

  /// Rotates an existing soundfield.
  /// For more information, see gvr_audio_set_soundfield_rotation().
  void SetSoundfieldRotation(AudioSourceId soundfield_id,
                             const Quatf& soundfield_rotation) {
    gvr_audio_set_soundfield_rotation(context_, soundfield_id,
                                      soundfield_rotation);
  }

  /// Changes the volume of an existing sound.
  /// For more information, see gvr_audio_set_sound_volume().
  void SetSoundVolume(AudioSourceId source_id, float volume) {
    gvr_audio_set_sound_volume(context_, source_id, volume);
  }

  /// Checks if a sound is playing.
  /// For more information, see gvr_audio_is_sound_playing().
  bool IsSoundPlaying(AudioSourceId source_id) const {
    return gvr_audio_is_sound_playing(context_, source_id);
  }

  /// Sets the head position from a matrix representation.
  /// For more information, see gvr_audio_set_head_pose().
  void SetHeadPose(const Mat4f& head_pose_matrix) {
    gvr_audio_set_head_pose(context_, head_pose_matrix);
  }

  /// Turns on/off the room reverberation effect.
  /// For more information, see gvr_audio_enable_room().
  void EnableRoom(bool enable) { gvr_audio_enable_room(context_, enable); }

  /// Sets the room properties describing the dimensions and surface materials
  /// of a given room. For more information, see
  /// gvr_audio_set_room_properties().
  void SetRoomProperties(float size_x, float size_y, float size_z,
                         gvr_audio_material_type wall_material,
                         gvr_audio_material_type ceiling_material,
                         gvr_audio_material_type floor_material) {
    gvr_audio_set_room_properties(context_, size_x, size_y, size_z,
                                  wall_material, ceiling_material,
                                  floor_material);
  }

  /// Adjusts the properties of the current reverb, allowing changes to the
  /// reverb's gain, duration and low/high frequency balance. For more
  /// information see gvr_audio_set_room_reverb_adjustments().
  void SetRoomReverbAdjustments(float gain, float time_adjust,
                                float brightness_adjust) {
    gvr_audio_set_room_reverb_adjustments(context_, gain, time_adjust,
                                          brightness_adjust);
  }

  /// Enables the stereo speaker mode. For more information see
  /// gvr_audio_enable_stereo_speaker_mode().
  void EnableStereoSpeakerMode(bool enable) {
    gvr_audio_enable_stereo_speaker_mode(context_, enable);
  }

  /// @name Wrapper manipulation
  /// @{
  /// Creates a C++ wrapper for a C object and takes ownership.
  explicit AudioApi(gvr_audio_context* context)
      : context_(context) {}

  /// Returns the wrapped C object. Does not affect ownership.
  gvr_audio_context* cobj() { return context_; }
  const gvr_audio_context* cobj() const { return context_; }

  /// Returns the wrapped C object and transfers its ownership to the caller.
  /// The wrapper becomes invalid and should not be used.
  gvr_audio_context* Release() {
    auto result = context_;
    context_ = nullptr;
    return result;
  }
  /// @}

 private:
  gvr_audio_context* context_;

  // Disallow copy and assign:
  AudioApi(const AudioApi&);
  void operator=(const AudioApi&);
};

}  // namespace gvr
#endif  // #if defined(__cplusplus) && !defined(GVR_NO_CPP_WRAPPER)

#endif  // VR_GVR_CAPI_INCLUDE_GVR_AUDIO_H_
