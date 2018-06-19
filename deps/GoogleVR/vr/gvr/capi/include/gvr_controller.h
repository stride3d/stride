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

#ifndef VR_GVR_CAPI_INCLUDE_GVR_CONTROLLER_H_
#define VR_GVR_CAPI_INCLUDE_GVR_CONTROLLER_H_

#ifdef __ANDROID__
#include <jni.h>
#endif

#include <stdint.h>

#include "vr/gvr/capi/include/gvr.h"
#include "vr/gvr/capi/include/gvr_types.h"

#ifdef __cplusplus
extern "C" {
#endif

/// @defgroup Controller Controller API
/// @brief This is the Controller C API, which allows access to a VR controller.
///
/// If you are writing C++ code, you might prefer to use the C++ wrapper rather
/// than implement this C API directly.
///
/// Typical initialization example:
///
///     // Get your gvr_context* pointer from GvrLayout:
///     gvr_context* gvr = ......;  // (get from GvrLayout in Java)
///
///     // Set up the API features:
///     int32_t options = gvr_controller_get_default_options();
///
///     // Enable non-default options, if needed:
///     options |= GVR_CONTROLLER_ENABLE_GYRO | GVR_CONTROLLER_ENABLE_ACCEL;
///
///     // Create and init:
///     gvr_controller_context* context =
///         gvr_controller_create_and_init(options, gvr);
///
///     // Check if init was successful.
///     if (!context) {
///       // Handle error.
///       return;
///     }
///
///     gvr_controller_state* state = gvr_controller_state_create();
///
///     // Resume:
///     gvr_controller_resume(api);
///
/// Usage:
///
///     void DrawFrame() {
///       gvr_controller_state_update(context, 0, state);
///       // ... process controller state ...
///     }
///
///     // When your application gets paused:
///     void OnPause() {
///       gvr_controller_pause(context);
///     }
///
///     // When your application gets resumed:
///     void OnResume() {
///       gvr_controller_resume(context);
///     }
///
/// To conserve battery, be sure to call gvr_controller_pause and
/// gvr_controller_resume when your app gets paused and resumed, respectively.
///
/// THREADING: unless otherwise noted, all functions are thread-safe, so
/// you can operate on the same gvr_controller_context object from multiple
/// threads.
/// @{

/// Represents a Daydream Controller API object, used to invoke the
/// Daydream Controller API.
typedef struct gvr_controller_context_ gvr_controller_context;

/// Returns the default features for the controller API.
///
/// @return The set of default features, as bit flags (an OR'ed combination of
///     the GVR_CONTROLLER_ENABLE_* feature flags).
int32_t gvr_controller_get_default_options();

/// Creates and initializes a gvr_controller_context instance which can be used
/// to invoke the Daydream Controller API functions. Important: after creation
/// the API will be in the paused state (the controller will be inactive).
/// You must call gvr_controller_resume() explicitly (typically, in your Android
/// app's onResume() callback).
///
/// @param options The API options. To get the defaults, use
///     gvr_controller_get_default_options().
/// @param context The GVR Context object to sync with (optional).
///     This can be nullptr. If provided, the context's state will
///     be synchronized with the controller's state where possible. For
///     example, when the user recenters the controller, this will
///     automatically recenter head tracking as well.
///     WARNING: the caller is responsible for making sure the pointer
///     remains valid for the lifetime of this object.
/// @return A pointer to the initialized API, or NULL if an error occurs.
gvr_controller_context* gvr_controller_create_and_init(
    int32_t options, gvr_context* context);

#ifdef __ANDROID__
/// Creates and initializes a gvr_controller_context instance with an explicit
/// Android context and class loader.
///
/// @param env The JNI Env associated with the current thread.
/// @param android_context The Android application context. This must be the
///     application context, NOT an Activity context (Note: from any Android
///     Activity in your app, you can call getApplicationContext() to
///     retrieve the application context).
/// @param class_loader The class loader to use when loading Java
///     classes. This must be your app's main class loader (usually
///     accessible through activity.getClassLoader() on any of your Activities).
/// @param options The API options. To get the defaults, use
///     gvr_controller_get_default_options().
/// @param context The GVR Context object to sync with (optional).
///     This can be nullptr. If provided, the context's state will
///     be synchronized with the controller's state where possible. For
///     example, when the user recenters the controller, this will
///     automatically recenter head tracking as well.
///     WARNING: the caller is responsible for making sure the pointer
///     remains valid for the lifetime of this object.
/// @return A pointer to the initialized API, or NULL if an error occurs.
gvr_controller_context* gvr_controller_create_and_init_android(
    JNIEnv *env, jobject android_context, jobject class_loader,
    int32_t options, gvr_context* context);
#endif  // #ifdef __ANDROID__

/// Destroys a gvr_controller_context that was previously created with
/// gvr_controller_init.
///
/// @param api Pointer to a pointer to a gvr_controller_context. The pointer
///     will be set to NULL after destruction.
void gvr_controller_destroy(gvr_controller_context** api);

/// Pauses the controller, possibly releasing resources.
/// Call this when your app/game loses focus.
/// Calling this when already paused is a no-op.
/// Thread-safe (call from any thread).
///
/// @param api Pointer to a pointer to a gvr_controller_context.
void gvr_controller_pause(gvr_controller_context* api);

/// Resumes the controller. Call this when your app/game regains focus.
/// Calling this when already resumed is a no-op.
/// Thread-safe (call from any thread).
///
/// @param api Pointer to a pointer to a gvr_controller_context.
void gvr_controller_resume(gvr_controller_context* api);

/// Convenience to convert an API status code to string. The returned pointer
/// is static and valid throughout the lifetime of the application.
///
/// @param status The gvr_controller_api_status to convert to string.
/// @return A pointer to a string that describes the value.
const char* gvr_controller_api_status_to_string(int32_t status);

/// Convenience to convert an connection state to string. The returned pointer
/// is static and valid throughout the lifetime of the application.
///
/// @param state The state to convert to string.
/// @return A pointer to a string that describes the value.
const char* gvr_controller_connection_state_to_string(int32_t state);

/// Convenience to convert an connection state to string. The returned pointer
/// is static and valid throughout the lifetime of the application.
///
/// @param button The gvr_controller_button to convert to string.
/// @return A pointer to a string that describes the value.
const char* gvr_controller_button_to_string(int32_t button);

/// Creates a gvr_controller_state.
gvr_controller_state* gvr_controller_state_create();

/// Destroys a a gvr_controller_state that was previously created with
/// gvr_controller_state_create.
void gvr_controller_state_destroy(gvr_controller_state** state);

/// Updates the controller state. Reading the controller state is not a
/// const getter: it has side-effects. In particular, some of the
/// gvr_controller_state fields (the ones documented as "transient") represent
/// one-time events and will be true for only one read operation, and false
/// in subsequente reads.
///
/// @param api Pointer to a pointer to a gvr_controller_context.
/// @param flags Optional flags reserved for future use. A value of 0 should be
///     used until corresponding flag attributes are defined and documented.
/// @param out_state A pointer where the controller's state
///     is to be written. This must have been allocated with
///     gvr_controller_state_create().
void gvr_controller_state_update(gvr_controller_context* api, int32_t flags,
                                 gvr_controller_state* out_state);

/// Gets the API status of the controller state. Returns one of the
/// gvr_controller_api_status variants, but returned as an int32_t for ABI
/// compatibility.
int32_t gvr_controller_state_get_api_status(const gvr_controller_state* state);

/// Gets the connection state of the controller. Returns one of the
/// gvr_controller_connection_state variants, but returned as an int32_t for ABI
/// compatibility.
int32_t gvr_controller_state_get_connection_state(
    const gvr_controller_state* state);

/// Returns the current controller orientation, in Start Space. The Start Space
/// is the same space as the headset space and has these three axes
/// (right-handed):
///
/// * The positive X axis points to the right.
/// * The positive Y axis points upwards.
/// * The positive Z axis points backwards.
///
/// The definition of "backwards" and "to the right" are based on the position
/// of the controller when tracking started. For Daydream, this is when the
/// controller was first connected in the "Connect your Controller" screen
/// which is shown when the user enters VR.
///
/// The definition of "upwards" is given by gravity (away from the pull of
/// gravity). This API may not work in environments without gravity, such
/// as space stations or near the center of the Earth.
///
/// Since the coordinate system is right-handed, rotations are given by the
/// right-hand rule. For example, rotating the controller counter-clockwise
/// on a table top as seen from above means a positive rotation about the
/// Y axis, while clockwise would mean negative.
///
/// Note that this is the Start Space for the *controller*, which initially
/// coincides with the Start Space for the headset, but they may diverge over
/// time due to controller/headset drift. A recentering operation will bring
/// the two spaces back into sync.
///
/// Remember that a quaternion expresses a rotation. Given a rotation of theta
/// radians about the (x, y, z) axis, the corresponding quaternion (in
/// xyzw order) is:
///
///     (x * sin(theta/2), y * sin(theta/2), z * sin(theta/2), cos(theta/2))
///
/// Here are some examples of orientations of the controller and their
/// corresponding quaternions, all given in xyzw order:
///
///   * Initial pose, pointing forward and lying flat on a surface: identity
///     quaternion (0, 0, 0, 1). Corresponds to "no rotation".
///
///   * Flat on table, rotated 90 degrees counter-clockwise: (0, 0.7071, 0,
///     0.7071). Corresponds to a +90 degree rotation about the Y axis.
///
///   * Flat on table, rotated 90 degrees clockwise: (0, -0.7071, 0, 0.7071).
///     Corresponds to a -90 degree rotation about the Y axis.
///
///   * Flat on table, rotated 180 degrees (pointing backwards): (0, 1, 0, 0).
///     Corresponds to a 180 degree rotation about the Y axis.
///
///   * Pointing straight up towards the sky: (0.7071, 0, 0, 0.7071).
///     Corresponds to a +90 degree rotation about the X axis.
///
///   * Pointing straight down towards the ground: (-0.7071, 0, 0, 0.7071).
///     Corresponds to a -90 degree rotation about the X axis.
///
///   * Banked 90 degrees to the left: (0, 0, 0.7071, 0.7071). Corresponds
///     to a +90 degree rotation about the Z axis.
///
///   * Banked 90 degrees to the right: (0, 0, -0.7071, 0.7071). Corresponds
///     to a -90 degree rotation about the Z axis.
gvr_quatf gvr_controller_state_get_orientation(
    const gvr_controller_state* state);

/// Returns the current controller gyro reading, in Start Space.
///
/// The gyro measures the controller's angular speed in radians per second.
/// Note that this is an angular *speed*, so it reflects how fast the
/// controller's orientation is changing with time.
/// In particular, if the controller is not being rotated, the angular speed
/// will be zero on all axes, regardless of the current pose.
///
/// The axes are in the controller's device space. Specifically:
///
///    * The X axis points to the right of the controller.
///    * The Y axis points upwards perpendicular to the top surface of the
///      controller.
///    * The Z axis points backwards along the body of the controller,
///      towards its rear, where the charging port is.
///
/// As usual in a right-handed coordinate system, the sign of the angular
/// velocity is given by the right-hand rule. So, for example:
///
///    * If the controller is flat on a table top spinning counter-clockwise
///      as seen from above, you will read a positive angular velocity
///      about the Y axis. Clockwise would be negative.
///    * If the controller is initially pointing forward and lying flat and
///      is then gradually angled up so that its tip points towards the sky,
///      it will report a positive angular velocity about the X axis during
///      that motion. Likewise, angling it down will report a negative angular
///      velocity about the X axis.
///    * If the controller is banked (rolled) to the right, this will
///      report a negative angular velocity about the Z axis during the
///      motion (remember the Z axis points backwards along the controller).
///      Banking to the left will report a positive angular velocity about
///      the Z axis.
gvr_vec3f gvr_controller_state_get_gyro(const gvr_controller_state* state);

/// Current (latest) controller accelerometer reading, in Start Space.
///
/// The accelerometer indicates the direction in which the controller feels
/// an acceleration, including gravity. The reading is given in meters
/// per second squared (m/s^2). The axes are the same as for the gyro.
/// To have an intuition for the signs used in the accelerometer, it is useful
/// to imagine that, when at rest, the controller is being "pushed" by a
/// force opposite to gravity. It is as if, by the equivalency princle, it were
/// on a frame of reference that is accelerating in the opposite direction to
/// gravity. For example:
///
///   * If the controller is lying flat on a table top, it will read a positive
///     acceleration of about 9.8 m/s^2 along the Y axis, corresponding to
///     the acceleration of gravity (as if the table were pushing the controller
///     upwards at 9.8 m/s^2 to counteract gravity).
///   * If, in that situation, the controller is now accelerated upwards at
///     3.0 m/s^2, then the reading will be 12.8 m/s^2 along the Y axis,
///     since the controller will now feel a stronger acceleration corresponding
///     to the 9.8 m/s^2 plus the upwards push of 3.0 m/s^2.
///   * If, the controller is accelerated downwards at 5.0 m/s^2, then the
///     reading will now be 4.8 m/s^2 along the Y axis, since the controller
///     will now feel a weaker acceleration (as the acceleration is giving in
///     to gravity).
///   * If you were to give in to gravity completely, letting the controller
///     free fall towards the ground, it will read 0 on all axes, as there
///     will be no force acting on the controller. (Please do not put your
///     controller in a free-fall situation. This is just a theoretical
///     example.)
gvr_vec3f gvr_controller_state_get_accel(const gvr_controller_state* state);

/// Returns whether the user is touching the touchpad.
bool gvr_controller_state_is_touching(const gvr_controller_state* state);

/// If the user is touching the touchpad, this returns the touch position in
/// normalized coordinates, where (0,0) is the top-left of the touchpad
/// and (1,1) is the bottom right. If the user is not touching the touchpad,
/// then this is the position of the last touch.
gvr_vec2f gvr_controller_state_get_touch_pos(const gvr_controller_state* state);

/// Returns true if user just started touching touchpad (this is a transient
/// event:
/// it is true for only one frame after the event).
bool gvr_controller_state_get_touch_down(const gvr_controller_state* state);

/// Returns true if user just stopped touching touchpad (this is a transient
/// event:
/// it is true for only one frame after the event).
bool gvr_controller_state_get_touch_up(const gvr_controller_state* state);

/// Returns true if a recenter operation just ended (this is a transient event:
/// it is true only for one frame after the recenter ended). If this is
/// true then the `orientation` field is already relative to the new center.
bool gvr_controller_state_get_recentered(const gvr_controller_state* state);

/// Returns whether the recenter flow is currently in progress.
///
/// @deprecated Use gvr_controller_state_get_recentered instead.
bool gvr_controller_state_get_recentering(const gvr_controller_state* state);

/// Returns whether the given button is currently pressed.
bool gvr_controller_state_get_button_state(const gvr_controller_state* state,

                                           int32_t button);

/// Returns whether the given button was just pressed (transient).
bool gvr_controller_state_get_button_down(const gvr_controller_state* state,
                                          int32_t button);

/// Returns whether the given button was just released (transient).
bool gvr_controller_state_get_button_up(const gvr_controller_state* state,
                                        int32_t button);

/// Returns the timestamp (nanos) when the last orientation event was received.
int64_t gvr_controller_state_get_last_orientation_timestamp(
    const gvr_controller_state* state);

/// Returns the timestamp (nanos) when the last gyro event was received.
int64_t gvr_controller_state_get_last_gyro_timestamp(
    const gvr_controller_state* state);

/// Returns the timestamp (nanos) when the last accelerometer event was
/// received.
int64_t gvr_controller_state_get_last_accel_timestamp(
    const gvr_controller_state* state);

/// Returns the timestamp (nanos) when the last touch event was received.
int64_t gvr_controller_state_get_last_touch_timestamp(
    const gvr_controller_state* state);

/// Returns the timestamp (nanos) when the last button event was received.
int64_t gvr_controller_state_get_last_button_timestamp(
    const gvr_controller_state* state);

/// @}

#ifdef __cplusplus
}  // extern "C"
#endif


// Convenience C++ wrapper.
#if defined(__cplusplus) && !defined(GVR_NO_CPP_WRAPPER)

#include <memory>

namespace gvr {
/// This is a convenience C++ wrapper for the Controller C API.
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
/// Typical C++ initialization example:
///
///     std::unique_ptr<ControllerApi> controller_api(new ControllerApi);
///
///     // Your GVR context pointer (which can be obtained from GvrLayout)
///     gvr_context* context = .....;  // (get it from GvrLayout)
///
///     // Set up the options:
///     int32_t options = ControllerApi::DefaultOptions();
///
///     // Enable non-default options, if you need them:
///     options |= GVR_CONTROLLER_ENABLE_GYRO;
///
///     // Init the ControllerApi object:
///     bool success = controller_api->Init(options, context);
///     if (!success) {
///       // Handle failure.
///       // Do NOT call other methods (like Resume, etc) if init failed.
///       return;
///     }
///
///     // Resume the ControllerApi (if your app is on the foreground).
///     controller_api->Resume();
///
///     ControllerState state;
///
/// Usage example:
///
///     void DrawFrame() {
///       state.Update(*controller_api);
///       // ... process controller state ...
///     }
///
///     // When your application gets paused:
///     void OnPause() {
///       controller_api->Pause();
///     }
///
///     // When your application gets resumed:
///     void OnResume() {
///       controller_api->Resume();
///     }
///
/// To conserve battery, be sure to call Pause() and Resume() when your app
/// gets paused and resumed, respectively. This will allow the underlying
/// logic to unbind from the VR service and let the controller sleep when
/// no apps are using it.
///
/// THREADING: this class is thread-safe and reentrant after initialized
/// with Init().
class ControllerApi {
 public:
  /// Creates an (uninitialized) ControllerApi object. You must initialize
  /// it by calling Init() before interacting with it.
  ControllerApi() : context_(nullptr) {}

  /// Returns the default controller options.
  static int32_t DefaultOptions() {
    return gvr_controller_get_default_options();
  }

  // Deprecated factory-style create method.
  // TODO(btco): remove this once no one is using it.
  static std::unique_ptr<ControllerApi> Create() {
    return std::unique_ptr<ControllerApi>(new ControllerApi);
  }

  /// Initializes the controller API.
  ///
  /// This method must be called exactly once in the lifetime of this object.
  /// Other methods in this class may only be called after Init() returns true.
  /// Note: this does not cause the ControllerApi to be resumed. You must call
  /// Resume() explicitly in order to start using the controller.
  ///
  /// For more information see gvr_controller_create_and_init().
  ///
  /// @return True if initialization was successful, false if it failed.
  ///     Initialization may fail, for example, because invalid options were
  ///     supplied.
  bool Init(int32_t options, gvr_context* context) {
    context_ = gvr_controller_create_and_init(options, context);
    return context_ != nullptr;
  }

#ifdef __ANDROID__
  /// Overload of Init() with explicit Android context and class loader
  /// (for Android only). For more information, see:
  /// gvr_controller_create_and_init_android().
  bool Init(JNIEnv *env, jobject android_context, jobject class_loader,
            int32_t options, gvr_context* context) {
    context_ = gvr_controller_create_and_init_android(
        env, android_context, class_loader, options, context);
    return context_ != nullptr;
  }
#endif  // #ifdef __ANDROID__

  /// Convenience overload that calls Init without a gvr_context.
  // TODO(btco): remove once it is no longer being used.
  bool Init(int32_t options) {
    return Init(options, nullptr);
  }

  /// Pauses the controller.
  /// For more information, see gvr_controller_pause().
  void Pause() {
    gvr_controller_pause(context_);
  }

  /// Resumes the controller.
  /// For more information, see gvr_controller_resume().
  void Resume() {
    gvr_controller_resume(context_);
  }

  /// Destroys this ControllerApi instance.
  ~ControllerApi() {
    if (context_) gvr_controller_destroy(&context_);
  }

  /// Convenience functions to convert enums to strings.
  /// For more information, see the corresponding functions in the C API.
  static const char* ToString(ControllerApiStatus status) {
    return gvr_controller_api_status_to_string(status);
  }

  static const char* ToString(ControllerConnectionState state) {
    return gvr_controller_connection_state_to_string(state);
  }

  static const char* ToString(ControllerButton button) {
    return gvr_controller_button_to_string(button);
  }

  /// @name Wrapper manipulation
  /// @{
  /// Creates a C++ wrapper for a C object and takes ownership.
  explicit ControllerApi(gvr_controller_context* context)
      : context_(context) {}

  /// Returns the wrapped C object. Does not affect ownership.
  gvr_controller_context* cobj() { return context_; }
  const gvr_controller_context* cobj() const { return context_; }

  /// Returns the wrapped C object and transfers its ownership to the caller.
  /// The wrapper becomes invalid and should not be used.
  gvr_controller_context* release() {
    auto result = context_;
    context_ = nullptr;
    return result;
  }
  /// @}

 protected:
  gvr_controller_context* context_;

 private:
  friend class ControllerState;

  // Disallow copy and assign:
  ControllerApi(const ControllerApi&);
  void operator=(const ControllerApi&);
};

/// Convenience C++ wrapper for the opaque gvr_controller_state type. See the
/// gvr_controller_state functions for more information.
class ControllerState {
 public:
  ControllerState() : state_(gvr_controller_state_create()) {}

  ~ControllerState() {
    if (state_) gvr_controller_state_destroy(&state_);
  }

  /// For more information, see gvr_controller_state_update().
  void Update(const ControllerApi& api) {
    gvr_controller_state_update(api.context_, 0, state_);
  }

  /// For more information, see gvr_controller_state_update().
  void Update(const ControllerApi& api, int32_t flags) {
    gvr_controller_state_update(api.context_, flags, state_);
  }

  /// For more information, see gvr_controller_state_get_api_status().
  ControllerApiStatus GetApiStatus() const {
    return static_cast<ControllerApiStatus>(
        gvr_controller_state_get_api_status(state_));
  }

  /// For more information, see gvr_controller_state_get_connection_state().
  ControllerConnectionState GetConnectionState() const {
    return static_cast<ControllerConnectionState>(
        gvr_controller_state_get_connection_state(state_));
  }

  /// For more information, see gvr_controller_state_get_orientation().
  gvr_quatf GetOrientation() const {
    return gvr_controller_state_get_orientation(state_);
  }

  /// For more information, see gvr_controller_state_get_gyro().
  gvr_vec3f GetGyro() const { return gvr_controller_state_get_gyro(state_); }

  /// For more information, see gvr_controller_state_get_accel().
  gvr_vec3f GetAccel() const { return gvr_controller_state_get_accel(state_); }

  /// For more information, see gvr_controller_state_is_touching().
  bool IsTouching() const { return gvr_controller_state_is_touching(state_); }

  /// For more information, see gvr_controller_state_get_touch_pos().
  gvr_vec2f GetTouchPos() const {
    return gvr_controller_state_get_touch_pos(state_);
  }

  /// For more information, see gvr_controller_state_get_touch_down().
  bool GetTouchDown() const {
    return gvr_controller_state_get_touch_down(state_);
  }

  /// For more information, see gvr_controller_state_get_touch_up().
  bool GetTouchUp() const { return gvr_controller_state_get_touch_up(state_); }

  /// For more information, see gvr_controller_state_get_recentered().
  bool GetRecentered() const {
    return gvr_controller_state_get_recentered(state_);
  }

  /// For more information, see gvr_controller_state_get_recentering().
  bool GetRecentering() const {
    return gvr_controller_state_get_recentering(state_);
  }

  /// For more information, see gvr_controller_state_get_button_state().
  bool GetButtonState(ControllerButton button) const {
    return gvr_controller_state_get_button_state(state_, button);
  }

  /// For more information, see gvr_controller_state_get_button_down().
  bool GetButtonDown(ControllerButton button) const {
    return gvr_controller_state_get_button_down(state_, button);
  }

  /// For more information, see gvr_controller_state_get_button_up().
  bool GetButtonUp(ControllerButton button) const {
    return gvr_controller_state_get_button_up(state_, button);
  }

  /// For more information, see
  /// gvr_controller_state_get_last_orientation_timestamp().
  int64_t GetLastOrientationTimestamp() const {
    return gvr_controller_state_get_last_orientation_timestamp(state_);
  }

  /// For more information, see gvr_controller_state_get_last_gyro_timestamp().
  int64_t GetLastGyroTimestamp() const {
    return gvr_controller_state_get_last_gyro_timestamp(state_);
  }

  /// For more information, see gvr_controller_state_get_last_accel_timestamp().
  int64_t GetLastAccelTimestamp() const {
    return gvr_controller_state_get_last_accel_timestamp(state_);
  }

  /// For more information, see gvr_controller_state_get_last_touch_timestamp().
  int64_t GetLastTouchTimestamp() const {
    return gvr_controller_state_get_last_touch_timestamp(state_);
  }

  /// For more information, see
  /// gvr_controller_state_get_last_button_timestamp().
  int64_t GetLastButtonTimestamp() const {
    return gvr_controller_state_get_last_button_timestamp(state_);
  }

  /// @name Wrapper manipulation
  /// @{
  /// Creates a C++ wrapper for a C object and takes ownership.
  explicit ControllerState(gvr_controller_state* state) : state_(state) {}

  /// Returns the wrapped C object. Does not affect ownership.
  gvr_controller_state* cobj() { return state_; }
  const gvr_controller_state* cobj() const { return state_; }

  /// Returns the wrapped C object and transfers its ownership to the caller.
  /// The wrapper becomes invalid and should not be used.
  gvr_controller_state* release() {
    auto result = state_;
    state_ = nullptr;
    return result;
  }
  /// @}

 private:
  gvr_controller_state* state_;
};

}  // namespace gvr
#endif  // #if defined(__cplusplus) && !defined(GVR_NO_CPP_WRAPPER)

#endif  // VR_GVR_CAPI_INCLUDE_GVR_CONTROLLER_H_
