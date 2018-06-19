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

#ifndef VR_GVR_CAPI_INCLUDE_GVR_H_
#define VR_GVR_CAPI_INCLUDE_GVR_H_

#ifdef __ANDROID__
#include <jni.h>
#endif

#include <stdint.h>
#include <stdlib.h>

#if defined(__cplusplus) && !defined(GVR_NO_CPP_WRAPPER)
#include <array>
#include <memory>
#include <vector>
#endif

#include "../GoogleVR/vr/gvr/capi/include/gvr_types.h"

#ifdef __cplusplus
extern "C" {
#endif

/// @defgroup base Google VR Base C API
/// @brief This is the Google VR C API. It supports clients writing VR
/// experiences for head mounted displays that consist of a mobile phone and a
/// VR viewer.
///
/// Example API usage:
///
///     #ifdef __ANDROID__
///     // On Android, the gvr_context should almost always be obtained from
///     // the Java GvrLayout object via
///     // GvrLayout.getGvrApi().getNativeGvrContext().
///     gvr_context* gvr = ...;
///     #else
///     gvr_context* gvr = gvr_create();
///     #endif
///
///     gvr_initialize_gl(gvr);
///
///     gvr_buffer_viewport_list* viewport_list =
///         gvr_buffer_viewport_list_create(gvr);
///     gvr_get_recommended_buffer_viewports(gvr, viewport_list);
///     gvr_buffer_viewport* left_eye_vp = gvr_buffer_viewport_create(gvr);
///     gvr_buffer_viewport* right_eye_vp = gvr_buffer_viewport_create(gvr);
///     gvr_buffer_viewport_list_get_item(viewport_list, 0, left_eye_vp);
///     gvr_buffer_viewport_list_get_item(viewport_list, 1, right_eye_vp);
///
///     while (client_app_should_render) {
///       // A client app should be ready for the render target size to change
///       // whenever a new QR code is scanned, or a new viewer is paired.
///       gvr_sizei render_target_size =
///           gvr_get_maximum_effective_render_target_size(gvr);
///       // The maximum effective render target size can be very large, most
///       // applications need to scale down to compensate.
///       render_target_size.width /= 2;
///       render_target_size.height /= 2;
///       gvr_swap_chain_resize_buffer(swap_chain, 0, render_target_size);
///
///       // This function will depend on your render loop's implementation.
///       gvr_clock_time_point next_vsync = AppGetNextVsyncTime();
///
///       const gvr_mat4f head_view =
///           gvr_get_head_space_from_start_space_rotation(gvr, next_vsync);
///       const gvr_mat4f left_eye_view = MatrixMultiply(
///           gvr_get_eye_from_head_matrix(gvr, GVR_LEFT_EYE), head_view);
///       const gvr::Mat4f right_eye_view = MatrixMultiply(
///           gvr_get_eye_from_head_matrix(gvr, GVR_RIGHT_EYE), head_view);
///
///       // Insert client rendering code here.
///
///       AppSetRenderTarget(offscreen_texture_id);
///
///       AppDoSomeRenderingForEye(
///           gvr_buffer_viewport_get_source_uv(left_eye_view),
///           left_eye_matrix);
///       AppDoSomeRenderingForEye(
///           gvr_buffer_viewport_get_source_uv(right_eye_view),
///           right_eye_matrix);
///       AppSetRenderTarget(primary_display);
///
///       gvr_frame_submit(&frame, viewport_list, head_matrix);
///     }
///
///     // Cleanup memory.
///     gvr_buffer_viewport_list_destroy(&viewport_list);
///     gvr_buffer_viewport_destroy(&left_eye_vp);
///     gvr_buffer_viewport_destroy(&right_eye_vp);
///
///     #ifdef __ANDROID__
///     // On Android, The Java GvrLayout owns the gvr_context.
///     #else
///     gvr_destroy(gvr);
///     #endif
///
/// Head tracking is enabled by default, and will begin as soon as the
/// gvr_context is created. The client should call gvr_pause_tracking() and
/// gvr_resume_tracking() when the app is paused and resumed, respectively.
///
/// Note: Unless otherwise noted, the functions in this API may not be
/// thread-safe with respect to the gvr_context, and it is up the caller to use
/// the API in a thread-safe manner.
///
/// @{

/// Creates a new gvr instance.
///
/// The instance must remain valid as long as any GVR object is in use. When
/// the application no longer needs to use the GVR SDK, call gvr_destroy().
///
///
/// On Android, the gvr_context should *almost always* be obtained from the Java
/// GvrLayout object, rather than explicitly created here. The GvrLayout should
/// live in the app's View hierarchy, and its use is required to ensure
/// consistent behavior across all varieties of GVR-compatible viewers. See
/// the Java GvrLayout and GvrApi documentation for more details.
///
#ifdef __ANDROID__
/// @param env The JNIEnv associated with the current thread.
/// @param app_context The Android application context. This must be the
///     application context, NOT an Activity context (Note: from any Android
///     Activity in your app, you can call getApplicationContext() to
///     retrieve the application context).
/// @param class_loader The class loader to use when loading Java classes.
///     This must be your app's main class loader (usually accessible through
///     activity.getClassLoader() on any of your Activities).
///
/// @return Pointer to the created gvr instance, NULL on failure.
gvr_context* gvr_create(JNIEnv* env, jobject app_context, jobject class_loader);
#else
/// @return Pointer to the created gvr instance, NULL on failure.
gvr_context* gvr_create();
#endif  // #ifdef __ANDROID__

/// Gets the current GVR runtime version.
///
/// Note: This runtime version may differ from the version against which the
/// client app is compiled, as defined by the semantic version components in
/// gvr_version.h.
///
/// @return The version as a gvr_version.
gvr_version gvr_get_version();

/// Gets a string representation of the current GVR runtime version. This is of
/// the form "MAJOR.MINOR.PATCH".
///
/// Note: This runtime version may differ from the version against which the
/// client app is compiled, as defined in gvr_version.h by
/// GVR_SDK_VERSION_STRING.
///
/// @return The version as a static char pointer.
const char* gvr_get_version_string();

/// Gets the current GVR error code, or GVR_ERROR_NONE if there is no error.
/// This function doesn't clear the error code; see gvr_clear_error().
///
/// @param gvr Pointer to the gvr instance.
/// @return The current gvr_error code, or GVR_ERROR_NONE if no error has
///    occurred.
int32_t gvr_get_error(gvr_context* gvr);

/// Clears the current GVR error code, and returns the error code that was
/// cleared.
///
/// @param gvr Pointer to the gvr instance.
/// @return The gvr_error code that was cleared by this function, or
/// GVR_ERROR_NONE if no error has occurred.
int32_t gvr_clear_error(gvr_context* gvr);

/// Gets a human-readable string representing the given error code.
///
/// @param error_code The gvr_error code.
/// @return A human-readable string representing the error code.
const char* gvr_get_error_string(int32_t error_code);

/// Returns an opaque struct containing information about user preferences.
///
/// The returned struct will remain valid as long as the context is valid.
/// The returned struct may be updated when the user changes their preferences,
/// so this function only needs to be called once, and calling it multiple
/// times will return the same object each time.
///
/// @param gvr Pointer to the gvr instance.
/// @return An opaque struct containing information about user preferences.
const gvr_user_prefs* gvr_get_user_prefs(gvr_context* gvr);

/// Returns the controller handedness of the given gvr_user_prefs struct.
///
/// @param user_prefs Pointer to the gvr_user_prefs object returned by
///     gvr_get_user_prefs.
/// @return Either GVR_CONTROLLER_RIGHT_HANDED or GVR_CONTROLLER_LEFT_HANDED
///     depending on which hand the user holds the controller in.
int32_t gvr_user_prefs_get_controller_handedness(
    const gvr_user_prefs* user_prefs);

/// Destroys a gvr_context instance.  The parameter will be nulled by this
/// operation.  Once this function is called, the behavior of any subsequent
/// call to a GVR SDK function that references objects created from this
/// context is undefined.
///
/// @param gvr Pointer to a pointer to the gvr instance to be destroyed and
///     nulled.
void gvr_destroy(gvr_context** gvr);

/// Initializes necessary GL-related objects and uses the current thread and
/// GL context for rendering. Please make sure that a valid GL context is
/// available when this function is called.
///
/// @param gvr Pointer to the gvr instance to be initialized.
void gvr_initialize_gl(gvr_context* gvr);

/// Gets whether asynchronous reprojection is currently enabled.
///
/// If enabled, frames will be collected by the rendering system and
/// asynchronously re-projected in sync with the scanout of the display. This
/// feature may not be available on every platform, and requires a
/// high-priority render thread with special extensions to function properly.
///
/// Note: On Android, this feature can be enabled solely via the GvrLayout Java
/// instance which (indirectly) owns this gvr_context. The corresponding
/// method call is GvrLayout.setAsyncReprojectionEnabled().
///
/// @param gvr Pointer to the gvr instance.
/// @return Whether async reprojection is enabled. Defaults to false.
bool gvr_get_async_reprojection_enabled(const gvr_context* gvr);

/// Gets the recommended buffer viewport configuration, populating a previously
/// allocated gvr_buffer_viewport_list object. The updated values include the
/// per-eye recommended viewport and field of view for the target.
///
/// When the recommended viewports are used for distortion rendering, this
/// method should always be called after calling refresh_viewer_profile(). That
/// will ensure that the populated viewports reflect the currently paired
/// viewer.
///
/// @param gvr Pointer to the gvr instance from which to get the viewports.
/// @param viewport_list Pointer to a previously allocated viewport list. This
///     will be populated with the recommended buffer viewports and resized if
///     necessary.
void gvr_get_recommended_buffer_viewports(
    const gvr_context* gvr, gvr_buffer_viewport_list* viewport_list);

/// Gets the screen (non-distorted) buffer viewport configuration, populating a
/// previously allocated gvr_buffer_viewport_list object. The updated values
/// include the per-eye recommended viewport and field of view for the target.
///
/// @param gvr Pointer to the gvr instance from which to get the viewports.
/// @param viewport_list Pointer to a previously allocated viewport list. This
///     will be populated with the screen buffer viewports and resized if
///     necessary.
void gvr_get_screen_buffer_viewports(const gvr_context* gvr,
                                     gvr_buffer_viewport_list* viewport_list);

/// Returns the maximum effective size for the client's render target, given the
/// parameters of the head mounted device selected. At this resolution, we have
/// a 1:1 ratio between source pixels and screen pixels in the most magnified
/// region of the screen. Applications should rarely, if ever, need to render
/// to a larger target, as it will simply result in sampling artifacts.
///
/// Note that this is probably too large for most applications to use as a
/// render target size. Applications should scale this value to be appropriate
/// to their graphical load.
///
/// @param gvr Pointer to the gvr instance from which to get the size.
///
/// @return Maximum effective size for the target render target.
gvr_sizei gvr_get_maximum_effective_render_target_size(const gvr_context* gvr);

/// Returns a non-distorted size for the screen, given the parameters
/// of the phone and/or the head mounted device selected.
///
/// @param gvr Pointer to the gvr instance from which to get the size.
///
/// @return Screen (non-distorted) size for the render target.
gvr_sizei gvr_get_screen_target_size(const gvr_context* gvr);

// Sets the size of the underlying render surface.
//
// By default, it is assumed that the display size matches the surface
// size. If that is the case for the client app, this method need never be
// called. However, in certain cases (e.g., hardware scaling), this will not
// always hold, in which case the distortion pass must be informed of the
// custom surface size.
//
// Note that the caller is responsible for resizing any BufferSpec objects
// created before this function is called. Otherwise there will be rendering
// artifacts, such as edges appearing pixelated. This function will change the
// result of get_maximum_effective_render_target_size(), so that function can be
// used to compute the appropriate size for buffers.
//
// @param gvr Pointer to the gvr_context instance.
// @param surface_size_pixels The size in pixels of the display surface. If
//     non-empty, this will be used in conjunction with the current display to
//     perform properly scaled distortion. If empty, it is assumed that the
//     rendering surface dimensions match that of the active display.
void gvr_set_surface_size(gvr_context* gvr, gvr_sizei surface_size_pixels);

/// Performs postprocessing, including lens distortion, on the contents of the
/// passed texture and shows the result on the screen. Lens distortion is
/// determined by the parameters of the viewer encoded in its QR code. The
/// passed texture is not modified.
///
/// If the application does not call gvr_initialize_gl() before calling this
/// function, the results are undefined.
///
/// @deprecated This function exists only to support legacy rendering pathways
///     for Cardboard devices. It is incompatible with the low-latency
///     experiences supported by async reprojection. Use the swap chain API
///     instead.
///
/// @param gvr Pointer to the gvr instance which will do the distortion.
/// @param texture_id The OpenGL ID of the texture that contains the next frame
///     to be displayed.
/// @param viewport_list Rendering parameters.
/// @param head_space_from_start_space This parameter is ignored.
/// @param target_presentation_time This parameter is ignored.
void gvr_distort_to_screen(gvr_context* gvr, int32_t texture_id,
                           const gvr_buffer_viewport_list* viewport_list,
                           gvr_mat4f head_space_from_start_space,
                           gvr_clock_time_point target_presentation_time);
/// @}

/////////////////////////////////////////////////////////////////////////////
// Viewports and viewport lists
/////////////////////////////////////////////////////////////////////////////
/// @defgroup viewport Viewports and viewport lists
/// @brief Objects to define the mapping between the application's rendering
///     output and the user's field of view.
/// @{

/// Creates a gvr_buffer_viewport instance.
gvr_buffer_viewport* gvr_buffer_viewport_create(gvr_context* gvr);

/// Frees a gvr_buffer_viewport instance and clears the pointer.
void gvr_buffer_viewport_destroy(gvr_buffer_viewport** viewport);

/// Gets the UV coordinates specifying where the output buffer is sampled.
///
/// @param viewport The buffer viewport.
/// @return UV coordinates as a rectangle.
gvr_rectf gvr_buffer_viewport_get_source_uv(
    const gvr_buffer_viewport* viewport);

/// Sets the UV coordinates specifying where the output buffer should be
/// sampled when compositing the final distorted image.
///
/// @param viewport The buffer viewport.
/// @param uv The new UV coordinates for sampling. The coordinates must be
///     valid, that is, left <= right and bottom <= top. Otherwise an empty
///     source region is set, which will result in no output for this viewport.
void gvr_buffer_viewport_set_source_uv(gvr_buffer_viewport* viewport,
                                       gvr_rectf uv);

/// Retrieves the field of view for the referenced buffer region.
///
/// @param viewport The buffer viewport.
/// @return The field of view of the rendered image, in degrees.
gvr_rectf gvr_buffer_viewport_get_source_fov(
    const gvr_buffer_viewport* viewport);

/// Sets the field of view for the referenced buffer region.
///
/// @param viewport The buffer viewport.
/// @param fov The field of view to use when compositing the rendered image.
void gvr_buffer_viewport_set_source_fov(gvr_buffer_viewport* viewport,
                                        gvr_rectf fov);

/// Gets the target logical eye for the specified viewport.
///
/// @param viewport The buffer viewport.
/// @return Index of the target logical eye for this viewport.
int32_t gvr_buffer_viewport_get_target_eye(const gvr_buffer_viewport* viewport);

/// Sets the target logical eye for the specified viewport.
///
/// @param viewport The buffer viewport.
/// @param index Index of the target logical eye.
void gvr_buffer_viewport_set_target_eye(gvr_buffer_viewport* viewport,
                                        int32_t index);

/// Gets the index of the source buffer from which the viewport reads its
/// undistorted pixels.
///
/// @param viewport The buffer viewport.
/// @return Index of the source buffer. This corresponds to the index in the
///     list of buffer specs that was passed to gvr_swap_chain_create().
int32_t gvr_buffer_viewport_get_source_buffer_index(
    const gvr_buffer_viewport* viewport);

/// Sets the buffer from which the viewport reads its undistorted pixels.
///
/// @param viewport The buffer viewport.
/// @param buffer_index The index of the source buffer. This corresponds to the
///     index in the list of buffer specs that was passed to
///     gvr_swap_chain_create().
void gvr_buffer_viewport_set_source_buffer_index(
    gvr_buffer_viewport* viewport, int32_t buffer_index);

/// Gets the ID of the externally-managed Surface texture from which this
/// viewport reads undistored pixels.
///
/// @param viewport The buffer viewport.
/// @return ID of the externally-managed Surface of undistorted pixels.
int32_t gvr_buffer_viewport_get_external_surface_id(
    const gvr_buffer_viewport* viewport);

/// Sets the ID of the externally-managed Surface texture from which this
/// viewport reads. The ID is issued by the SurfaceTextureManager. If the ID
/// is not -1, the distortion renderer will sample color pixels from the
/// external surface at ID, using the source buffer for texture coords.
///
/// @param viewport The buffer viewport.
/// @param external_surface_id The ID of the surface to read from.
void gvr_buffer_viewport_set_external_surface_id(
    gvr_buffer_viewport* viewport, int32_t external_surface_id);

/// Gets the type of reprojection to perform on the specified viewport.
///
/// @param viewport The buffer viewport.
/// @return Type of reprojection that is applied to the viewport.
int32_t gvr_buffer_viewport_get_reprojection(
    const gvr_buffer_viewport* viewport);

/// Sets the type of reprojection to perform on the specified viewport.
/// Viewports that display world content should use full reprojection.
/// Viewports that display head-locked UI should disable reprojection to avoid
/// excessive judder. The default is to perform full reprojection.
///
/// @param viewport The buffer viewport.
/// @param reprojection Type of reprojection that will be applied to the passed
///     viewport.
void gvr_buffer_viewport_set_reprojection(gvr_buffer_viewport* viewport,
                                          int32_t reprojection);

/// Compares two gvr_buffer_viewport instances and returns true if they specify
/// the same view mapping.
///
/// @param a Instance of a buffer viewport.
/// @param b Another instance of a buffer viewport.
/// @return True if the passed viewports are the same.
bool gvr_buffer_viewport_equal(const gvr_buffer_viewport* a,
                               const gvr_buffer_viewport* b);

/// Creates a new, empty list of viewports. The viewport list defines how the
/// application's rendering output should be transformed into the stabilized,
/// lens-distorted image that is sent to the screen.
///
/// The caller should populate the returned viewport using one of:
///   - gvr_get_recommended_buffer_viewports()
///   - gvr_get_screen_buffer_viewports()
///   - gvr_buffer_viewport_list_set_item()
///
/// @param gvr Pointer the gvr instance from which to allocate the viewport
/// list.
/// @return Pointer to an allocated gvr_buffer_viewport_list object. The caller
//      is responsible for calling gvr_buffer_viewport_list_destroy() on the
///     returned object when it is no longer needed.
gvr_buffer_viewport_list* gvr_buffer_viewport_list_create(
    const gvr_context* gvr);

/// Destroys a gvr_buffer_viewport_list instance. The parameter will be nulled
/// by this operation.
///
/// @param viewport_list Pointer to a pointer to the viewport list instance to
///     be destroyed and nulled.
void gvr_buffer_viewport_list_destroy(gvr_buffer_viewport_list** viewport_list);

/// Returns the size of the given viewport list.
///
/// @param viewport_list Pointer to a viewport list.
/// @return The number of entries in the viewport list.
size_t gvr_buffer_viewport_list_get_size(
    const gvr_buffer_viewport_list* viewport_list);

/// Retrieve a buffer viewport entry from a list.
///
/// @param viewport_list Pointer to the previously allocated viewport list.
/// @param index Zero-based index of the viewport entry to query. Must be
///    smaller than the list size.
/// @param viewport The buffer viewport structure that will be populated with
///    retrieved data.
void gvr_buffer_viewport_list_get_item(
    const gvr_buffer_viewport_list* viewport_list, size_t index,
    gvr_buffer_viewport* viewport);

/// Update an element of the viewport list or append a new one at the end.
///
/// @param viewport_list Pointer to a previously allocated viewport list.
/// @param index Index of the buffer viewport entry to update. If the
///     `viewport_list` size is equal to the index, a new viewport entry will be
///     added. The `viewport_list` size must *not* be less than the index value.
/// @param viewport A pointer to the buffer viewport object.
void gvr_buffer_viewport_list_set_item(gvr_buffer_viewport_list* viewport_list,
                                       size_t index,
                                       const gvr_buffer_viewport* viewport);

/// @}

/////////////////////////////////////////////////////////////////////////////
// Swapchains and frames
/////////////////////////////////////////////////////////////////////////////
/// @defgroup swap_chain Swap chains and frames
/// @brief Functions to create a swap chain, manipulate it and submit frames
///     for lens distortion and presentation on the screen.
/// @{

/// Creates a default buffer specification.
gvr_buffer_spec* gvr_buffer_spec_create(gvr_context* gvr);

/// Destroy the buffer specification and null the pointer.
void gvr_buffer_spec_destroy(gvr_buffer_spec** spec);

/// Gets the size of the buffer to be created.
///
/// @param spec Buffer specification.
/// @return Size of the pixel buffer. The default is equal to the recommended
///     render target size at the time when the specification was created.
gvr_sizei gvr_buffer_spec_get_size(const gvr_buffer_spec* spec);

/// Sets the size of the buffer to be created.
///
/// @param spec Buffer specification.
/// @param size The size. Width and height must both be greater than zero.
///     Otherwise, the application is aborted.
void gvr_buffer_spec_set_size(gvr_buffer_spec* spec, gvr_sizei size);

/// Gets the number of samples per pixel in the buffer to be created.
///
/// @param spec Buffer specification.
/// @return Value >= 1 giving the number of samples. 1 means multisampling is
///     disabled. Negative values and 0 are never returned.
int32_t gvr_buffer_spec_get_samples(const gvr_buffer_spec* spec);

/// Sets the number of samples per pixel in the buffer to be created.
///
/// @param spec Buffer specification.
/// @param num_samples The number of samples. Negative values are an error.
///     The values 0 and 1 are treated identically and indicate that
//      multisampling should be disabled.
void gvr_buffer_spec_set_samples(gvr_buffer_spec* spec, int32_t num_samples);

/// Sets the color format for the buffer to be created. Default format is
/// GVR_COLOR_FORMAT_RGBA_8888.
///
/// @param spec Buffer specification.
/// @param color_format The color format for the buffer. Valid formats are in
///     the gvr_color_format_type enum.
void gvr_buffer_spec_set_color_format(gvr_buffer_spec* spec,
                                      int32_t color_format);

/// Sets the depth and stencil format for the buffer to be created. Currently,
/// only packed stencil formats are supported. Default format is
/// GVR_DEPTH_STENCIL_FORMAT_DEPTH_16.
///
/// @param spec Buffer specification.
/// @param depth_stencil_format The depth and stencil format for the buffer.
///     Valid formats are in the gvr_depth_stencil_format_type enum.
void gvr_buffer_spec_set_depth_stencil_format(gvr_buffer_spec* spec,
                                              int32_t depth_stencil_format);

/// Creates a swap chain from the given buffer specifications.
/// This is a potentially time-consuming operation. All frames within the
/// swapchain will be allocated. Once rendering is stopped, call
/// gvr_swap_chain_destroy() to free GPU resources. The passed gvr_context must
/// not be destroyed until then.
///
/// Note: Currently, swap chains only support more than one buffer when
/// asynchronous reprojection is enabled. This restriction will be lifted in a
/// future release.
///
/// @param gvr GVR instance for which a swap chain will be created.
/// @param buffers Array of pixel buffer specifications. Each frame in the
///     swap chain will be composed of these buffers.
/// @param count Number of buffer specifications in the array.
/// @return Opaque handle to the newly created swap chain.
gvr_swap_chain* gvr_swap_chain_create(gvr_context* gvr,
                                      const gvr_buffer_spec** buffers,
                                      int32_t count);

/// Destroys the swap chain and nulls the pointer.
void gvr_swap_chain_destroy(gvr_swap_chain** swap_chain);

/// Gets the number of buffers in each frame of the swap chain.
int32_t gvr_swap_chain_get_buffer_count(const gvr_swap_chain* swap_chain);

/// Retrieves the size of the specified pixel buffer. Note that if the buffer
/// was resized while the current frame was acquired, the return value will be
/// different than the value obtained from the equivalent function for the
/// current frame.
///
/// @param swap_chain The swap chain.
/// @param index Index of the pixel buffer.
/// @return Size of the specified pixel buffer in frames that will be returned
///     from gvr_swap_chain_acquire_frame().
gvr_sizei gvr_swap_chain_get_buffer_size(gvr_swap_chain* swap_chain,
                                         int32_t index);

/// Resizes the specified pixel buffer to the given size. The frames are resized
/// when they are unused, so the currently acquired frame will not be resized
/// immediately.
///
/// @param swap_chain The swap chain.
/// @param index Index of the pixel buffer to resize.
/// @param size New size for the specified pixel buffer.
void gvr_swap_chain_resize_buffer(gvr_swap_chain* swap_chain, int32_t index,
                                  gvr_sizei size);

/// Acquires a frame from the swap chain for rendering. Buffers that are part of
/// the frame can then be bound with gvr_frame_bind_buffer(). Once the frame
/// is finished and all its constituent buffers are ready, call
/// gvr_frame_submit() to display it while applying lens distortion.
///
/// @param swap_chain The swap chain.
/// @return Handle to the acquired frame. NULL if the swap chain is invalid,
///     or if acquire has already been called on this swap chain.
gvr_frame* gvr_swap_chain_acquire_frame(gvr_swap_chain* swap_chain);

/// Binds a pixel buffer that is part of the frame to the OpenGL framebuffer.
///
/// @param frame Frame handle acquired from the swap chain.
/// @param index Index of the pixel buffer to bind. This corresponds to the
///     index in the buffer spec list that was passed to
///     gvr_swap_chain_create().
void gvr_frame_bind_buffer(gvr_frame* frame, int32_t index);

/// Unbinds any buffers bound from this frame and binds the default OpenGL
/// framebuffer.
void gvr_frame_unbind(gvr_frame* frame);

/// Returns the dimensions of the pixel buffer with the specified index. Note
/// that a frame that was acquired before resizing a swap chain buffer will not
/// be resized until it is submitted to the swap chain.
///
/// @param frame Frame handle.
/// @param index Index of the pixel buffer to inspect.
/// @return Dimensions of the specified pixel buffer.
gvr_sizei gvr_frame_get_buffer_size(const gvr_frame* frame, int32_t index);

/// Gets the name (ID) of the framebuffer object associated with the specified
/// buffer. The OpenGL state is not modified.
///
/// @param frame Frame handle.
/// @param index Index of a pixel buffer.
/// @return OpenGL object name (ID) of a framebuffer object which can be used
///     to render into the buffer. The ID is valid only until the frame is
///     submitted.
int32_t gvr_frame_get_framebuffer_object(const gvr_frame* frame, int32_t index);

/// Submits the frame for distortion and display on the screen. The passed
/// pointer is nulled to prevent reuse.
///
/// @param frame The frame to submit.
/// @param list Buffer view configuration to be used for this frame.
/// @param head_space_from_start_space Transform from start space (space with
///     head at the origin at last tracking reset) to head space (space with
///     head at the origin and axes aligned to the view vector).
void gvr_frame_submit(gvr_frame** frame, const gvr_buffer_viewport_list* list,
                      gvr_mat4f head_space_from_start_space);

/// Resets the OpenGL framebuffer binding to what it was at the time the
/// passed gvr_context was created.
void gvr_bind_default_framebuffer(gvr_context* gvr);

/// @}

/////////////////////////////////////////////////////////////////////////////
// Head tracking
/////////////////////////////////////////////////////////////////////////////
/// @defgroup Headtracking Head tracking
/// @brief Functions for managing head tracking.
/// @{

/// Gets the current monotonic system time.
///
/// @return The current monotonic system time.
gvr_clock_time_point gvr_get_time_point_now();

/// Gets the rotation from start space to head space.  The head space is a
/// space where the head is at the origin and faces the -Z direction.
///
/// @param gvr Pointer to the gvr instance from which to get the pose.
/// @param time The time at which to get the head pose. The time should be in
///     the future. If the time is not in the future, it will be clamped to now.
/// @return A matrix representation of the rotation from start space (the space
///     where the head was last reset) to head space (the space with the head
///     at the origin, and the axes aligned to the view vector).
gvr_mat4f gvr_get_head_space_from_start_space_rotation(
    const gvr_context* gvr, const gvr_clock_time_point time);

/// Applies a simple neck model translation based on the rotation of the
/// provided head pose.
///
/// Note: Neck model application may not be appropriate for all tracking
/// scenarios, e.g., when tracking is non-biological.
///
/// @param gvr Pointer to the context instance from which the pose was obtained.
/// @param head_rotation_in_start_space The head rotation as returned by
///     gvr_get_head_space_from_start_space_rotation().
/// @param factor A scaling factor for the neck model offset, clamped from 0 to
///     1. This should be 1 for most scenarios, while 0 will effectively disable
///     neck model application. This value can be animated to smoothly
///     interpolate between alternative (client-defined) neck models.
/// @return The new head pose with the neck model applied.
gvr_mat4f gvr_apply_neck_model(const gvr_context* gvr,
                               gvr_mat4f head_space_from_start_space_rotation,
                               float factor);

/// Pauses head tracking, disables all sensors (to save power).
///
/// @param gvr Pointer to the gvr instance for which tracking will be paused and
///     sensors disabled.
void gvr_pause_tracking(gvr_context* gvr);

/// Resumes head tracking, re-enables all sensors.
///
/// @param gvr Pointer to the gvr instance for which tracking will be resumed.
void gvr_resume_tracking(gvr_context* gvr);

/// Resets head tracking.
///
/// This API call is deprecated. Use gvr_recenter_tracking instead, which
/// accomplishes the same effects but avoids the undesirable side-effects of
/// a full reset (temporary loss of tracking quality).
///
/// Only to be used by Cardboard apps. Daydream apps must not call this. On the
/// Daydream platform, recentering is handled automatically and should never
/// be triggered programatically by applications. Hybrid apps that support both
/// Cardboard and Daydream must only call this function when in Cardboard mode
/// (that is, when the phone is paired with a Cardboard viewer), never in
/// Daydream mode.
///
/// @param gvr Pointer to the gvr instance for which tracking will be reseted.
/// @deprecated Calls to this method can be safely replaced by calls to
//    gvr_recenter_tracking.
void gvr_reset_tracking(gvr_context* gvr);

/// Recenters the head orientation (resets the yaw to zero, leaving pitch and
/// roll unmodified).
///
/// Only to be used by Cardboard apps. Daydream apps must not call this. On the
/// Daydream platform, recentering is handled automatically and should never
/// be triggered programatically by applications. Hybrid apps that support both
/// Cardboard and Daydream must only call this function when in Cardboard mode
/// (that is, when the phone is paired with a Cardboard viewer), never in
/// Daydream mode.
///
/// @param gvr Pointer to the gvr instance for which tracking will be
///     recentered.
void gvr_recenter_tracking(gvr_context* gvr);

/// @}


/////////////////////////////////////////////////////////////////////////////
// Head mounted display.
/////////////////////////////////////////////////////////////////////////////
/// @defgroup HMD Head Mounted Display
/// @brief Functions for managing viewer information.
/// @{

/// Sets the default viewer profile specified by viewer_profile_uri.
/// The viewer_profile_uri that is passed in will be ignored if a valid
/// viewer profile has already been stored on the device that the app
/// is running on.
///
/// Note: This function has the potential of blocking for up to 30 seconds for
/// each redirect if a shortened URI is passed in as argument. It will try to
/// unroll the shortened URI for a maximum number of 5 times if the redirect
/// continues. In that case, it is recommended to create a separate thread to
/// call this function so that other tasks like rendering will not be blocked
/// on this. The blocking can be avoided if a standard URI is passed in.
///
/// @param gvr Pointer to the gvr instance which to set the profile on.
/// @param viewer_profile_uri A string that contains either the shortened URI or
///     the standard URI representing the viewer profile that the app should be
///     using. If the valid viewer profile can be found on the device, the URI
///     that is passed in will be ignored and nothing will happen. Otherwise,
///     gvr will look for the viewer profile specified by viewer_profile_uri,
///     and it will be stored if found. Also, the values will be applied to gvr.
///     A valid standard URI can be generated from this page:
///     https://www.google.com/get/cardboard/viewerprofilegenerator/
/// @return True if the viewer profile specified by viewer_profile_uri was
///     successfully stored and applied, false otherwise.
bool gvr_set_default_viewer_profile(gvr_context* gvr,
                                    const char* viewer_profile_uri);

/// Refreshes gvr_context with the viewer profile that is stored on the device.
/// If it can not find the viewer profile, nothing will happen.
///
/// @param gvr Pointer to the gvr instance to refresh the profile on.
void gvr_refresh_viewer_profile(gvr_context* gvr);

/// Gets the name of the viewer vendor.
///
/// @param gvr Pointer to the gvr instance from which to get the vendor.
/// @return A pointer to the vendor name. May be NULL if no viewer is paired.
///     WARNING: This method guarantees the validity of the returned pointer
///     only until the next use of the `gvr` context. The string should be
///     copied immediately if persistence is required.
const char* gvr_get_viewer_vendor(const gvr_context* gvr);

/// Gets the name of the viewer model.
///
/// @param gvr Pointer to the gvr instance from which to get the name.
/// @return A pointer to the model name. May be NULL if no viewer is paired.
///     WARNING: This method guarantees the validity of the returned pointer
///     only until the next use of the `gvr` context. The string should be
///     copied immediately if persistence is required.
const char* gvr_get_viewer_model(const gvr_context* gvr);

/// Gets the type of the viewer, as defined by gvr_viewer_type.
///
/// @param gvr Pointer to the gvr instance from which to get the viewer type.
/// @return The gvr_viewer_type of the currently paired viewer.
int32_t gvr_get_viewer_type(const gvr_context* gvr);

/// Gets the transformation matrix to convert from Head Space to Eye Space for
/// the given eye.
///
/// @param gvr Pointer to the gvr instance from which to get the matrix.
/// @param eye Selected gvr_eye type.
/// @return Transformation matrix from Head Space to selected Eye Space.
gvr_mat4f gvr_get_eye_from_head_matrix(const gvr_context* gvr,
                                       const int32_t eye);

/// Gets the window bounds.
///
/// @param gvr Pointer to the gvr instance from which to get the bounds.
///
/// @return Window bounds in physical pixels.
gvr_recti gvr_get_window_bounds(const gvr_context* gvr);

/// Computes the distorted point for a given point in a given eye.  The
/// distortion inverts the optical distortion caused by the lens for the eye.
/// Due to chromatic aberration, the distortion is different for each
/// color channel.
///
/// @param gvr Pointer to the gvr instance which will do the computing.
/// @param eye The gvr_eye type (left or right).
/// @param uv_in A point in screen eye Viewport Space in [0,1]^2 with (0, 0)
///     in the lower left corner of the eye's viewport and (1, 1) in the
///     upper right corner of the eye's viewport.
/// @param uv_out A pointer to an array of (at least) 3 elements, with each
///     element being a Point2f representing a point in render texture eye
///     Viewport Space in [0,1]^2 with (0, 0) in the lower left corner of the
///     eye's viewport and (1, 1) in the upper right corner of the eye's
///     viewport.
///     `uv_out[0]` is the corrected position of `uv_in` for the red channel
///     `uv_out[1]` is the corrected position of `uv_in` for the green channel
///     `uv_out[2]` is the corrected position of `uv_in` for the blue channel
void gvr_compute_distorted_point(const gvr_context* gvr, const int32_t eye,
                                 const gvr_vec2f uv_in, gvr_vec2f uv_out[3]);

/// @}

#ifdef __cplusplus
}  // extern "C"
#endif

#if defined(__cplusplus) && !defined(GVR_NO_CPP_WRAPPER)
namespace gvr {

/// Convenience C++ wrapper for gvr_user_prefs.
class UserPrefs {
 public:
  /// Creates a C++ wrapper for a gvr_user_prefs object. Note that unlike most
  /// of the C++ wrappers in the API, this does not take ownership, as the
  /// gvr_user_prefs will remain valid for the lifetime of the GVR context.
  explicit UserPrefs(const gvr_user_prefs* user_prefs)
      : user_prefs_(user_prefs) {}

  UserPrefs(UserPrefs&& other) : user_prefs_(nullptr) {
    std::swap(user_prefs_, other.user_prefs_);
  }

  UserPrefs& operator=(UserPrefs&& other) {
    std::swap(user_prefs_, other.user_prefs_);
    return *this;
  }

  /// For more information, see gvr_user_prefs_get_controller_handedness().
  ControllerHandedness GetControllerHandedness() const {
    return static_cast<ControllerHandedness>(
        gvr_user_prefs_get_controller_handedness(user_prefs_));
  }

  /// Returns the wrapped C object. Does not affect ownership.
  const gvr_user_prefs* cobj() const { return user_prefs_; }

 private:
  const gvr_user_prefs* user_prefs_;

  // Disallow copy and assign.
  UserPrefs(const UserPrefs&);
  void operator=(const UserPrefs&);
};

/// Convenience C++ wrapper for the opaque gvr_buffer_viewport type.
/// The constructor allocates memory, so when used in tight loops, instances
/// should be reused.
class BufferViewport {
 public:
  BufferViewport(BufferViewport&& other)
      : viewport_(nullptr) {
    std::swap(viewport_, other.viewport_);
  }

  BufferViewport& operator=(BufferViewport&& other) {
    std::swap(viewport_, other.viewport_);
    return *this;
  }

  ~BufferViewport() {
    if (viewport_) gvr_buffer_viewport_destroy(&viewport_);
  }

  /// For more information, see gvr_buffer_viewport_get_source_fov().
  Rectf GetSourceFov() const {
    return gvr_buffer_viewport_get_source_fov(viewport_);
  }

  /// For more information, see gvr_buffer_viewport_set_source_fov().
  void SetSourceFov(const Rectf& fov) {
    gvr_buffer_viewport_set_source_fov(viewport_, fov);
  }

  /// For more information, see gvr_buffer_viewport_get_source_uv().
  Rectf GetSourceUv() const {
    return gvr_buffer_viewport_get_source_uv(viewport_);
  }

  /// For more information, see gvr_buffer_viewport_set_source_uv().
  void SetSourceUv(const Rectf& uv) {
    gvr_buffer_viewport_set_source_uv(viewport_, uv);
  }

  /// For more information, see gvr_buffer_viewport_get_target_eye().
  Eye GetTargetEye() const {
    return static_cast<Eye>(gvr_buffer_viewport_get_target_eye(viewport_));
  }

  /// For more information, see gvr_buffer_viewport_set_target_eye().
  void SetTargetEye(Eye eye) {
    gvr_buffer_viewport_set_target_eye(viewport_, eye);
  }

  /// For more information, see gvr_buffer_viewport_get_source_buffer_index().
  int32_t GetSourceBufferIndex() const {
    return gvr_buffer_viewport_get_source_buffer_index(viewport_);
  }

  /// For more information, see gvr_buffer_viewport_set_source_buffer_index().
  void SetSourceBufferIndex(int32_t buffer_index) {
    gvr_buffer_viewport_set_source_buffer_index(viewport_, buffer_index);
  }

  /// For more information, see gvr_buffer_viewport_get_external_surface_id().
  int32_t GetExternalSurfaceId() const {
    return gvr_buffer_viewport_get_external_surface_id(viewport_);
  }

  /// For more information, see gvr_buffer_viewport_set_external_surface_id().
  void SetExternalSurfaceId(const int32_t external_surface_id) {
    gvr_buffer_viewport_set_external_surface_id(viewport_, external_surface_id);
  }

  /// For more information, see gvr_buffer_viewport_get_reprojection().
  gvr_reprojection GetReprojection() const {
    return static_cast<gvr_reprojection>(
        gvr_buffer_viewport_get_reprojection(viewport_));
  }
  /// For more information, see gvr_buffer_viewport_set_reprojection().
  void SetReprojection(gvr_reprojection reprojection) {
    gvr_buffer_viewport_set_reprojection(viewport_, reprojection);
  }

  /// For more information, see gvr_buffer_viewport_equal().
  bool operator==(const BufferViewport& other) const {
    return gvr_buffer_viewport_equal(viewport_, other.viewport_) ? true : false;
  }
  bool operator!=(const BufferViewport& other) const {
    return !(*this == other);
  }

  /// @name Wrapper manipulation
  /// @{
  /// Creates a C++ wrapper for a C object and takes ownership.
  explicit BufferViewport(gvr_buffer_viewport* viewport)
      : viewport_(viewport) {}

  /// Returns the wrapped C object. Does not affect ownership.
  gvr_buffer_viewport* cobj() { return viewport_; }
  const gvr_buffer_viewport* cobj() const { return viewport_; }

  /// Returns the wrapped C object and transfers its ownership to the caller.
  /// The wrapper becomes invalid and should not be used.
  gvr_buffer_viewport* release() {
    auto result = viewport_;
    viewport_ = nullptr;
    return result;
  }
  /// @}

 private:
  friend class GvrApi;
  friend class BufferViewportList;

  explicit BufferViewport(gvr_context* gvr)
      : viewport_(gvr_buffer_viewport_create(gvr)) {}

  gvr_buffer_viewport* viewport_;
};

/// Convenience C++ wrapper for the opaque gvr_buffer_viewport_list type. This
/// class will automatically release the wrapped gvr_buffer_viewport_list upon
/// destruction. It can only be created via a `GvrApi` instance, and its
/// validity is tied to the lifetime of that instance.
class BufferViewportList {
 public:
  BufferViewportList(BufferViewportList&& other)
      : context_(nullptr), viewport_list_(nullptr) {
    std::swap(context_, other.context_);
    std::swap(viewport_list_, other.viewport_list_);
  }

  BufferViewportList& operator=(BufferViewportList&& other) {
    std::swap(context_, other.context_);
    std::swap(viewport_list_, other.viewport_list_);
    return *this;
  }

  ~BufferViewportList() {
    if (viewport_list_) {
      gvr_buffer_viewport_list_destroy(&viewport_list_);
    }
  }

  /// For more information, see gvr_get_recommended_buffer_viewports().
  void SetToRecommendedBufferViewports() {
    gvr_get_recommended_buffer_viewports(context_, viewport_list_);
  }

  /// For more information, see gvr_get_screen_buffer_viewports().
  void SetToScreenBufferViewports() {
    gvr_get_screen_buffer_viewports(context_, viewport_list_);
  }

  /// For more information, see gvr_buffer_viewport_list_set_item().
  void SetBufferViewport(size_t index, const BufferViewport& viewport) {
    gvr_buffer_viewport_list_set_item(viewport_list_, index,
                                      viewport.viewport_);
  }

  /// For more information, see gvr_buffer_viewport_list_get_item().
  void GetBufferViewport(size_t index, BufferViewport* viewport) const {
    gvr_buffer_viewport_list_get_item(viewport_list_, index,
                                      viewport->viewport_);
  }

  /// For more information, see gvr_buffer_viewport_list_get_size().
  size_t GetSize() const {
    return gvr_buffer_viewport_list_get_size(viewport_list_);
  }

  /// @name Wrapper manipulation
  /// @{
  /// Creates a C++ wrapper for a C object and takes ownership.
  BufferViewportList(gvr_buffer_viewport_list* viewport_list,
                     gvr_context* context)
      : context_(context),
        viewport_list_(viewport_list) {}

  /// Returns the wrapped C object. Does not affect ownership.
  gvr_buffer_viewport_list* cobj() { return viewport_list_; }
  const gvr_buffer_viewport_list* cobj() const { return viewport_list_; }

  /// Returns the wrapped C object and transfers its ownership to the caller.
  /// The wrapper becomes invalid and should not be used.
  gvr_buffer_viewport_list* release() {
    auto result = viewport_list_;
    viewport_list_ = nullptr;
    return result;
  }
  /// @}

 private:
  friend class Frame;
  friend class GvrApi;
  friend class SwapChain;

  explicit BufferViewportList(gvr_context* context)
      : context_(context),
        viewport_list_(gvr_buffer_viewport_list_create(context)) {}

  const gvr_context* context_;
  gvr_buffer_viewport_list* viewport_list_;

  // Disallow copy and assign.
  BufferViewportList(const BufferViewportList&) = delete;
  void operator=(const BufferViewportList&) = delete;
};

/// Convenience C++ wrapper for gvr_buffer_spec, an opaque pixel buffer
/// specification. Frees the underlying gvr_buffer_spec on destruction.
class BufferSpec {
 public:
  BufferSpec(BufferSpec&& other)
      : spec_(nullptr) {
    std::swap(spec_, other.spec_);
  }

  BufferSpec& operator=(BufferSpec&& other) {
    std::swap(spec_, other.spec_);
    return *this;
  }

  ~BufferSpec() {
    if (spec_) gvr_buffer_spec_destroy(&spec_);
  }

  /// Gets the buffer's size. The default value is the recommended render
  /// target size. For more information, see gvr_buffer_spec_get_size().
  Sizei GetSize() const {
    return gvr_buffer_spec_get_size(spec_);
  }

  /// Sets the buffer's size. For more information, see
  /// gvr_buffer_spec_set_size().
  void SetSize(const Sizei& size) {
    gvr_buffer_spec_set_size(spec_, size);
  }

  /// Sets the buffer's size to the passed width and height. For more
  /// information, see gvr_buffer_spec_set_size().
  ///
  /// @param width The width in pixels. Must be greater than 0.
  /// @param height The height in pixels. Must be greater than 0.
  void SetSize(int32_t width, int32_t height) {
    gvr_sizei size{width, height};
    gvr_buffer_spec_set_size(spec_, size);
  }

  /// Gets the number of samples per pixel in the buffer. For more
  /// information, see gvr_buffer_spec_get_samples().
  int32_t GetSamples() const { return gvr_buffer_spec_get_samples(spec_); }

  /// Sets the number of samples per pixel. For more information, see
  /// gvr_buffer_spec_set_samples().
  void SetSamples(int32_t num_samples) {
    gvr_buffer_spec_set_samples(spec_, num_samples);
  }

  /// Sets the color format for this buffer. For more information, see
  /// gvr_buffer_spec_set_color_format().
  void SetColorFormat(ColorFormat color_format) {
    gvr_buffer_spec_set_color_format(spec_, color_format);
  }

  /// Sets the depth and stencil format for this buffer. For more
  /// information, see gvr_buffer_spec_set_depth_stencil_format().
  void SetDepthStencilFormat(DepthStencilFormat depth_stencil_format) {
    gvr_buffer_spec_set_depth_stencil_format(spec_, depth_stencil_format);
  }

  /// @name Wrapper manipulation
  /// @{
  /// Creates a C++ wrapper for a C object and takes ownership.
  explicit BufferSpec(gvr_buffer_spec* spec) : spec_(spec) {}

  /// Returns the wrapped C object. Does not affect ownership.
  gvr_buffer_spec* cobj() { return spec_; }
  const gvr_buffer_spec* cobj() const { return spec_; }

  /// Returns the wrapped C object and transfers its ownership to the caller.
  /// The wrapper becomes invalid and should not be used.
  gvr_buffer_spec* release() {
    auto result = spec_;
    spec_ = nullptr;
    return result;
  }
  /// @}

 private:
  friend class GvrApi;
  friend class SwapChain;

  explicit BufferSpec(gvr_context* gvr) {
    spec_ = gvr_buffer_spec_create(gvr);
  }

  gvr_buffer_spec* spec_;
};

/// Convenience C++ wrapper for gvr_frame, which represents a single frame
/// acquired for rendering from the swap chain.
class Frame {
 public:
  Frame(Frame&& other) : frame_(nullptr) {
    std::swap(frame_, other.frame_);
  }

  Frame& operator=(Frame&& other) {
    std::swap(frame_, other.frame_);
    return *this;
  }

  ~Frame() {
    // The swap chain owns the frame, so no clean-up is required.
  }

  /// For more information, see gvr_frame_get_buffer_size().
  Sizei GetBufferSize(int32_t index) const {
    return gvr_frame_get_buffer_size(frame_, index);
  }

  /// For more information, see gvr_frame_bind_buffer().
  void BindBuffer(int32_t index) {
    gvr_frame_bind_buffer(frame_, index);
  }

  /// For more information, see gvr_frame_unbind().
  void Unbind() {
    gvr_frame_unbind(frame_);
  }

  /// For more information, see gvr_frame_get_framebuffer_object().
  int32_t GetFramebufferObject(int32_t index) {
    return gvr_frame_get_framebuffer_object(frame_, index);
  }

  /// For more information, see gvr_frame_submit().
  void Submit(const BufferViewportList& viewport_list,
              const Mat4f& head_space_from_start_space) {
    gvr_frame_submit(&frame_, viewport_list.viewport_list_,
                     head_space_from_start_space);
  }

  /// @name Wrapper manipulation
  /// @{
  /// Creates a C++ wrapper for a C object and takes ownership.
  explicit Frame(gvr_frame* frame) : frame_(frame) {}

  /// Returns the wrapped C object. Does not affect ownership.
  gvr_frame* cobj() { return frame_; }
  const gvr_frame* cobj() const { return frame_; }

  /// Returns whether the wrapped gvr_frame reference is valid.
  bool is_valid() const { return frame_ != nullptr; }
  explicit operator bool const() { return is_valid(); }

  /// Returns the wrapped C object and transfers its ownership to the caller.
  /// The wrapper becomes invalid and should not be used.
  gvr_frame* release() {
    auto result = frame_;
    frame_ = nullptr;
    return result;
  }
  /// @}

 private:
  friend class SwapChain;

  gvr_frame* frame_;
};

/// Convenience C++ wrapper for gvr_swap_chain, which represents a queue of
/// frames. The GvrApi object must outlive any SwapChain objects created from
/// it.
class SwapChain {
 public:
  SwapChain(SwapChain&& other)
      : swap_chain_(nullptr) {
    std::swap(swap_chain_, other.swap_chain_);
  }

  SwapChain& operator=(SwapChain&& other) {
    std::swap(swap_chain_, other.swap_chain_);
    return *this;
  }

  ~SwapChain() {
    if (swap_chain_) gvr_swap_chain_destroy(&swap_chain_);
  }

  /// For more information, see gvr_swap_chain_get_buffer_count().
  int32_t GetBufferCount() const {
    return gvr_swap_chain_get_buffer_count(swap_chain_);
  }

  /// For more information, see gvr_swap_chain_get_buffer_size().
  Sizei GetBufferSize(int32_t index) const {
    return gvr_swap_chain_get_buffer_size(swap_chain_, index);
  }

  /// For more information, see gvr_swap_chain_resize_buffer().
  void ResizeBuffer(int32_t index, Sizei size) {
    gvr_swap_chain_resize_buffer(swap_chain_, index, size);
  }

  /// For more information, see gvr_swap_chain_acquire_frame().
  /// Note that if Frame acquisition fails, the returned Frame may not be valid.
  /// The caller should inspect the returned Frame's validity before using,
  /// and reschedule frame acquisition upon failure.
  Frame AcquireFrame() {
    Frame result(gvr_swap_chain_acquire_frame(swap_chain_));
    return result;
  }

  /// @name Wrapper manipulation
  /// @{
  /// Creates a C++ wrapper for a C object and takes ownership.
  explicit SwapChain(gvr_swap_chain* swap_chain) : swap_chain_(swap_chain) {}

  /// Returns the wrapped C object. Does not affect ownership.
  gvr_swap_chain* cobj() { return swap_chain_; }
  const gvr_swap_chain* cobj() const { return swap_chain_; }

  /// Returns the wrapped C object and transfers its ownership to the caller.
  /// The wrapper becomes invalid and should not be used.
  gvr_swap_chain* release() {
    auto result = swap_chain_;
    swap_chain_ = nullptr;
    return result;
  }
  /// @}

 private:
  friend class GvrApi;

  SwapChain(gvr_context* gvr, const std::vector<BufferSpec>& specs) {
    std::vector<const gvr_buffer_spec*> c_specs;
    for (const auto& spec : specs)
      c_specs.push_back(spec.spec_);
    swap_chain_ = gvr_swap_chain_create(gvr, c_specs.data(),
                                        static_cast<int32_t>(c_specs.size()));
  }

  gvr_swap_chain* swap_chain_;

  // Disallow copy and assign.
  SwapChain(const SwapChain&);
  void operator=(const SwapChain&);
};

/// This is a convenience C++ wrapper for the Google VR C API.
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
/// Example API usage:
///
///     // Functionality supplied by the application in the example below has
///     // the "app-" prefix.
///     #ifdef __ANDROID__
///     // On Android, the gvr_context should almost always be obtained from the
///     // Java GvrLayout object via
///     // GvrLayout.getGvrApi().getNativeGvrContext().
///     std::unique_ptr<GvrApi> gvr = GvrApi::WrapNonOwned(gvr_context);
///     #else
///     std::unique_ptr<GvrApi> gvr = GvrApi::Create();
///     #endif
///
///     gvr->InitializeGl();
///
///     gvr::BufferViewportList viewport_list =
///         gvr->CreateEmptyBufferViewportList();
///     gvr->GetRecommendedBufferViewports(&viewport_list);
///     gvr::BufferViewport left_eye_viewport = gvr->CreateBufferViewport();
///     gvr::BufferViewport right_eye_viewport = gvr->CreateBufferViewport();
///     viewport_list.Get(0, &left_eye_view);
///     viewport_list.Get(1, &right_eye_view);
///
///     std::vector<gvr::BufferSpec> specs;
///     specs.push_back(gvr->CreateBufferSpec());
///     specs[0].SetSamples(app_samples);
///     gvr::SwapChain swap_chain = gvr->CreateSwapChain(specs);
///
///     while (client_app_should_render) {
///       // A client app should be ready for the render target size to change
///       // whenever a new QR code is scanned, or a new viewer is paired.
///       gvr::Sizei render_target_size =
///           gvr->GetRecommendedRenderTargetSize();
///       swap_chain.ResizeBuffer(0, render_target_size);
///       gvr::Frame frame = swap_chain.AcquireFrame();
///       while (!frame) {
///         std::this_thread::sleep_for(2ms);
///         frame = swap_chain.AcquireFrame();
///       }
///
///       // This function will depend on your render loop's implementation.
///       gvr::ClockTimePoint next_vsync = AppGetNextVsyncTime();
///
///       const gvr::Mat4f head_view =
///           gvr->GetHeadSpaceFromStartSpaceRotation(next_vsync);
///       const gvr::Mat4f left_eye_view = MatrixMultiply(
///           gvr->GetEyeFromHeadMatrix(kLeftEye), head_view);
///       const gvr::Mat4f right_eye_view = MatrixMultiply(
///           gvr->GetEyeFromHeadMatrix(kRightEye), head_view);
///
///       frame.BindBuffer(0);
///       // App does its rendering to the current framebuffer here.
///       AppDoSomeRenderingForEye(
///           left_eye_viewport.GetSourceUv(), left_eye_view);
///       AppDoSomeRenderingForEye(
///           right_eye_viewport.GetSourceUv(), right_eye_view);
///       frame.Unbind();
///       frame.Submit(viewport_list, head_matrix);
///     }
///
class GvrApi {
 public:
#ifdef __ANDROID__
  /// Instantiates and returns a GvrApi instance that owns a gvr_context.
  ///
  /// @param env The JNIEnv associated with the current thread.
  /// @param app_context The Android application context. This must be the
  ///     application context, NOT an Activity context (Note: from any Android
  ///     Activity in your app, you can call getApplicationContext() to
  ///     retrieve the application context).
  /// @param class_loader The class loader to use when loading Java classes.
  ///     This must be your app's main class loader (usually accessible through
  ///     activity.getClassLoader() on any of your Activities).
  /// @return unique_ptr to the created GvrApi instance, nullptr on failure.
  static std::unique_ptr<GvrApi> Create(JNIEnv* env, jobject app_context,
                                        jobject class_loader) {
    gvr_context* context = gvr_create(env, app_context, class_loader);
    if (!context) {
      return nullptr;
    }
    return std::unique_ptr<GvrApi>(new GvrApi(context, true /* owned */));
  }
#else
  /// Instantiates and returns a GvrApi instance that owns a gvr_context.
  ///
  /// @return unique_ptr to the created GvrApi instance, nullptr on failure.
  static std::unique_ptr<GvrApi> Create() {
    gvr_context* context = gvr_create();
    if (!context) {
      return nullptr;
    }
    return std::unique_ptr<GvrApi>(new GvrApi(context, true /* owned */));
  }
#endif  // #ifdef __ANDROID__

  ~GvrApi() {
    if (context_ && owned_) {
      gvr_destroy(&context_);
    }
  }

  /// @name Error handling
  /// @{

  /// For more information, see gvr_get_error().
  Error GetError() { return static_cast<Error>(gvr_get_error(context_)); }

  /// For more information, see gvr_clear_error().
  Error ClearError() { return static_cast<Error>(gvr_clear_error(context_)); }

  /// For more information, see gvr_get_error_string().
  static const char* GetErrorString(Error error_code) {
    return gvr_get_error_string(error_code);
  }

  /// For more information, see gvr_get_user_prefs().
  UserPrefs GetUserPrefs() { return UserPrefs(gvr_get_user_prefs(context_)); }

  /// @}

  /// @name Rendering
  /// @{

  /// For more information, see gvr_initialize_gl().
  void InitializeGl() { gvr_initialize_gl(context_); }

  /// For more information, see gvr_get_async_reprojection_enabled().
  bool GetAsyncReprojectionEnabled() const {
    return gvr_get_async_reprojection_enabled(context_);
  }

  /// Constructs a C++ wrapper for a gvr_buffer_viewport object.  For more
  /// information, see gvr_buffer_viewport_create().
  ///
  /// @return A new BufferViewport instance with memory allocated for an
  ///     underlying gvr_buffer_viewport.
  BufferViewport CreateBufferViewport() const {
    return BufferViewport(context_);
  }

  /// Constructs a C++ wrapper for a gvr_buffer_viewport_list object.
  /// For more information, see gvr_buffer_viewport_list_create().
  ///
  /// @return A new, empty BufferViewportList instance.
  ///     Note: The validity of the returned object is closely tied to the
  ///     lifetime of the member gvr_context. The caller is responsible for
  ///     ensuring correct usage accordingly.
  BufferViewportList CreateEmptyBufferViewportList() const {
    return BufferViewportList(context_);
  }

  /// For more information, see gvr_get_maximum_effective_render_target_size().
  Sizei GetMaximumEffectiveRenderTargetSize() const {
    return gvr_get_maximum_effective_render_target_size(context_);
  }

  /// For more information, see gvr_get_screen_target_size().
  Sizei GetScreenTargetSize() const {
    return gvr_get_screen_target_size(context_);
  }

  /// For more information, see gvr_set_surface_size().
  void SetSurfaceSize(Sizei surface_size_pixels) {
    gvr_set_surface_size(context_, surface_size_pixels);
  }

  /// For more information, see gvr_distort_to_screen().
  void DistortToScreen(int32_t texture_id,
                       const BufferViewportList& viewport_list,
                       const Mat4f& rendered_head_pose_in_start_space_matrix,
                       const ClockTimePoint& texture_presentation_time) {
    gvr_distort_to_screen(context_, texture_id, viewport_list.viewport_list_,
                          rendered_head_pose_in_start_space_matrix,
                          texture_presentation_time);
  }

  /// For more information, see gvr_buffer_spec_create().
  BufferSpec CreateBufferSpec() {
    return BufferSpec(context_);
  }

  /// For more information, see gvr_swap_chain_create().
  SwapChain CreateSwapChain(const std::vector<BufferSpec>& specs) {
    return SwapChain(context_, specs);
  }

  /// For more information, see gvr_bind_default_framebuffer().
  void BindDefaultFramebuffer() {
    gvr_bind_default_framebuffer(context_);
  }
  /// @}

  /// @name Head tracking
  /// @{

  /// For more information see gvr_get_head_space_from_start_space_rotation.
  ///
  /// @param time_point The time at which to calculate the head pose in start
  ///     space.
  /// @return The matrix representation of the rotation from start space
  ///     (the space with the head pose at the last tracking reset at origin) to
  ///     head space (the space with the head at origin and axes aligned to the
  ///     view vector).
  Mat4f GetHeadSpaceFromStartSpaceRotation(const ClockTimePoint& time_point) {
    return gvr_get_head_space_from_start_space_rotation(context_, time_point);
  }

  /// For more information, see gvr_apply_neck_model().
  Mat4f ApplyNeckModel(const Mat4f& head_pose_in_start_space, float factor) {
    return gvr_apply_neck_model(context_, head_pose_in_start_space, factor);
  }

  /// For more information, see gvr_pause_tracking().
  void PauseTracking() { gvr_pause_tracking(context_); }

  /// For more information, see gvr_resume_tracking().
  void ResumeTracking() { gvr_resume_tracking(context_); }

  /// For more information, see gvr_reset_tracking().
  void ResetTracking() { gvr_reset_tracking(context_); }

  // For more information, see gvr_recenter_tracking().
  void RecenterTracking() { gvr_recenter_tracking(context_); }

  /// For more information, see gvr_get_time_point_now().
  static ClockTimePoint GetTimePointNow() { return gvr_get_time_point_now(); }
  /// @}

  /// @name Viewer parameters
  /// @{

  /// For more information, see gvr_set_default_viewer_profile().
  bool SetDefaultViewerProfile(const char* viewer_profile_uri) {
    return gvr_set_default_viewer_profile(context_, viewer_profile_uri);
  }

  /// For more information, see gvr_refresh_viewer_profile().
  void RefreshViewerProfile() { gvr_refresh_viewer_profile(context_); }

  /// For more information, see gvr_get_viewer_vendor().
  const char* GetViewerVendor() const {
    return gvr_get_viewer_vendor(context_);
  }

  /// For more information, see gvr_get_viewer_model().
  const char* GetViewerModel() const { return gvr_get_viewer_model(context_); }

  /// For more information, see gvr_get_viewer_type().
  ViewerType GetViewerType() const {
    return static_cast<ViewerType>(gvr_get_viewer_type(context_));
  }

  /// For more information, see gvr_get_eye_from_head_matrix().
  Mat4f GetEyeFromHeadMatrix(Eye eye) const {
    return gvr_get_eye_from_head_matrix(context_, eye);
  }

  /// For more information, see gvr_get_window_bounds().
  Recti GetWindowBounds() const { return gvr_get_window_bounds(context_); }

  /// For more information, see gvr_compute_distorted_point().
  std::array<Vec2f, 3> ComputeDistortedPoint(Eye eye, const Vec2f& uv_in) {
    std::array<Vec2f, 3> uv_out = {{{}}};
    gvr_compute_distorted_point(context_, eye, uv_in, uv_out.data());
    return uv_out;
  }
  /// @}

  /// @name Wrapper manipulation
  /// @{
  /// Creates a C++ wrapper for a C object and optionally takes ownership.
  ///
  /// @param context C object to wrap.
  /// @param owned Whether the wrapper will own the underlying C object.
  explicit GvrApi(gvr_context* context, bool owned = true)
      : context_(context), owned_(owned) {}

  /// Returns the wrapped C object. Does not affect ownership.
  gvr_context* cobj() { return context_; }
  const gvr_context* cobj() const { return context_; }

  /// @deprecated Use cobj() instead.
  gvr_context* GetContext() { return context_; }
  /// @deprecated Use cobj() instead.
  const gvr_context* GetContext() const { return context_; }

  /// Returns the wrapped C object and transfers its ownership to the caller.
  /// The wrapper becomes invalid and should not be used.
  gvr_context* release() {
    auto result = context_;
    context_ = nullptr;
    return result;
  }

  /// Instantiates a GvrApi instance that wraps a *non-owned* gvr_context.
  ///
  /// Ownership of the provided `context` remains with the caller, and they
  /// are responsible for ensuring proper disposal of the context.
  ///
  /// @param context Pointer to a non-null, non-owned gvr_context instance.
  /// @return unique_ptr to the created GvrApi instance. Never null.
  static std::unique_ptr<GvrApi> WrapNonOwned(gvr_context* context) {
    return std::unique_ptr<GvrApi>(new GvrApi(context, false /* owned */));
  }
  /// @}

 private:
  gvr_context* context_;

  // Whether context_ is owned by the GvrApi instance. If owned, the context
  // will be released upon destruction.
  const bool owned_;

  // Disallow copy and assign.
  GvrApi(const GvrApi&);
  void operator=(const GvrApi&);
};

}  // namespace gvr
#endif  // #if defined(__cplusplus) && !defined(GVR_NO_CPP_WRAPPER)

#endif  // VR_GVR_CAPI_INCLUDE_GVR_H_
