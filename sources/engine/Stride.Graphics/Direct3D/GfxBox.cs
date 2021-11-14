using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;

namespace Stride.Graphics
{
    public interface GfxBox<T> : GfxBox
    {
        public T GetData();
        public void Release();
    }
    public interface GfxBox {}

    public class VertexShader : GfxBox<ComPtr<ID3D11VertexShader>>
    {
        ComPtr<ID3D11VertexShader> Shader;

        public VertexShader(ComPtr<ID3D11VertexShader> shader)
        {
            Shader = shader;
        }

        public ComPtr<ID3D11VertexShader> GetData()
        {
            return Shader;
        }

        public void Release()
        {
            Shader.Release();
        }
        public static explicit operator ComPtr<ID3D11VertexShader>(VertexShader vs) => vs.Shader;
    }
    public class PixelShader : GfxBox<ComPtr<ID3D11PixelShader>>
    {
        ComPtr<ID3D11PixelShader> Shader;

        public PixelShader(ComPtr<ID3D11PixelShader> shader)
        {
            Shader = shader;
        }

        public ComPtr<ID3D11PixelShader> GetData()
        {
            return Shader;
        }

        public void Release()
        {
            Shader.Release();
        }
        public static explicit operator ComPtr<ID3D11PixelShader>(PixelShader vs) => vs.Shader;

    }
    public class ComputeShader : GfxBox<ComPtr<ID3D11ComputeShader>>
    {
        ComPtr<ID3D11ComputeShader> Shader;

        public ComputeShader(ComPtr<ID3D11ComputeShader> shader)
        {
            Shader = shader;
        }

        public ComPtr<ID3D11ComputeShader> GetData()
        {
            return Shader;
        }

        public void Release()
        {
            Shader.Release();
        }

        public static explicit operator ComPtr<ID3D11ComputeShader>(ComputeShader vs) => vs.Shader;

    }
    public class HullShader : GfxBox<ComPtr<ID3D11HullShader>>
    {
        ComPtr<ID3D11HullShader> Shader;

        public HullShader(ComPtr<ID3D11HullShader> shader)
        {
            Shader = shader;
        }

        public ComPtr<ID3D11HullShader> GetData()
        {
            return Shader;
        }

        public void Release()
        {
            Shader.Release();
        }

        public static explicit operator ComPtr<ID3D11HullShader>(HullShader vs) => vs.Shader;

    }
    public class GeometryShader : GfxBox<ComPtr<ID3D11GeometryShader>>
    {
        ComPtr<ID3D11GeometryShader> Shader;

        public GeometryShader(ComPtr<ID3D11GeometryShader> shader)
        {
            Shader = shader;
        }

        public ComPtr<ID3D11GeometryShader> GetData()
        {
            return Shader;
        }

        public void Release()
        {
            Shader.Release();
        }
        public static explicit operator ComPtr<ID3D11GeometryShader>(GeometryShader vs) => vs.Shader;

    }
    public class DomainShader : GfxBox<ComPtr<ID3D11DomainShader>>
    {
        ComPtr<ID3D11DomainShader> Shader;

        public DomainShader(ComPtr<ID3D11DomainShader> shader)
        {
            Shader = shader;
        }

        public ComPtr<ID3D11DomainShader> GetData()
        {
            return Shader;
        }

        public void Release()
        {
            Shader.Release();
        }
        public static explicit operator ComPtr<ID3D11DomainShader>(DomainShader vs) => vs.Shader;

    }
    public class BlendState : GfxBox<ComPtr<ID3D11BlendState>>
    {
        ComPtr<ID3D11BlendState> blendState;

        public BlendState(ComPtr<ID3D11BlendState> blendState)
        {
            this.blendState = blendState;
        }

        public ComPtr<ID3D11BlendState> GetData()
        {
            return blendState;
        }

        public void Release()
        {
            blendState.Release();
        }
        public static explicit operator ComPtr<ID3D11BlendState>(BlendState bs) => bs.blendState;

    }
    public class RasterizerState : GfxBox<ComPtr<ID3D11RasterizerState>>
    {
        ComPtr<ID3D11RasterizerState> rasterizerState;

        public RasterizerState(ComPtr<ID3D11RasterizerState> rasterizerState)
        {
            this.rasterizerState = rasterizerState;
        }

        public ComPtr<ID3D11RasterizerState> GetData()
        {
            return rasterizerState;
        }

        public void Release()
        {
            rasterizerState.Release();
        }
        public static explicit operator ComPtr<ID3D11RasterizerState>(RasterizerState rs) => rs.rasterizerState;

    }
    public class DepthStencilState : GfxBox<ComPtr<ID3D11DepthStencilState>>
    {
        ComPtr<ID3D11DepthStencilState> rasterizerState;

        public DepthStencilState(ComPtr<ID3D11DepthStencilState> rasterizerState)
        {
            this.rasterizerState = rasterizerState;
        }

        public ComPtr<ID3D11DepthStencilState> GetData()
        {
            return rasterizerState;
        }

        public void Release()
        {
            rasterizerState.Release();
        }
        public static explicit operator ComPtr<ID3D11DepthStencilState>(DepthStencilState rs) => rs.rasterizerState;

    }
}
