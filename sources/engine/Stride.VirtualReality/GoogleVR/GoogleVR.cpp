// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if defined(DONT_BUILD_FOR_NOW) && (defined(ANDROID) || defined(IOS)) || !defined(__clang__)

#if !defined(__clang__)
#define size_t unsigned long //shutup a error on resharper
#endif

#if defined(IOS)
#define NP_STATIC_LINKING
#endif

#include "../../../common/core/Stride.Core.Native/CoreNative.h"
#include "../../../deps/NativePath/NativeDynamicLinking.h"
#include "../../../deps/NativePath/NativePath.h"

#define GVR_NO_CPP_WRAPPER
#include "../../../../deps/GoogleVR/vr/gvr/capi/include/gvr_types.h"
#include "../../../../deps/GoogleVR/vr/gvr/capi/include/gvr.h"

extern "C" {
	void* gGvrLibrary = NULL;
	void* gGvrGLESv2 = NULL;
	gvr_context* gGvrContext = NULL;

#define GL_COLOR_WRITEMASK 0x0C23
#define GL_SCISSOR_TEST 0x0C11
#define GL_BLEND 0x0BE2
#define GL_CULL_FACE 0x0B44
#define GL_DEPTH_TEST 0x0B71
#define GL_BLEND_EQUATION_RGB 0x8009
#define GL_BLEND_EQUATION_ALPHA 0x883D
#define GL_BLEND_DST_RGB 0x80C8
#define GL_BLEND_SRC_RGB 0x80C9
#define GL_BLEND_DST_ALPHA 0x80CA
#define GL_BLEND_SRC_ALPHA 0x80CB
#define GL_VIEWPORT 0x0BA2
#define GL_VERTEX_ATTRIB_ARRAY_ENABLED 0x8622
#define GL_VERTEX_ATTRIB_ARRAY_SIZE 0x8623
#define GL_VERTEX_ATTRIB_ARRAY_STRIDE 0x8624
#define GL_VERTEX_ATTRIB_ARRAY_TYPE 0x8625
#define GL_VERTEX_ATTRIB_ARRAY_NORMALIZED 0x886A
#define GL_VERTEX_ATTRIB_ARRAY_POINTER 0x8645
#define GL_VERTEX_ATTRIB_ARRAY_BUFFER_BINDING 0x889F
#define GL_ELEMENT_ARRAY_BUFFER_BINDING 0x8895
#define GL_ELEMENT_ARRAY_BUFFER 0x8893

#define M_PI 3.14159265358979323846

	typedef unsigned char GLboolean;
	typedef unsigned int GLenum;
	typedef unsigned int GLuint;
	typedef int GLint;
	typedef int GLsizei;
	typedef void GLvoid;

	NP_IMPORT(void, glColorMask, GLboolean red, GLboolean green, GLboolean blue, GLboolean alpha);
	NP_IMPORT(void, glDisable, GLenum cap);
	NP_IMPORT(void, glEnable, GLenum cap);
	NP_IMPORT(void, glGetBooleanv, GLenum pname, GLboolean* data);
	NP_IMPORT(void, glGetIntegerv, GLenum pname, GLint* data);
	NP_IMPORT(void, glBlendEquationSeparate, GLenum modeRGB, GLenum modeAlpha);
	NP_IMPORT(void, glBlendFuncSeparate, GLenum sfactorRGB, GLenum dfactorRGB, GLenum sfactorAlpha, GLenum dfactorAlpha);
	NP_IMPORT(void, glViewport, GLint x, GLint y, GLsizei width, GLsizei height);
	NP_IMPORT(void, glDisableVertexAttribArray, GLuint index);
	NP_IMPORT(void, glGetVertexAttribiv, GLuint index, GLenum pname, GLint *params);
	NP_IMPORT(void, glVertexAttribPointer, GLuint index, GLint size, GLenum type, GLboolean normalized, GLsizei stride, const GLvoid* pointer);
	NP_IMPORT(void, glGetVertexAttribPointerv, GLuint index, GLenum pname, GLvoid** pointer);
	NP_IMPORT(void, glBindBuffer, GLenum target, GLuint buffer);

	NP_IMPORT(void, gvr_initialize_gl, gvr_context* gvr);
	NP_IMPORT(int32_t, gvr_clear_error, gvr_context* gvr);
	NP_IMPORT(int32_t, gvr_get_error, gvr_context* gvr);
	NP_IMPORT(gvr_buffer_viewport_list*, gvr_buffer_viewport_list_create, const gvr_context* gvr);
	NP_IMPORT(void, gvr_get_recommended_buffer_viewports, const gvr_context* gvr, gvr_buffer_viewport_list* viewport_list);
	NP_IMPORT(void, gvr_buffer_viewport_list_get_item, const gvr_buffer_viewport_list* viewport_list, size_t index, gvr_buffer_viewport* viewport);
	NP_IMPORT(gvr_buffer_viewport*, gvr_buffer_viewport_create, gvr_context* gvr);
	NP_IMPORT(gvr_sizei, gvr_get_maximum_effective_render_target_size, const gvr_context* gvr);
	NP_IMPORT(gvr_buffer_spec*, gvr_buffer_spec_create, gvr_context* gvr);
	NP_IMPORT(void, gvr_buffer_spec_destroy, gvr_buffer_spec** spec);
	NP_IMPORT(void, gvr_buffer_spec_set_size, gvr_buffer_spec* spec, gvr_sizei size);
	NP_IMPORT(void, gvr_buffer_spec_set_samples, gvr_buffer_spec* spec, int32_t num_samples);
	NP_IMPORT(gvr_frame*, gvr_swap_chain_acquire_frame, gvr_swap_chain* swap_chain);
	NP_IMPORT(int32_t, gvr_frame_get_framebuffer_object, const gvr_frame* frame, int32_t index);
	NP_IMPORT(void, gvr_swap_chain_resize_buffer, gvr_swap_chain* swap_chain, int32_t index, gvr_sizei size);
	NP_IMPORT(void, gvr_frame_submit, gvr_frame** frame, const gvr_buffer_viewport_list* list, gvr_mat4f head_space_from_start_space);
	NP_IMPORT(gvr_mat4f, gvr_get_head_space_from_start_space_rotation ,const gvr_context* gvr, const gvr_clock_time_point time);
	NP_IMPORT(gvr_clock_time_point, gvr_get_time_point_now);
	NP_IMPORT(gvr_rectf, gvr_buffer_viewport_get_source_uv, const gvr_buffer_viewport* viewport);
	NP_IMPORT(void, gvr_refresh_viewer_profile, gvr_context* gvr);
	NP_IMPORT(void, gvr_frame_bind_buffer, gvr_frame* frame, int32_t index);
	NP_IMPORT(void, gvr_frame_unbind, gvr_frame* frame);
	NP_IMPORT(gvr_context*, gvr_create);
	NP_IMPORT(void, gvr_set_surface_size, gvr_context* gvr, gvr_sizei surface_size_pixels);
	NP_IMPORT(void, gvr_buffer_spec_set_color_format, gvr_buffer_spec* spec, int32_t color_format);
	NP_IMPORT(void, gvr_buffer_spec_set_depth_stencil_format, gvr_buffer_spec* spec, int32_t depth_stencil_format);
	NP_IMPORT(gvr_swap_chain*, gvr_swap_chain_create, gvr_context* gvr, const gvr_buffer_spec** buffers, int32_t count);
	NP_IMPORT(gvr_mat4f, gvr_get_eye_from_head_matrix, const gvr_context* gvr, const int32_t eye);
	NP_IMPORT(gvr_rectf, gvr_buffer_viewport_get_source_fov, const gvr_buffer_viewport* viewport);

	gvr_buffer_viewport_list* xnGvr_ViewportsList = NULL;
	gvr_buffer_viewport* xnGvr_LeftVieport = NULL;
	gvr_buffer_viewport* xnGvr_RightVieport = NULL;

	gvr_swap_chain* xnGvr_swap_chain = NULL;

	gvr_sizei xnGvr_size;

	uint64_t kPredictionTimeWithoutVsyncNanos = 50000000;

	int xnGvrStartup(gvr_context* context)
	{
		//cnDebugPrintLine("xnGvrStartup");

		if (!gGvrLibrary)
		{
#if defined(ANDROID)
			gGvrLibrary = LoadDynamicLibrary("libgvr");
			gGvrGLESv2 = LoadDynamicLibrary("libGLESv2");
			auto core = LoadDynamicLibrary("libcore");
			cnDebugPrintLine = (CnPrintDebugFunc)GetSymbolAddress(core, "cnDebugPrintLine");
#else
			gGvrLibrary = LoadDynamicLibrary(NULL);
			gGvrGLESv2 = LoadDynamicLibrary(NULL);
#endif

			if (!gGvrLibrary) return 1;

			NP_LOAD(gGvrGLESv2, glColorMask);
			NP_CHECK(glColorMask, return false);
			NP_LOAD(gGvrGLESv2, glDisable);
			NP_CHECK(glDisable, return false);
			NP_LOAD(gGvrGLESv2, glEnable);
			NP_CHECK(glEnable, return false);
			NP_LOAD(gGvrGLESv2, glGetBooleanv);
			NP_CHECK(glGetBooleanv, return false);
			NP_LOAD(gGvrGLESv2, glGetIntegerv);
			NP_CHECK(glGetIntegerv, return false);
			NP_LOAD(gGvrGLESv2, glBlendEquationSeparate);
			NP_CHECK(glBlendEquationSeparate, return false);
			NP_LOAD(gGvrGLESv2, glBlendFuncSeparate);
			NP_CHECK(glBlendFuncSeparate, return false);
			NP_LOAD(gGvrGLESv2, glViewport);
			NP_CHECK(glViewport, return false);
			NP_LOAD(gGvrGLESv2, glDisableVertexAttribArray);
			NP_CHECK(glDisableVertexAttribArray, return false);
			NP_LOAD(gGvrGLESv2, glGetVertexAttribiv);
			NP_CHECK(glGetVertexAttribiv, return false);
			NP_LOAD(gGvrGLESv2, glVertexAttribPointer);
			NP_CHECK(glVertexAttribPointer, return false);
			NP_LOAD(gGvrGLESv2, glGetVertexAttribPointerv);
			NP_CHECK(glGetVertexAttribPointerv, return false);
			NP_LOAD(gGvrGLESv2, glBindBuffer);
			NP_CHECK(glBindBuffer, return false);

			NP_LOAD(gGvrLibrary, gvr_refresh_viewer_profile);
			NP_CHECK(gvr_refresh_viewer_profile, return 2);
		}

		if(context)
		{
			gGvrContext = context;
		}
		else
		{
			NP_LOAD(gGvrLibrary, gvr_create);
			NP_CHECK(gvr_create, return 3);
			gGvrContext = NP_CALL(gvr_create);

			if (gGvrContext == NULL) return 4;
		}
		
		NP_LOAD(gGvrLibrary, gvr_get_maximum_effective_render_target_size);
		NP_CHECK(gvr_get_maximum_effective_render_target_size, return 5);

		NP_CALL(gvr_refresh_viewer_profile, gGvrContext);

		NP_LOAD(gGvrLibrary, gvr_set_surface_size);
		NP_CHECK(gvr_set_surface_size, return 6);

		return 0;
	}

	void xnGvrGetMaxRenderSize(int* outWidth, int* outHeight)
	{
		auto maxSize = NP_CALL(gvr_get_maximum_effective_render_target_size, gGvrContext);
		*outHeight = maxSize.height;
		*outWidth = maxSize.width;
	}

	npBool xnGvrInit(int width, int height)
	{
		xnGvr_size.width = width;
		xnGvr_size.height = height;

		NP_CALL(gvr_set_surface_size, gGvrContext, xnGvr_size);

		NP_LOAD(gGvrLibrary, gvr_initialize_gl);
		NP_CHECK(gvr_initialize_gl, return false);
		NP_LOAD(gGvrLibrary, gvr_clear_error);
		NP_CHECK(gvr_clear_error, return false);
		NP_LOAD(gGvrLibrary, gvr_get_error);
		NP_CHECK(gvr_get_error, return false);
		NP_LOAD(gGvrLibrary, gvr_buffer_viewport_list_create);
		NP_CHECK(gvr_buffer_viewport_list_create, return false);
		NP_LOAD(gGvrLibrary, gvr_get_recommended_buffer_viewports);
		NP_CHECK(gvr_get_recommended_buffer_viewports, return false);
		NP_LOAD(gGvrLibrary, gvr_buffer_viewport_list_get_item);
		NP_CHECK(gvr_buffer_viewport_list_get_item, return false);
		NP_LOAD(gGvrLibrary, gvr_buffer_viewport_create);
		NP_CHECK(gvr_buffer_viewport_create, return false);
		NP_LOAD(gGvrLibrary, gvr_buffer_spec_create);
		NP_CHECK(gvr_buffer_spec_create, return false);
		NP_LOAD(gGvrLibrary, gvr_buffer_spec_destroy);
		NP_CHECK(gvr_buffer_spec_destroy, return false);
		NP_LOAD(gGvrLibrary, gvr_swap_chain_acquire_frame);
		NP_CHECK(gvr_swap_chain_acquire_frame, return false);
		NP_LOAD(gGvrLibrary, gvr_frame_get_framebuffer_object);
		NP_CHECK(gvr_frame_get_framebuffer_object, return false);
		NP_LOAD(gGvrLibrary, gvr_swap_chain_resize_buffer);
		NP_CHECK(gvr_swap_chain_resize_buffer, return false);
		NP_LOAD(gGvrLibrary, gvr_buffer_spec_set_color_format);
		NP_CHECK(gvr_buffer_spec_set_color_format, return false);
		NP_LOAD(gGvrLibrary, gvr_buffer_spec_set_depth_stencil_format);
		NP_CHECK(gvr_buffer_spec_set_depth_stencil_format, return false);
		NP_LOAD(gGvrLibrary, gvr_swap_chain_create);
		NP_CHECK(gvr_swap_chain_create, return false);
		NP_LOAD(gGvrLibrary, gvr_frame_submit);
		NP_CHECK(gvr_frame_submit, return false);
		NP_LOAD(gGvrLibrary, gvr_get_head_space_from_start_space_rotation);
		NP_CHECK(gvr_get_head_space_from_start_space_rotation, return false);
		NP_LOAD(gGvrLibrary, gvr_get_time_point_now);
		NP_CHECK(gvr_get_time_point_now, return false);
		NP_LOAD(gGvrLibrary, gvr_frame_bind_buffer);
		NP_CHECK(gvr_frame_bind_buffer, return false);
		NP_LOAD(gGvrLibrary, gvr_frame_unbind);
		NP_CHECK(gvr_frame_unbind, return false);
		NP_LOAD(gGvrLibrary, gvr_buffer_viewport_get_source_uv);
		NP_CHECK(gvr_buffer_viewport_get_source_uv, return false);
		NP_LOAD(gGvrLibrary, gvr_buffer_spec_set_size);
		NP_CHECK(gvr_buffer_spec_set_size, return false);
		NP_LOAD(gGvrLibrary, gvr_buffer_spec_set_samples);
		NP_CHECK(gvr_buffer_spec_set_samples, return false);
		NP_LOAD(gGvrLibrary, gvr_get_eye_from_head_matrix);
		NP_CHECK(gvr_get_eye_from_head_matrix, return false);
		NP_LOAD(gGvrLibrary, gvr_buffer_viewport_get_source_fov);
		NP_CHECK(gvr_buffer_viewport_get_source_fov, return false);

		NP_CALL(gvr_clear_error, gGvrContext);
		NP_CALL(gvr_initialize_gl, gGvrContext);
		if (NP_CALL(gvr_get_error, gGvrContext) != GVR_ERROR_NONE) return false;

		NP_CALL(gvr_clear_error, gGvrContext);
		xnGvr_ViewportsList = NP_CALL(gvr_buffer_viewport_list_create, gGvrContext);
		NP_CALL(gvr_get_recommended_buffer_viewports, gGvrContext, xnGvr_ViewportsList);
		if (NP_CALL(gvr_get_error, gGvrContext) != GVR_ERROR_NONE) return false;

		NP_CALL(gvr_clear_error, gGvrContext);
		xnGvr_LeftVieport = NP_CALL(gvr_buffer_viewport_create, gGvrContext);
		xnGvr_RightVieport = NP_CALL(gvr_buffer_viewport_create, gGvrContext);
		if (NP_CALL(gvr_get_error, gGvrContext) != GVR_ERROR_NONE) return false;

		NP_CALL(gvr_clear_error, gGvrContext);
		NP_CALL(gvr_buffer_viewport_list_get_item, xnGvr_ViewportsList, 0, xnGvr_LeftVieport);
		NP_CALL(gvr_buffer_viewport_list_get_item, xnGvr_ViewportsList, 1, xnGvr_RightVieport);
		if (NP_CALL(gvr_get_error, gGvrContext) != GVR_ERROR_NONE) return false;

		NP_CALL(gvr_clear_error, gGvrContext);
		auto bufferSpec = NP_CALL(gvr_buffer_spec_create, gGvrContext);
		if (NP_CALL(gvr_get_error, gGvrContext) != GVR_ERROR_NONE) return false;

		NP_CALL(gvr_clear_error, gGvrContext);
		NP_CALL(gvr_buffer_spec_set_size, bufferSpec, xnGvr_size);
		if (NP_CALL(gvr_get_error, gGvrContext) != GVR_ERROR_NONE) return false;

		NP_CALL(gvr_clear_error, gGvrContext);
		NP_CALL(gvr_buffer_spec_set_samples, bufferSpec, 1);
		if (NP_CALL(gvr_get_error, gGvrContext) != GVR_ERROR_NONE) return false;

		NP_CALL(gvr_clear_error, gGvrContext);
		NP_CALL(gvr_buffer_spec_set_color_format, bufferSpec, GVR_COLOR_FORMAT_RGBA_8888);
		if (NP_CALL(gvr_get_error, gGvrContext) != GVR_ERROR_NONE) return false;

		NP_CALL(gvr_clear_error, gGvrContext);
		NP_CALL(gvr_buffer_spec_set_depth_stencil_format, bufferSpec, GVR_DEPTH_STENCIL_FORMAT_NONE);
		if (NP_CALL(gvr_get_error, gGvrContext) != GVR_ERROR_NONE) return false;
	
		NP_CALL(gvr_clear_error, gGvrContext);
		const gvr_buffer_spec* specs[] = { bufferSpec };
		xnGvr_swap_chain = NP_CALL(gvr_swap_chain_create, gGvrContext, specs, 1);
		if (NP_CALL(gvr_get_error, gGvrContext) != GVR_ERROR_NONE || xnGvr_swap_chain == NULL) return false;

		NP_CALL(gvr_buffer_spec_destroy, &bufferSpec);

		NP_CALL(gvr_clear_error, gGvrContext);
		NP_CALL(gvr_swap_chain_resize_buffer, xnGvr_swap_chain, 0, xnGvr_size);
		if (NP_CALL(gvr_get_error, gGvrContext)) return false;

		return true;
	}

	void xnGvrGetPerspectiveMatrix(int eyeIndex, float near_clip, float far_clip, gvr_mat4f* outResult)
	{
#ifdef IOS
		//eyeIndex = eyeIndex == 0 ? 1 : 0;
#endif

		auto fov = NP_CALL(gvr_buffer_viewport_get_source_fov, eyeIndex == 0 ? xnGvr_LeftVieport : xnGvr_RightVieport);
		float x_left = -tan(fov.left * M_PI / 180.0f) * near_clip;
		float x_right = tan(fov.right * M_PI / 180.0f) * near_clip;
		float y_bottom = -tan(fov.bottom * M_PI / 180.0f) * near_clip;
		float y_top = tan(fov.top * M_PI / 180.0f) * near_clip;

		const auto X = (2 * near_clip) / (x_right - x_left);
		const auto Y = (2 * near_clip) / (y_top - y_bottom);
		const auto A = (x_right + x_left) / (x_right - x_left);
		const auto B = (y_top + y_bottom) / (y_top - y_bottom);
		const auto C = (near_clip + far_clip) / (near_clip - far_clip);
		const auto D = (2 * near_clip * far_clip) / (near_clip - far_clip);

		for (auto i = 0; i < 4; ++i) 
		{
			for (auto j = 0; j < 4; ++j) 
			{
				outResult->m[i][j] = 0.0f;
			}
		}

		outResult->m[0][0] = X;
		outResult->m[0][2] = A;
		outResult->m[1][1] = Y;
		outResult->m[1][2] = B;
		outResult->m[2][2] = C;
		outResult->m[2][3] = D;
		outResult->m[3][2] = -1;
	}

	void xnGvrGetHeadMatrix(float* outMatrix)
	{
		auto time = NP_CALL(gvr_get_time_point_now);
		time.monotonic_system_time_nanos += kPredictionTimeWithoutVsyncNanos;
		auto gvrMat4 = reinterpret_cast<gvr_mat4f*>(outMatrix);
		*gvrMat4 = NP_CALL(gvr_get_head_space_from_start_space_rotation, gGvrContext, time);
	}

	void xnGvrGetEyeMatrix(int eyeIndex, float* outMatrix)
	{
#ifdef IOS
		//eyeIndex = eyeIndex == 0 ? 1 : 0;
#endif

		auto gvrMat4 = reinterpret_cast<gvr_mat4f*>(outMatrix);
		*gvrMat4 = NP_CALL(gvr_get_eye_from_head_matrix, gGvrContext, eyeIndex);
	}

	void* xnGvrGetNextFrame()
	{
		NP_CALL(gvr_clear_error, gGvrContext);
		auto frame = NP_CALL(gvr_swap_chain_acquire_frame, xnGvr_swap_chain);
		auto err = NP_CALL(gvr_get_error, gGvrContext);
		return err == GVR_ERROR_NONE ? frame : NULL;
	}

	int xnGvrGetFBOIndex(gvr_frame* frame, int index)
	{
		return NP_CALL(gvr_frame_get_framebuffer_object, frame, index);
	}

	npBool xnGvrSubmitFrame(gvr_frame* frame, float* headMatrix)
	{
		GLboolean masks[4];
		NP_CALL(glGetBooleanv, GL_COLOR_WRITEMASK, masks);
		NP_CALL(glColorMask, true, true, true, true); // This was the super major headache and it's needed

		GLboolean scissor;
		NP_CALL(glGetBooleanv, GL_SCISSOR_TEST, &scissor);

		GLboolean blend;
		NP_CALL(glGetBooleanv, GL_BLEND, &blend);

		GLboolean cullFace;
		NP_CALL(glGetBooleanv, GL_CULL_FACE, &cullFace);

		GLboolean depthTest;
		NP_CALL(glGetBooleanv, GL_DEPTH_TEST, &depthTest);

		GLint eqRgb;
		NP_CALL(glGetIntegerv, GL_BLEND_EQUATION_RGB, &eqRgb);
		GLint eqAlpha;
		NP_CALL(glGetIntegerv, GL_BLEND_EQUATION_ALPHA, &eqAlpha);

		GLint dstRgb;
		NP_CALL(glGetIntegerv, GL_BLEND_DST_RGB, &dstRgb);
		GLint dstAlpha;
		NP_CALL(glGetIntegerv, GL_BLEND_DST_ALPHA, &dstAlpha);
		GLint srcRgb;
		NP_CALL(glGetIntegerv, GL_BLEND_SRC_RGB, &srcRgb);
		GLint srcAlpha;
		NP_CALL(glGetIntegerv, GL_BLEND_SRC_ALPHA, &srcAlpha);

		GLint viewport[4];
		NP_CALL(glGetIntegerv, GL_VIEWPORT, viewport);

		GLint index0Vert, index1Vert;
		NP_CALL(glGetVertexAttribiv, 0, GL_VERTEX_ATTRIB_ARRAY_ENABLED, &index0Vert);
		NP_CALL(glGetVertexAttribiv, 1, GL_VERTEX_ATTRIB_ARRAY_ENABLED, &index1Vert);

		GLint index0Size, index1Size;
		NP_CALL(glGetVertexAttribiv, 0, GL_VERTEX_ATTRIB_ARRAY_SIZE, &index0Size);
		NP_CALL(glGetVertexAttribiv, 1, GL_VERTEX_ATTRIB_ARRAY_SIZE, &index1Size);

		GLint index0Type, index1Type;
		NP_CALL(glGetVertexAttribiv, 0, GL_VERTEX_ATTRIB_ARRAY_TYPE, &index0Type);
		NP_CALL(glGetVertexAttribiv, 1, GL_VERTEX_ATTRIB_ARRAY_TYPE, &index1Type);

		GLint index0Normalized, index1Normalized;
		NP_CALL(glGetVertexAttribiv, 0, GL_VERTEX_ATTRIB_ARRAY_NORMALIZED, &index0Normalized);
		NP_CALL(glGetVertexAttribiv, 1, GL_VERTEX_ATTRIB_ARRAY_NORMALIZED, &index1Normalized);

		GLint index0Stride, index1Stride;
		NP_CALL(glGetVertexAttribiv, 0, GL_VERTEX_ATTRIB_ARRAY_STRIDE, &index0Stride);
		NP_CALL(glGetVertexAttribiv, 1, GL_VERTEX_ATTRIB_ARRAY_STRIDE, &index1Stride);

		GLvoid* index0Ptr;
		GLvoid* index1Ptr;
		NP_CALL(glGetVertexAttribPointerv, 0, GL_VERTEX_ATTRIB_ARRAY_POINTER, &index0Ptr);
		NP_CALL(glGetVertexAttribPointerv, 1, GL_VERTEX_ATTRIB_ARRAY_POINTER, &index1Ptr);

		GLint indexBuffer;
		NP_CALL(glGetIntegerv, GL_ELEMENT_ARRAY_BUFFER_BINDING, &indexBuffer);

		NP_CALL(gvr_clear_error, gGvrContext);		
		gvr_mat4f* gvrMat4 = reinterpret_cast<gvr_mat4f*>(headMatrix);
		NP_CALL(gvr_frame_submit, &frame, xnGvr_ViewportsList, *gvrMat4);
		auto err = NP_CALL(gvr_get_error, gGvrContext);

		NP_CALL(glViewport, viewport[0], viewport[1], viewport[2], viewport[3]);

		NP_CALL(glColorMask, masks[0], masks[1], masks[2], masks[3]);
		
		if (scissor)
		{
			NP_CALL(glEnable, GL_SCISSOR_TEST);
		}

		if(!blend)
		{
			NP_CALL(glDisable, GL_BLEND);
		}

		if(cullFace)
		{
			NP_CALL(glEnable, GL_CULL_FACE);
		}

		if(depthTest)
		{
			NP_CALL(glEnable, GL_DEPTH_TEST);
		}

		NP_CALL(glBlendEquationSeparate, eqRgb, eqAlpha);

		NP_CALL(glBlendFuncSeparate, srcRgb, dstRgb, srcAlpha, dstAlpha);

		NP_CALL(glVertexAttribPointer, 0, index0Size, index0Type, index0Normalized, index0Stride, index0Ptr);
		NP_CALL(glVertexAttribPointer, 1, index1Size, index1Type, index1Normalized, index1Stride, index1Ptr);

		if(!index0Vert)
		{
			NP_CALL(glDisableVertexAttribArray, 0);
		}

		if (!index1Vert)
		{
			NP_CALL(glDisableVertexAttribArray, 1);
		}

		NP_CALL(glBindBuffer, GL_ELEMENT_ARRAY_BUFFER, indexBuffer);

		return err == GVR_ERROR_NONE;
	}
}

#else

#endif
