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

#ifndef VR_GVR_CAPI_INCLUDE_GVR_VERSION_H_
#define VR_GVR_CAPI_INCLUDE_GVR_VERSION_H_

#ifdef __cplusplus
extern "C" {
#endif

/// A string representation of the current GVR build version. This is of
/// the form "MAJOR.MINOR.PATCH". Note that this may differ from the runtime
/// GVR version as reported by gvr_get_version_string().
#define GVR_SDK_VERSION_STRING "1.0.2"

/// Semantic components for the current GVR build version. Note that these
/// values may differ from the runtime GVR version as reported by
/// gvr_get_version().
enum {
  GVR_SDK_MAJOR_VERSION = 1,
  GVR_SDK_MINOR_VERSION = 0,
  GVR_SDK_PATCH_VERSION = 2,
};

#ifdef __cplusplus
}  // extern "C"
#endif

#endif  // VR_GVR_CAPI_INCLUDE_GVR_VERSION_H_
