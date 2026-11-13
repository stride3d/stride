// Host-side Vulkan layer for running the Android emulator with the Stride NuGet
// Lavapipe (gfxstream forwards the guest's Vulkan calls through the host loader,
// so this layer sits in that chain).
//
// It stamps the emulator host OS into VkPhysicalDeviceProperties.deviceName
// (e.g. "...StrideHost=Linux"). The Lavapipe package ships per-RID builds
// (win-x64/linux-x64) that render slightly differently, but the guest can't see
// which host it runs on. This layer is compiled per-host, so it knows; the guest
// reads the stamp from the (forwarded) device name and buckets gold images by it.

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <vulkan/vulkan.h>
#include <vulkan/vk_layer.h>

#define LAYER_NAME "VK_LAYER_STRIDE_android_emu_helper"

#if defined(_WIN32)
#define STRIDE_HOST_OS "Windows"
#elif defined(__APPLE__)
#define STRIDE_HOST_OS "macOS"
#else
#define STRIDE_HOST_OS "Linux"
#endif

static PFN_vkGetPhysicalDeviceProperties     next_vkGetPhysicalDeviceProperties;
static PFN_vkGetPhysicalDeviceProperties2    next_vkGetPhysicalDeviceProperties2;
static PFN_vkGetPhysicalDeviceProperties2    next_vkGetPhysicalDeviceProperties2KHR;
static PFN_vkGetInstanceProcAddr next_gipa;
static PFN_vkGetDeviceProcAddr   next_gdpa;

// Append " StrideHost=<os>" to a deviceName, if it fits and isn't already stamped.
static void stamp_host(char* deviceName)
{
    static const char marker[] = " StrideHost=" STRIDE_HOST_OS;
    size_t len = strlen(deviceName);
    if (strstr(deviceName, "StrideHost=")) return;
    if (len + sizeof(marker) > VK_MAX_PHYSICAL_DEVICE_NAME_SIZE) return;
    memcpy(deviceName + len, marker, sizeof(marker));
}

static VKAPI_ATTR void VKAPI_CALL
Layer_vkGetPhysicalDeviceProperties(VkPhysicalDevice pd, VkPhysicalDeviceProperties* p)
{
    next_vkGetPhysicalDeviceProperties(pd, p);
    if (p) stamp_host(p->deviceName);
}

static VKAPI_ATTR void VKAPI_CALL
Layer_vkGetPhysicalDeviceProperties2(VkPhysicalDevice pd, VkPhysicalDeviceProperties2* p)
{
    next_vkGetPhysicalDeviceProperties2(pd, p);
    if (p) stamp_host(p->properties.deviceName);
}

static VKAPI_ATTR void VKAPI_CALL
Layer_vkGetPhysicalDeviceProperties2KHR(VkPhysicalDevice pd, VkPhysicalDeviceProperties2* p)
{
    next_vkGetPhysicalDeviceProperties2KHR(pd, p);
    if (p) stamp_host(p->properties.deviceName);
}

static VKAPI_ATTR VkResult VKAPI_CALL
Layer_vkCreateInstance(const VkInstanceCreateInfo* pCreateInfo,
                       const VkAllocationCallbacks* pAllocator, VkInstance* pInstance)
{
    VkLayerInstanceCreateInfo* link = (VkLayerInstanceCreateInfo*)pCreateInfo->pNext;
    while (link && !(link->sType == VK_STRUCTURE_TYPE_LOADER_INSTANCE_CREATE_INFO
                     && link->function == VK_LAYER_LINK_INFO)) {
        link = (VkLayerInstanceCreateInfo*)link->pNext;
    }
    if (!link) return VK_ERROR_INITIALIZATION_FAILED;
    next_gipa = link->u.pLayerInfo->pfnNextGetInstanceProcAddr;
    link->u.pLayerInfo = link->u.pLayerInfo->pNext;

    PFN_vkCreateInstance create = (PFN_vkCreateInstance)next_gipa(VK_NULL_HANDLE, "vkCreateInstance");
    VkResult r = create(pCreateInfo, pAllocator, pInstance);
    if (r != VK_SUCCESS) return r;
    next_vkGetPhysicalDeviceProperties =
        (PFN_vkGetPhysicalDeviceProperties)next_gipa(*pInstance, "vkGetPhysicalDeviceProperties");
    next_vkGetPhysicalDeviceProperties2 =
        (PFN_vkGetPhysicalDeviceProperties2)next_gipa(*pInstance, "vkGetPhysicalDeviceProperties2");
    next_vkGetPhysicalDeviceProperties2KHR =
        (PFN_vkGetPhysicalDeviceProperties2)next_gipa(*pInstance, "vkGetPhysicalDeviceProperties2KHR");
    return VK_SUCCESS;
}

static VKAPI_ATTR VkResult VKAPI_CALL
Layer_vkCreateDevice(VkPhysicalDevice pd, const VkDeviceCreateInfo* pCreateInfo,
                     const VkAllocationCallbacks* pAllocator, VkDevice* pDevice)
{
    VkLayerDeviceCreateInfo* link = (VkLayerDeviceCreateInfo*)pCreateInfo->pNext;
    while (link && !(link->sType == VK_STRUCTURE_TYPE_LOADER_DEVICE_CREATE_INFO
                     && link->function == VK_LAYER_LINK_INFO)) {
        link = (VkLayerDeviceCreateInfo*)link->pNext;
    }
    if (!link) return VK_ERROR_INITIALIZATION_FAILED;
    PFN_vkGetInstanceProcAddr ngipa = link->u.pLayerInfo->pfnNextGetInstanceProcAddr;
    next_gdpa = link->u.pLayerInfo->pfnNextGetDeviceProcAddr;
    link->u.pLayerInfo = link->u.pLayerInfo->pNext;

    // Pass through unchanged — we only need to advance the chain link so the device
    // layer chain stays intact and next_gdpa is captured for Layer_vkGetDeviceProcAddr.
    PFN_vkCreateDevice create = (PFN_vkCreateDevice)ngipa(VK_NULL_HANDLE, "vkCreateDevice");
    return create(pd, pCreateInfo, pAllocator, pDevice);
}

static VKAPI_ATTR VkResult VKAPI_CALL
Layer_vkEnumerateInstanceLayerProperties(uint32_t* pCount, VkLayerProperties* pProps)
{
    VkLayerProperties self = { LAYER_NAME, VK_MAKE_VERSION(1, 0, 0), 1,
        "Stamps emulator host OS into deviceName for Stride gold bucketing" };
    if (!pProps) { *pCount = 1; return VK_SUCCESS; }
    if (*pCount < 1) return VK_INCOMPLETE;
    pProps[0] = self; *pCount = 1; return VK_SUCCESS;
}

static VKAPI_ATTR VkResult VKAPI_CALL
Layer_vkEnumerateInstanceExtensionProperties(const char* pLayerName, uint32_t* pCount, VkExtensionProperties* p)
{
    if (pLayerName && strcmp(pLayerName, LAYER_NAME) == 0) { *pCount = 0; return VK_SUCCESS; }
    return VK_ERROR_LAYER_NOT_PRESENT;
}

VKAPI_ATTR PFN_vkVoidFunction VKAPI_CALL
Layer_vkGetInstanceProcAddr(VkInstance instance, const char* pName)
{
    if (!pName) return NULL;
    if (!strcmp(pName, "vkGetInstanceProcAddr")) return (PFN_vkVoidFunction)Layer_vkGetInstanceProcAddr;
    if (!strcmp(pName, "vkCreateInstance"))      return (PFN_vkVoidFunction)Layer_vkCreateInstance;
    if (!strcmp(pName, "vkCreateDevice"))        return (PFN_vkVoidFunction)Layer_vkCreateDevice;
    if (!strcmp(pName, "vkEnumerateInstanceLayerProperties"))
        return (PFN_vkVoidFunction)Layer_vkEnumerateInstanceLayerProperties;
    if (!strcmp(pName, "vkEnumerateInstanceExtensionProperties"))
        return (PFN_vkVoidFunction)Layer_vkEnumerateInstanceExtensionProperties;
    if (!strcmp(pName, "vkGetPhysicalDeviceProperties") && next_vkGetPhysicalDeviceProperties)
        return (PFN_vkVoidFunction)Layer_vkGetPhysicalDeviceProperties;
    if (!strcmp(pName, "vkGetPhysicalDeviceProperties2") && next_vkGetPhysicalDeviceProperties2)
        return (PFN_vkVoidFunction)Layer_vkGetPhysicalDeviceProperties2;
    if (!strcmp(pName, "vkGetPhysicalDeviceProperties2KHR") && next_vkGetPhysicalDeviceProperties2KHR)
        return (PFN_vkVoidFunction)Layer_vkGetPhysicalDeviceProperties2KHR;
    return next_gipa ? next_gipa(instance, pName) : NULL;
}

VKAPI_ATTR PFN_vkVoidFunction VKAPI_CALL
Layer_vkGetDeviceProcAddr(VkDevice device, const char* pName)
{
    return next_gdpa ? next_gdpa(device, pName) : NULL;
}

VKAPI_ATTR VkResult VKAPI_CALL
vkNegotiateLoaderLayerInterfaceVersion(VkNegotiateLayerInterface* pVersionStruct)
{
    if (pVersionStruct->loaderLayerInterfaceVersion >= 2)
        pVersionStruct->loaderLayerInterfaceVersion = 2;
    pVersionStruct->pfnGetInstanceProcAddr = Layer_vkGetInstanceProcAddr;
    pVersionStruct->pfnGetDeviceProcAddr   = Layer_vkGetDeviceProcAddr;
    pVersionStruct->pfnGetPhysicalDeviceProcAddr = NULL;
    return VK_SUCCESS;
}
