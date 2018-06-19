//-------------------------------------------------------------------------------------
// BCDirectCompute.h
//  
// Direct3D 11 Compute Shader BC Compressor
//
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//-------------------------------------------------------------------------------------

#pragma once

namespace DirectX
{

class GPUCompressBC
{
public:
    GPUCompressBC();

    HRESULT Initialize( _In_ ID3D11Device* pDevice );

    HRESULT Prepare( size_t width, size_t height, DWORD flags, DXGI_FORMAT format, float alphaWeight );

    HRESULT Compress( const Image& srcImage, const Image& destImage );

    DXGI_FORMAT GetSourceFormat() const { return m_srcformat; }

private:
    DXGI_FORMAT                                         m_bcformat;
    DXGI_FORMAT                                         m_srcformat;
    float                                               m_alphaWeight;
    bool                                                m_bc7_mode02;
    bool                                                m_bc7_mode137;
    size_t                                              m_width;
    size_t                                              m_height;

    Microsoft::WRL::ComPtr<ID3D11Device>                m_device;
    Microsoft::WRL::ComPtr<ID3D11DeviceContext>         m_context;

    Microsoft::WRL::ComPtr<ID3D11Buffer>                m_err1;
    Microsoft::WRL::ComPtr<ID3D11UnorderedAccessView>   m_err1UAV;
    Microsoft::WRL::ComPtr<ID3D11ShaderResourceView>    m_err1SRV;

    Microsoft::WRL::ComPtr<ID3D11Buffer>                m_err2;
    Microsoft::WRL::ComPtr<ID3D11UnorderedAccessView>   m_err2UAV;
    Microsoft::WRL::ComPtr<ID3D11ShaderResourceView>    m_err2SRV;

    Microsoft::WRL::ComPtr<ID3D11Buffer>                m_output;
    Microsoft::WRL::ComPtr<ID3D11Buffer>                m_outputCPU;
    Microsoft::WRL::ComPtr<ID3D11UnorderedAccessView>   m_outputUAV;
    Microsoft::WRL::ComPtr<ID3D11Buffer>                m_constBuffer;
    
    // Compute shader library
    Microsoft::WRL::ComPtr<ID3D11ComputeShader>         m_BC6H_tryModeG10CS;
    Microsoft::WRL::ComPtr<ID3D11ComputeShader>         m_BC6H_tryModeLE10CS;
    Microsoft::WRL::ComPtr<ID3D11ComputeShader>         m_BC6H_encodeBlockCS;

    Microsoft::WRL::ComPtr<ID3D11ComputeShader>         m_BC7_tryMode456CS;
    Microsoft::WRL::ComPtr<ID3D11ComputeShader>         m_BC7_tryMode137CS;
    Microsoft::WRL::ComPtr<ID3D11ComputeShader>         m_BC7_tryMode02CS;
    Microsoft::WRL::ComPtr<ID3D11ComputeShader>         m_BC7_encodeBlockCS;    
};

}; // namespace