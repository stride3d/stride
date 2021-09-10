using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Direct3D11;
using Silk.NET.Core.Native;

namespace Stride.Graphics.Direct3D.Extensions
{
    public static class DXConvert
    {
        public static unsafe ID3D11RasterizerState ToRasterizeState(IUnknown source)
        {
            var pSource = &source;
            ID3D11RasterizerState* result = null;
            SilkMarshal.ThrowHResult(pSource->QueryInterface(SilkMarshal.GuidPtrOf<ID3D11Resource>(), (void**)&result));
            return *result;
        }
        public static unsafe ID3D11DepthStencilState ToDepthStencilState(IUnknown source)
        {
            var pSource = &source;
            ID3D11DepthStencilState* result = null;
            SilkMarshal.ThrowHResult(pSource->QueryInterface(SilkMarshal.GuidPtrOf<ID3D11Resource>(), (void**)&result));
            return *result;
        }
        public static unsafe ID3D11VertexShader ToVSShader(IUnknown source)
        {
            var pSource = &source;
            ID3D11VertexShader* result = null;
            SilkMarshal.ThrowHResult(pSource->QueryInterface(SilkMarshal.GuidPtrOf<ID3D11Resource>(), (void**)&result));
            return *result;
        }
        public static unsafe ID3D11ComputeShader ToCSShader(IUnknown source)
        {
            var pSource = &source;
            ID3D11ComputeShader* result = null;
            SilkMarshal.ThrowHResult(pSource->QueryInterface(SilkMarshal.GuidPtrOf<ID3D11Resource>(), (void**)&result));
            return *result;
        }
        public static unsafe ID3D11GeometryShader ToGSShader(IUnknown source)
        {
            var pSource = &source;
            ID3D11GeometryShader* result = null;
            SilkMarshal.ThrowHResult(pSource->QueryInterface(SilkMarshal.GuidPtrOf<ID3D11Resource>(), (void**)&result));
            return *result;
        }
        public static unsafe ID3D11PixelShader ToPSShader(IUnknown source)
        {
            var pSource = &source;
            ID3D11PixelShader* result = null;
            SilkMarshal.ThrowHResult(pSource->QueryInterface(SilkMarshal.GuidPtrOf<ID3D11Resource>(), (void**)&result));
            return *result;
        }
        public static unsafe ID3D11HullShader ToHSShader(IUnknown source)
        {
            var pSource = &source;
            ID3D11HullShader* result = null;
            SilkMarshal.ThrowHResult(pSource->QueryInterface(SilkMarshal.GuidPtrOf<ID3D11Resource>(), (void**)&result));
            return *result;
        }
        public static unsafe ID3D11DomainShader ToDSShader(IUnknown source)
        {
            var pSource = &source;
            ID3D11DomainShader* result = null;
            SilkMarshal.ThrowHResult(pSource->QueryInterface(SilkMarshal.GuidPtrOf<ID3D11Resource>(), (void**)&result));
            return *result;
        }
    }
}
