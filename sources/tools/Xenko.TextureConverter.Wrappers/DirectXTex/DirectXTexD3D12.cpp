//-------------------------------------------------------------------------------------
// DirectXTexD3D12.cpp
//  
// DirectX Texture Library - Direct3D 12 helpers
//
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//
// http://go.microsoft.com/fwlink/?LinkId=248926
//-------------------------------------------------------------------------------------

#include "directxtexp.h"

#if defined(_XBOX_ONE) && defined(_TITLE)
#include "d3dx12_x.h"
#else
#include "d3dx12.h"
#endif

#ifndef IID_GRAPHICS_PPV_ARGS
#define IID_GRAPHICS_PPV_ARGS(x) IID_PPV_ARGS(x)
#endif

using namespace DirectX;
using Microsoft::WRL::ComPtr;

static_assert(TEX_DIMENSION_TEXTURE1D == D3D12_RESOURCE_DIMENSION_TEXTURE1D, "header enum mismatch");
static_assert(TEX_DIMENSION_TEXTURE2D == D3D12_RESOURCE_DIMENSION_TEXTURE2D, "header enum mismatch");
static_assert(TEX_DIMENSION_TEXTURE3D == D3D12_RESOURCE_DIMENSION_TEXTURE3D, "header enum mismatch");

namespace
{
    template<typename T> void AdjustPlaneResource(
        _In_ DXGI_FORMAT fmt,
        _In_ size_t height,
        _In_ size_t slicePlane,
        _Inout_ T& res)
    {
        switch (static_cast<int>(fmt))
        {
        case DXGI_FORMAT_NV12:
        case DXGI_FORMAT_P010:
        case DXGI_FORMAT_P016:
        case XBOX_DXGI_FORMAT_D16_UNORM_S8_UINT:
        case XBOX_DXGI_FORMAT_R16_UNORM_X8_TYPELESS:
        case XBOX_DXGI_FORMAT_X16_TYPELESS_G8_UINT:
            if (!slicePlane)
            {
                // Plane 0
                res.SlicePitch = res.RowPitch * height;
            }
            else
            {
                // Plane 1
                res.pData = (uint8_t*)(res.pData) + res.RowPitch * height;
                res.SlicePitch = res.RowPitch * ((height + 1) >> 1);
            }
            break;

        case DXGI_FORMAT_NV11:
            if (!slicePlane)
            {
                // Plane 0
                res.SlicePitch = res.RowPitch * height;
            }
            else
            {
                // Plane 1
                res.pData = (uint8_t*)(res.pData) + res.RowPitch * height;
                res.RowPitch = (res.RowPitch >> 1);
                res.SlicePitch = res.RowPitch * height;
            }
            break;
        }
    }


    //--------------------------------------------------------------------------------------
    inline void TransitionResource(
        _In_ ID3D12GraphicsCommandList* commandList,
        _In_ ID3D12Resource* resource,
        _In_ D3D12_RESOURCE_STATES stateBefore,
        _In_ D3D12_RESOURCE_STATES stateAfter)
    {
        assert(commandList != 0);
        assert(resource != 0);

        if (stateBefore == stateAfter)
            return;

        D3D12_RESOURCE_BARRIER desc = {};
        desc.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
        desc.Transition.pResource = resource;
        desc.Transition.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;
        desc.Transition.StateBefore = stateBefore;
        desc.Transition.StateAfter = stateAfter;

        commandList->ResourceBarrier(1, &desc);
    }


    //--------------------------------------------------------------------------------------
    HRESULT Capture(_In_ ID3D12Device* device,
        _In_ ID3D12CommandQueue* pCommandQ,
        _In_ ID3D12Resource* pSource,
        const D3D12_RESOURCE_DESC& desc,
        ComPtr<ID3D12Resource>& pStaging,
        std::unique_ptr<uint8_t[]>& layoutBuff,
        UINT& numberOfPlanes,
        UINT& numberOfResources,
        D3D12_RESOURCE_STATES beforeState,
        D3D12_RESOURCE_STATES afterState)
    {
        if (!pCommandQ || !pSource)
            return E_INVALIDARG;

        numberOfPlanes = D3D12GetFormatPlaneCount(device, desc.Format);
        if (!numberOfPlanes)
            return E_INVALIDARG;

        if ((numberOfPlanes > 1) && IsDepthStencil(desc.Format))
        {
            // DirectX 12 uses planes for stencil, DirectX 11 does not
            return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
        }

        D3D12_HEAP_PROPERTIES sourceHeapProperties;
        D3D12_HEAP_FLAGS sourceHeapFlags;
        HRESULT hr = pSource->GetHeapProperties(&sourceHeapProperties, &sourceHeapFlags);
        if (FAILED(hr))
            return hr;

        numberOfResources = (desc.Dimension == D3D12_RESOURCE_DIMENSION_TEXTURE3D)
                            ? 1 : desc.DepthOrArraySize;
        numberOfResources *= desc.MipLevels;
        numberOfResources *= numberOfPlanes;

        if (numberOfResources > D3D12_REQ_SUBRESOURCES)
            return E_UNEXPECTED;

        size_t memAlloc = (sizeof(D3D12_PLACED_SUBRESOURCE_FOOTPRINT) + sizeof(UINT) + sizeof(UINT64)) * numberOfResources;
        if (memAlloc > SIZE_MAX)
            return E_UNEXPECTED;

        layoutBuff.reset(new uint8_t[memAlloc]);

        auto pLayout = reinterpret_cast<D3D12_PLACED_SUBRESOURCE_FOOTPRINT*>(layoutBuff.get());
        auto pRowSizesInBytes = reinterpret_cast<UINT64*>(pLayout + numberOfResources);
        auto pNumRows = reinterpret_cast<UINT*>(pRowSizesInBytes + numberOfResources);

        UINT64 totalResourceSize = 0;
        device->GetCopyableFootprints(&desc, 0, numberOfResources, 0,
            pLayout, pNumRows, pRowSizesInBytes, &totalResourceSize);

        if (sourceHeapProperties.Type == D3D12_HEAP_TYPE_READBACK)
        {
            // Handle case where the source is already a staging texture we can use directly
            pStaging = pSource;
            return S_OK;
        }

        // Create a command allocator
        ComPtr<ID3D12CommandAllocator> commandAlloc;
        hr = device->CreateCommandAllocator(D3D12_COMMAND_LIST_TYPE_DIRECT, IID_GRAPHICS_PPV_ARGS(commandAlloc.GetAddressOf()));
        if (FAILED(hr))
            return hr;

        // Spin up a new command list
        ComPtr<ID3D12GraphicsCommandList> commandList;
        hr = device->CreateCommandList(0, D3D12_COMMAND_LIST_TYPE_DIRECT, commandAlloc.Get(), nullptr, IID_GRAPHICS_PPV_ARGS(commandList.GetAddressOf()));
        if (FAILED(hr))
            return hr;

        // Create a fence
        ComPtr<ID3D12Fence> fence;
        hr = device->CreateFence(0, D3D12_FENCE_FLAG_NONE, IID_GRAPHICS_PPV_ARGS(fence.GetAddressOf()));
        if (FAILED(hr))
            return hr;

        CD3DX12_HEAP_PROPERTIES defaultHeapProperties(D3D12_HEAP_TYPE_DEFAULT);
        CD3DX12_HEAP_PROPERTIES readBackHeapProperties(D3D12_HEAP_TYPE_READBACK);

        // Readback resources must be buffers
        D3D12_RESOURCE_DESC bufferDesc = {};
        bufferDesc.Alignment = desc.Alignment;
        bufferDesc.DepthOrArraySize = 1;
        bufferDesc.Dimension = D3D12_RESOURCE_DIMENSION_BUFFER;
        bufferDesc.Flags = D3D12_RESOURCE_FLAG_NONE;
        bufferDesc.Format = DXGI_FORMAT_UNKNOWN;
        bufferDesc.Height = 1;
        bufferDesc.Width = totalResourceSize;
        bufferDesc.Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR;
        bufferDesc.MipLevels = 1;
        bufferDesc.SampleDesc.Count = 1;
        bufferDesc.SampleDesc.Quality = 0;

        ComPtr<ID3D12Resource> copySource(pSource);
        if (desc.SampleDesc.Count > 1)
        {
            // MSAA content must be resolved before being copied to a staging texture
            auto descCopy = desc;
            descCopy.SampleDesc.Count = 1;
            descCopy.SampleDesc.Quality = 0;

            ComPtr<ID3D12Resource> pTemp;
            hr = device->CreateCommittedResource(
                &defaultHeapProperties,
                D3D12_HEAP_FLAG_NONE,
                &descCopy,
                D3D12_RESOURCE_STATE_COPY_DEST,
                nullptr,
                IID_GRAPHICS_PPV_ARGS(pTemp.GetAddressOf()));
            if (FAILED(hr))
                return hr;

            assert(pTemp);

            DXGI_FORMAT fmt = desc.Format;
            if (IsTypeless(fmt))
            {
                // Assume a UNORM if it exists otherwise use FLOAT
                fmt = MakeTypelessUNORM(fmt);
                fmt = MakeTypelessFLOAT(fmt);
            }

            D3D12_FEATURE_DATA_FORMAT_SUPPORT formatInfo = { fmt };
            hr = device->CheckFeatureSupport(D3D12_FEATURE_FORMAT_SUPPORT, &formatInfo, sizeof(formatInfo));
            if (FAILED(hr))
                return hr;

            if (!(formatInfo.Support1 & D3D12_FORMAT_SUPPORT1_TEXTURE2D))
                return E_FAIL;

            for (UINT plane = 0; plane < numberOfPlanes; ++plane)
            {
                for (UINT item = 0; item < desc.DepthOrArraySize; ++item)
                {
                    for (UINT level = 0; level < desc.MipLevels; ++level)
                    {
                        UINT index = D3D12CalcSubresource(level, item, plane, desc.MipLevels, desc.DepthOrArraySize);
                        commandList->ResolveSubresource(pTemp.Get(), index, pSource, index, fmt);
                    }
                }
            }

            copySource = pTemp;
        }

        // Create a staging texture
        hr = device->CreateCommittedResource(
            &readBackHeapProperties,
            D3D12_HEAP_FLAG_NONE,
            &bufferDesc,
            D3D12_RESOURCE_STATE_COPY_DEST,
            nullptr,
            IID_GRAPHICS_PPV_ARGS(pStaging.GetAddressOf()));
        if (FAILED(hr))
            return hr;

        assert(pStaging);

        // Transition the resource if necessary
        TransitionResource(commandList.Get(), pSource, beforeState, D3D12_RESOURCE_STATE_COPY_SOURCE);

        // Get the copy target location
        for (UINT j = 0; j < numberOfResources; ++j)
        {
            CD3DX12_TEXTURE_COPY_LOCATION copyDest(pStaging.Get(), pLayout[j]);
            CD3DX12_TEXTURE_COPY_LOCATION copySrc(copySource.Get(), j);
            commandList->CopyTextureRegion(&copyDest, 0, 0, 0, &copySrc, nullptr);
        }

        // Transition the resource to the next state
        TransitionResource(commandList.Get(), pSource, D3D12_RESOURCE_STATE_COPY_SOURCE, afterState);

        hr = commandList->Close();
        if (FAILED(hr))
            return hr;

        // Execute the command list
        pCommandQ->ExecuteCommandLists(1, (ID3D12CommandList**)commandList.GetAddressOf());

        // Signal the fence
        hr = pCommandQ->Signal(fence.Get(), 1);
        if (FAILED(hr))
            return hr;

        // Block until the copy is complete
        while (fence->GetCompletedValue() < 1)
            SwitchToThread();

        return S_OK;
    }
}


//=====================================================================================
// Entry-points
//=====================================================================================

//-------------------------------------------------------------------------------------
// Determine if given texture metadata is supported on the given device
//-------------------------------------------------------------------------------------
_Use_decl_annotations_
bool DirectX::IsSupportedTexture(
    ID3D12Device* pDevice,
    const TexMetadata& metadata)
{
    if (!pDevice)
        return false;

    // Validate format
    DXGI_FORMAT fmt = metadata.format;

    if (!IsValid(fmt))
        return false;

    // Validate miplevel count
    if (metadata.mipLevels > D3D12_REQ_MIP_LEVELS)
        return false;

    // Validate array size, dimension, and width/height
    size_t arraySize = metadata.arraySize;
    size_t iWidth = metadata.width;
    size_t iHeight = metadata.height;
    size_t iDepth = metadata.depth;

    // Most cases are known apriori based on feature level, but we use this for robustness to handle the few optional cases
    D3D12_FEATURE_DATA_FORMAT_SUPPORT formatSupport = { fmt };
    HRESULT hr = pDevice->CheckFeatureSupport(D3D12_FEATURE_FORMAT_SUPPORT, &formatSupport, sizeof(formatSupport));
    if (FAILED(hr))
    {
        memset(&formatSupport, 0, sizeof(formatSupport));
    }

    if (metadata.mipLevels > 1 && !(formatSupport.Support1 & D3D12_FORMAT_SUPPORT1_MIP))
    {
        return false;
    }

    switch (metadata.dimension)
    {
    case TEX_DIMENSION_TEXTURE1D:
        if (!(formatSupport.Support1 & D3D12_FORMAT_SUPPORT1_TEXTURE1D))
            return false;

        if ((arraySize > D3D12_REQ_TEXTURE1D_ARRAY_AXIS_DIMENSION)
            || (iWidth > D3D12_REQ_TEXTURE1D_U_DIMENSION))
            return false;

        {
            UINT numberOfResources = static_cast<UINT>(arraySize * metadata.mipLevels);
            if (numberOfResources > D3D12_REQ_SUBRESOURCES)
                return false;
        }
        break;

    case TEX_DIMENSION_TEXTURE2D:
        if (metadata.IsCubemap())
        {
            if (!(formatSupport.Support1 & D3D12_FORMAT_SUPPORT1_TEXTURECUBE))
                return false;

            if ((arraySize > D3D12_REQ_TEXTURE2D_ARRAY_AXIS_DIMENSION)
                || (iWidth > D3D12_REQ_TEXTURECUBE_DIMENSION)
                || (iHeight > D3D12_REQ_TEXTURECUBE_DIMENSION))
                return false;
        }
        else // Not a cube map
        {
            if (!(formatSupport.Support1 & D3D12_FORMAT_SUPPORT1_TEXTURE2D))
                return false;

            if ((arraySize > D3D12_REQ_TEXTURE2D_ARRAY_AXIS_DIMENSION)
                || (iWidth > D3D12_REQ_TEXTURE2D_U_OR_V_DIMENSION)
                || (iHeight > D3D12_REQ_TEXTURE2D_U_OR_V_DIMENSION))
                return false;
        }

        {
            UINT numberOfResources = static_cast<UINT>(arraySize * metadata.mipLevels);
            if (numberOfResources > D3D12_REQ_SUBRESOURCES)
                return false;
        }
        break;

    case TEX_DIMENSION_TEXTURE3D:
        if (!(formatSupport.Support1 & D3D12_FORMAT_SUPPORT1_TEXTURE3D))
            return false;

        if ((arraySize > 1)
            || (iWidth > D3D12_REQ_TEXTURE3D_U_V_OR_W_DIMENSION)
            || (iHeight > D3D12_REQ_TEXTURE3D_U_V_OR_W_DIMENSION)
            || (iDepth > D3D12_REQ_TEXTURE3D_U_V_OR_W_DIMENSION))
            return false;

        {
            UINT numberOfResources = static_cast<UINT>(metadata.mipLevels);
            if (numberOfResources > D3D12_REQ_SUBRESOURCES)
                return false;
        }
        break;

    default:
        // Not a supported dimension
        return false;
    }

    return true;
}


//-------------------------------------------------------------------------------------
// Create a texture resource
//-------------------------------------------------------------------------------------
_Use_decl_annotations_
HRESULT DirectX::CreateTexture(
    ID3D12Device* pDevice,
    const TexMetadata& metadata,
    ID3D12Resource** ppResource)
{
    return CreateTextureEx(
        pDevice, metadata,
        D3D12_RESOURCE_FLAG_NONE, false,
        ppResource);
}

_Use_decl_annotations_
HRESULT DirectX::CreateTextureEx(
    ID3D12Device* pDevice,
    const TexMetadata& metadata,
    D3D12_RESOURCE_FLAGS resFlags,
    bool forceSRGB,
    ID3D12Resource** ppResource)
{
    if (!pDevice || !ppResource)
        return E_INVALIDARG;

    *ppResource = nullptr;

    if (!metadata.mipLevels || !metadata.arraySize)
        return E_INVALIDARG;

    if ((metadata.width > UINT32_MAX) || (metadata.height > UINT32_MAX)
        || (metadata.mipLevels > UINT16_MAX) || (metadata.arraySize > UINT16_MAX))
        return E_INVALIDARG;

    DXGI_FORMAT format = metadata.format;
    if (forceSRGB)
    {
        format = MakeSRGB(format);
    }

    D3D12_RESOURCE_DESC desc = {};
    desc.Width = static_cast<UINT>(metadata.width);
    desc.Height = static_cast<UINT>(metadata.height);
    desc.MipLevels = static_cast<UINT16>(metadata.mipLevels);
    desc.DepthOrArraySize = (metadata.dimension == TEX_DIMENSION_TEXTURE3D)
                            ? static_cast<UINT16>(metadata.depth)
                            : static_cast<UINT16>(metadata.arraySize);
    desc.Format = format;
    desc.Flags = resFlags;
    desc.SampleDesc.Count = 1;
    desc.Dimension = static_cast<D3D12_RESOURCE_DIMENSION>(metadata.dimension);

    CD3DX12_HEAP_PROPERTIES defaultHeapProperties(D3D12_HEAP_TYPE_DEFAULT);

    HRESULT hr = pDevice->CreateCommittedResource(
        &defaultHeapProperties,
        D3D12_HEAP_FLAG_NONE,
        &desc,
        D3D12_RESOURCE_STATE_COPY_DEST,
        nullptr,
        IID_GRAPHICS_PPV_ARGS(ppResource));

    return hr;
}


//-------------------------------------------------------------------------------------
// Prepares a texture resource for upload 
//-------------------------------------------------------------------------------------

_Use_decl_annotations_
HRESULT DirectX::PrepareUpload(
    ID3D12Device* pDevice,
    const Image* srcImages,
    size_t nimages,
    const TexMetadata& metadata,
    std::vector<D3D12_SUBRESOURCE_DATA>& subresources)
{
    if (!pDevice || !srcImages || !nimages || !metadata.mipLevels || !metadata.arraySize)
        return E_INVALIDARG;

    UINT numberOfPlanes = D3D12GetFormatPlaneCount(pDevice, metadata.format);
    if (!numberOfPlanes)
        return E_INVALIDARG;

    if ((numberOfPlanes > 1) && IsDepthStencil(metadata.format))
    {
        // DirectX 12 uses planes for stencil, DirectX 11 does not
        return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);
    }

    size_t numberOfResources = (metadata.dimension == TEX_DIMENSION_TEXTURE3D)
                               ? 1 : metadata.arraySize;
    numberOfResources *= metadata.mipLevels;
    numberOfResources *= numberOfPlanes;

    if (numberOfResources > D3D12_REQ_SUBRESOURCES)
        return E_INVALIDARG;

    subresources.clear();
    subresources.reserve(numberOfResources);

    // Fill out subresource array
    if (metadata.IsVolumemap())
    {
        //--- Volume case -------------------------------------------------------------
        if (!metadata.depth)
            return E_INVALIDARG;

        if (metadata.depth > UINT16_MAX)
            return E_INVALIDARG;

        if (metadata.arraySize > 1)
            // Direct3D 12 doesn't support arrays of 3D textures
            return HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED);

        for (size_t plane = 0; plane < numberOfPlanes; ++plane)
        {
            size_t depth = metadata.depth;

            for (size_t level = 0; level < metadata.mipLevels; ++level)
            {
                size_t index = metadata.ComputeIndex(level, 0, 0);
                if (index >= nimages)
                    return E_FAIL;

                const Image& img = srcImages[index];

                if (img.format != metadata.format)
                    return E_FAIL;

                if (!img.pixels)
                    return E_POINTER;

                // Verify pixels in image 1 .. (depth-1) are exactly image->slicePitch apart
                // For 3D textures, this relies on all slices of the same miplevel being continous in memory
                // (this is how ScratchImage lays them out), which is why we just give the 0th slice to Direct3D 11
                const uint8_t* pSlice = img.pixels + img.slicePitch;
                for (size_t slice = 1; slice < depth; ++slice)
                {
                    size_t tindex = metadata.ComputeIndex(level, 0, slice);
                    if (tindex >= nimages)
                        return E_FAIL;

                    const Image& timg = srcImages[tindex];

                    if (!timg.pixels)
                        return E_POINTER;

                    if (timg.pixels != pSlice
                        || timg.format != metadata.format
                        || timg.rowPitch != img.rowPitch
                        || timg.slicePitch != img.slicePitch)
                        return E_FAIL;

                    pSlice = timg.pixels + img.slicePitch;
                }

                D3D12_SUBRESOURCE_DATA res =
                {
                    img.pixels,
                    static_cast<LONG_PTR>(img.rowPitch),
                    static_cast<LONG_PTR>(img.slicePitch)
                };

                AdjustPlaneResource(metadata.format, img.height, plane, res);

                subresources.emplace_back(res);

                if (depth > 1)
                    depth >>= 1;
            }
        }
    }
    else
    {
        //--- 1D or 2D texture case ---------------------------------------------------
        for (size_t plane = 0; plane < numberOfPlanes; ++plane)
        {
            for (size_t item = 0; item < metadata.arraySize; ++item)
            {
                for (size_t level = 0; level < metadata.mipLevels; ++level)
                {
                    size_t index = metadata.ComputeIndex(level, item, 0);
                    if (index >= nimages)
                        return E_FAIL;

                    const Image& img = srcImages[index];

                    if (img.format != metadata.format)
                        return E_FAIL;

                    if (!img.pixels)
                        return E_POINTER;

                    D3D12_SUBRESOURCE_DATA res =
                    {
                        img.pixels,
                        static_cast<LONG_PTR>(img.rowPitch),
                        static_cast<LONG_PTR>(img.slicePitch)
                    };

                    AdjustPlaneResource(metadata.format, img.height, plane, res);

                    subresources.emplace_back(res);
                }
            }
        }
    }

    return S_OK;
}


//-------------------------------------------------------------------------------------
// Save a texture resource
//-------------------------------------------------------------------------------------
_Use_decl_annotations_
HRESULT DirectX::CaptureTexture(
    ID3D12CommandQueue* pCommandQueue,
    ID3D12Resource* pSource,
    bool isCubeMap,
    ScratchImage& result,
    D3D12_RESOURCE_STATES beforeState,
    D3D12_RESOURCE_STATES afterState)
{
    if (!pCommandQueue || !pSource)
        return E_INVALIDARG;

    ComPtr<ID3D12Device> device;
    pCommandQueue->GetDevice(IID_GRAPHICS_PPV_ARGS(device.GetAddressOf()));

    auto desc = pSource->GetDesc();

    ComPtr<ID3D12Resource> pStaging;
    std::unique_ptr<uint8_t[]> layoutBuff;
    UINT numberOfPlanes, numberOfResources;
    HRESULT hr = Capture(device.Get(),
        pCommandQueue,
        pSource,
        desc,
        pStaging,
        layoutBuff,
        numberOfPlanes,
        numberOfResources,
        beforeState,
        afterState);
    if (FAILED(hr))
        return hr;

    if (!layoutBuff || !numberOfPlanes || !numberOfResources)
        return E_UNEXPECTED;

    auto pLayout = reinterpret_cast<const D3D12_PLACED_SUBRESOURCE_FOOTPRINT*>(layoutBuff.get());
    auto pRowSizesInBytes = reinterpret_cast<const UINT64*>(pLayout + numberOfResources);
    auto pNumRows = reinterpret_cast<const UINT*>(pRowSizesInBytes + numberOfResources);

    switch (desc.Dimension)
    {
        case D3D12_RESOURCE_DIMENSION_TEXTURE1D:
        {
            TexMetadata mdata;
            mdata.width = static_cast<size_t>(desc.Width);
            mdata.height = mdata.depth = 1;
            mdata.arraySize = desc.DepthOrArraySize;
            mdata.mipLevels = desc.MipLevels;
            mdata.miscFlags = 0;
            mdata.miscFlags2 = 0;
            mdata.format = desc.Format;
            mdata.dimension = TEX_DIMENSION_TEXTURE1D;

            hr = result.Initialize(mdata);
            if (FAILED(hr))
                return hr;
        }
        break;

    case D3D12_RESOURCE_DIMENSION_TEXTURE2D:
        {
            TexMetadata mdata;
            mdata.width = static_cast<size_t>(desc.Width);
            mdata.height = desc.Height;
            mdata.depth = 1;
            mdata.arraySize = desc.DepthOrArraySize;
            mdata.mipLevels = desc.MipLevels;
            mdata.miscFlags = isCubeMap ? TEX_MISC_TEXTURECUBE : 0;
            mdata.miscFlags2 = 0;
            mdata.format = desc.Format;
            mdata.dimension = TEX_DIMENSION_TEXTURE2D;

            hr = result.Initialize(mdata);
            if (FAILED(hr))
                return hr;
        }
        break;

    case D3D12_RESOURCE_DIMENSION_TEXTURE3D:
        {
            TexMetadata mdata;
            mdata.width = static_cast<size_t>(desc.Width);
            mdata.height = desc.Height;
            mdata.depth = desc.DepthOrArraySize;
            mdata.arraySize = 1;
            mdata.mipLevels = desc.MipLevels;
            mdata.miscFlags = 0;
            mdata.miscFlags2 = 0;
            mdata.format = desc.Format;
            mdata.dimension = TEX_DIMENSION_TEXTURE3D;

            hr = result.Initialize(mdata);
            if (FAILED(hr))
                return hr;
        }
        break;

    default:
        return E_FAIL;
    }

    BYTE* pData;
    hr = pStaging->Map(0, nullptr, reinterpret_cast<void**>(&pData));
    if (FAILED(hr))
    {
        result.Release();
        return E_FAIL;
    }

    UINT arraySize, depth;
    if (desc.Dimension == D3D12_RESOURCE_DIMENSION_TEXTURE3D)
    {
        arraySize = 1;
        depth = desc.DepthOrArraySize;
    }
    else
    {
        arraySize = desc.DepthOrArraySize;
        depth = 1;
    }

    for (UINT plane = 0; plane < numberOfPlanes; ++plane)
    {
        for (UINT item = 0; item < arraySize; ++item)
        {
            for (UINT level = 0; level < desc.MipLevels; ++level)
            {
                UINT dindex = D3D12CalcSubresource(level, item, plane, desc.MipLevels, arraySize);
                assert(dindex < numberOfResources);

                const Image* img = result.GetImage(level, item, 0);
                if (!img)
                {
                    pStaging->Unmap(0, nullptr);
                    result.Release();
                    return E_FAIL;
                }

                if (!img->pixels)
                {
                    pStaging->Unmap(0, nullptr);
                    result.Release();
                    return E_POINTER;
                }

                D3D12_MEMCPY_DEST destData = { img->pixels, img->rowPitch, img->slicePitch };

                AdjustPlaneResource(img->format, img->height, plane, destData);

                D3D12_SUBRESOURCE_DATA srcData =
                {
                    pData + pLayout[dindex].Offset,
                    static_cast<LONG_PTR>(pLayout[dindex].Footprint.RowPitch),
                    static_cast<LONG_PTR>(pLayout[dindex].Footprint.RowPitch * pNumRows[dindex])
                };

                if (pRowSizesInBytes[dindex] > (SIZE_T)-1)
                {
                    pStaging->Unmap(0, nullptr);
                    result.Release();
                    return E_FAIL;
                }

                MemcpySubresource(&destData, &srcData,
                    (SIZE_T)pRowSizesInBytes[dindex],
                    pNumRows[dindex],
                    pLayout[dindex].Footprint.Depth);
            }
        }
    }

    pStaging->Unmap(0, nullptr);

    return S_OK;
}
