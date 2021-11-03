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
        public static unsafe ComPtr<ID3D11BlendState> ToBlendState(ComPtr<IUnknown> source)
        {
            ID3D11BlendState* result = null;
            SilkMarshal.ThrowHResult(source.Get().QueryInterface(SilkMarshal.GuidPtrOf<ID3D11BlendState>(), (void**)&result));
            return new ComPtr<ID3D11BlendState>(result);
        }
        public static unsafe ComPtr<ID3D11RasterizerState> ToRasterizeState(ComPtr<IUnknown> source)
        {
            ID3D11RasterizerState* result = null;
            SilkMarshal.ThrowHResult(source.Get().QueryInterface(SilkMarshal.GuidPtrOf<ID3D11RasterizerState>(), (void**)&result));
            return new ComPtr<ID3D11RasterizerState>(result);
        }
        public static unsafe ComPtr<ID3D11DepthStencilState> ToDepthStencilState(ComPtr<IUnknown> source)
        {
            ID3D11DepthStencilState* result = null;
            SilkMarshal.ThrowHResult(source.Get().QueryInterface(SilkMarshal.GuidPtrOf<ID3D11DepthStencilState>(), (void**)&result));
            return new ComPtr<ID3D11DepthStencilState>(result);
        }
        public static unsafe ComPtr<ID3D11VertexShader> ToVSShader(ComPtr<IUnknown> source)
        {
            ID3D11VertexShader* result = null;
            SilkMarshal.ThrowHResult(source.Get().QueryInterface(SilkMarshal.GuidPtrOf<ID3D11VertexShader>(), (void**)&result));
            return new ComPtr<ID3D11VertexShader>(result);
        }
        public static unsafe ComPtr<ID3D11ComputeShader> ToCSShader(ComPtr<IUnknown> source)
        {
            ID3D11ComputeShader* result = null;
            SilkMarshal.ThrowHResult(source.Get().QueryInterface(SilkMarshal.GuidPtrOf<ID3D11ComputeShader>(), (void**)&result));
            return new ComPtr<ID3D11ComputeShader>(result);
        }
        public static unsafe ComPtr<ID3D11GeometryShader> ToGSShader(ComPtr<IUnknown> source)
        {
            ID3D11GeometryShader* result = null;
            SilkMarshal.ThrowHResult(source.Get().QueryInterface(SilkMarshal.GuidPtrOf<ID3D11GeometryShader>(), (void**)&result));
            return new ComPtr<ID3D11GeometryShader>(result);
        }
        public static unsafe ComPtr<ID3D11PixelShader> ToPSShader(ComPtr<IUnknown> source)
        {
            ID3D11PixelShader* result = null;
            SilkMarshal.ThrowHResult(source.Get().QueryInterface(SilkMarshal.GuidPtrOf<ID3D11PixelShader>(), (void**)&result));
            return new ComPtr<ID3D11PixelShader>(result);
        }
        public static unsafe ComPtr<ID3D11HullShader> ToHSShader(ComPtr<IUnknown> source)
        {
            ID3D11HullShader* result = null;
            SilkMarshal.ThrowHResult(source.Get().QueryInterface(SilkMarshal.GuidPtrOf<ID3D11HullShader>(), (void**)&result));
            return new ComPtr<ID3D11HullShader>(result);
        }
        public static unsafe ComPtr<ID3D11DomainShader> ToDSShader(ComPtr<IUnknown> source)
        {
            ID3D11DomainShader* result = null;
            SilkMarshal.ThrowHResult(source.Get().QueryInterface(SilkMarshal.GuidPtrOf<ID3D11DomainShader>(), (void**)&result));
            return new ComPtr<ID3D11DomainShader>(result);
        }
    }
}
