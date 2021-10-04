using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.Assimp;
using Stride.Core.Mathematics;

namespace Stride.Importer.Assimp.Material
{
    public static unsafe class Materials
    {
        public const string MatKeyTexTypeBase = "$tex.type";
        public const string MatKeyTexColorBase = "$tex.color";
        public const string MatKeyTexAlphaBase = "$tex.alpha";

        /// <summary>
        /// Converts an Assimp's material stack operation From c++ to c#.
        /// </summary>
        public static readonly Operation[] ConvertAssimpStackOperationCppToCs = new Operation[]
        {
            Operation.Add,			//aiStackOperation_Add
		    Operation.Add3ds,		//aiStackOperation_Add3ds
		    Operation.AddMaya,		//aiStackOperation_AddMaya
		    Operation.Average,		//aiStackOperation_Average
		    Operation.Color,			//aiStackOperation_Color
		    Operation.ColorBurn,		//aiStackOperation_ColorBurn
		    Operation.ColorDodge,	//aiStackOperation_ColorDodge
		    Operation.Darken3ds,		//aiStackOperation_Darken3ds
		    Operation.DarkenMaya,	//aiStackOperation_DarkenMaya
		    Operation.Desaturate,	//aiStackOperation_Desaturate
		    Operation.Difference3ds,	//aiStackOperation_Difference3ds
		    Operation.DifferenceMaya,//aiStackOperation_DifferenceMaya
		    Operation.Divide,		//aiStackOperation_Divide
		    Operation.Exclusion,		//aiStackOperation_Exclusion
		    Operation.HardLight,		//aiStackOperation_HardLight
		    Operation.HardMix,		//aiStackOperation_HardMix
		    Operation.Hue,			//aiStackOperation_Hue
		    Operation.Illuminate,	//aiStackOperation_Illuminate
		    Operation.In,			//aiStackOperation_In
		    Operation.Lighten3ds,	//aiStackOperation_Lighten3ds
		    Operation.LightenMaya,	//aiStackOperation_LightenMaya
		    Operation.LinearBurn,	//aiStackOperation_LinearBurn
		    Operation.LinearDodge,	//aiStackOperation_LinearDodge
		    Operation.Multiply3ds,	//aiStackOperation_Multiply3ds
		    Operation.MultiplyMaya,	//aiStackOperation_MultiplyMaya
		    Operation.None,			//aiStackOperation_None
		    Operation.Out,			//aiStackOperation_Out
		    Operation.Over3ds,		//aiStackOperation_Over3ds
		    Operation.Overlay3ds,	//aiStackOperation_Overlay3ds
		    Operation.OverMaya,		//aiStackOperation_OverMaya
		    Operation.PinLight,		//aiStackOperation_PinLight
		    Operation.Saturate,		//aiStackOperation_Saturate
		    Operation.Saturation,	//aiStackOperation_Saturation
		    Operation.Screen,		//aiStackOperation_Screen
		    Operation.SoftLight,		//aiStackOperation_SoftLight
		    Operation.Substract3ds,	//aiStackOperation_Substract3ds
		    Operation.SubstractMaya,	//aiStackOperation_SubstractMaya
		    Operation.Value,			//aiStackOperation_Value
		    Operation.Mask           //aiStackOperation_Mask
        };

        /// <summary>
        /// Converts an Assimp's material stack node type From c++ to c#.
        /// </summary>
        public static readonly StackType[] ConvertAssimpStackTypeCppToCs = new StackType[]
        {
            StackType.Color,			// aiStackType_ColorType
		    StackType.Texture,		// aiStackType_TextureType
		    StackType.Operation      // aiStackType_BlemdOpType
        };

        public static readonly MappingMode[] ConvertAssimpMappingModeCppToCs = new MappingMode[]
        {
            MappingMode.Wrap,		// aiTextureMapMode_Wrap
		    MappingMode.Clamp,		// aiTextureMapMode_Clamp
		    MappingMode.Mirror,		// aiTextureMapMode_Mirror
		    MappingMode.Decal        // aiTextureMapMode_Decal
        };

        public static unsafe Stack ConvertAssimpStackCppToCs(Silk.NET.Assimp.Assimp assimp, Silk.NET.Assimp.Material* material, Silk.NET.Assimp.TextureType type)
        {
            var ret = new Stack();
            var count = (int)assimp.GetMaterialTextureCount(material, type);

            // Process the material stack
            for (int iEl = count - 1; iEl >= 0; --iEl)
            {
                StackElement el;
                // Common properties
                int elType = 0, elFlags = 0;
                float elAlpha = 0.0f, elBlend = 0.0f;
                // Operation-specific properties
                int elOp = 0;
                // Color-specific properties
                var elColor = new System.Numerics.Vector4();
                // Texture-specific properties
                var elTexPath = new AssimpString();
                int elTexChannel = 0, elMappingModeU = 0, elMappingModeV = 0;
                uint pMax = 0;

                if (assimp.GetMaterialFloatArray(material, MatKeyTexAlphaBase, (uint)type, (uint)iEl, ref elAlpha, ref pMax) != Return.ReturnSuccess)
                    elAlpha = 1.0f; // default alpha
                if (assimp.GetMaterialFloatArray(material, Silk.NET.Assimp.Assimp.MaterialTexblendBase, (uint)type, (uint)iEl, ref elBlend, ref pMax) != Return.ReturnSuccess)
                    elBlend = 1.0f; // default blend
                if (assimp.GetMaterialIntegerArray(material, Silk.NET.Assimp.Assimp.MaterialTexflagsBase, (uint)type, (uint)iEl, ref elFlags, ref pMax) != Return.ReturnSuccess)
                    elFlags = 0; // default flags (no flags)
                if (assimp.GetMaterialIntegerArray(material, MatKeyTexTypeBase, (uint)type, (uint)iEl, ref elType, ref pMax) != Return.ReturnSuccess)
                    elType = (int)StackType.Texture;//continue; // error !

                switch ((StackType)elType)
                {
                    case StackType.Operation:
                        if (assimp.GetMaterialIntegerArray(material, Silk.NET.Assimp.Assimp.MaterialTexopBase, (uint)type, (uint)iEl, ref elOp, ref pMax) != Return.ReturnSuccess)
                            continue; // error !

                        el = new StackOperation(ConvertAssimpStackOperationCppToCs[elOp], elAlpha, elBlend, elFlags);
                        break;
                    case StackType.Color:
                        if (assimp.GetMaterialColor(material, MatKeyTexColorBase, (uint)type, (uint)iEl, ref elColor) != Return.ReturnSuccess)
                            continue; // error !
                        el = new StackColor(new Color3(elColor.X, elColor.Y, elColor.Z), elAlpha, elBlend, elFlags);
                        break;
                    case StackType.Texture:
                        if (assimp.GetMaterialString(material, Silk.NET.Assimp.Assimp.MaterialTextureBase, (uint)type, (uint)iEl, ref elTexPath) != Return.ReturnSuccess)
                            continue; // error !
                        if (assimp.GetMaterialIntegerArray(material, Silk.NET.Assimp.Assimp.MaterialUvwsrcBase, (uint)type, (uint)iEl, ref elTexChannel, ref pMax) != Return.ReturnSuccess)
                            elTexChannel = 0; // default channel
                        if (assimp.GetMaterialIntegerArray(material, Silk.NET.Assimp.Assimp.MaterialMappingmodeUBase, (uint)type, (uint)iEl, ref elMappingModeU, ref pMax) != Return.ReturnSuccess)
                            elMappingModeU = (int)TextureMapMode.TextureMapModeWrap; // default mapping mode
                        if (assimp.GetMaterialIntegerArray(material, Silk.NET.Assimp.Assimp.MaterialMappingmodeVBase, (uint)type, (uint)iEl, ref elMappingModeV, ref pMax) != Return.ReturnSuccess)
                            elMappingModeV = (int)TextureMapMode.TextureMapModeWrap; // default mapping mode

                        el = new StackTexture(
                            elTexPath.AsString,
                            elTexChannel,
                            ConvertAssimpMappingModeCppToCs[elMappingModeU],
                            ConvertAssimpMappingModeCppToCs[elMappingModeV],
                            elAlpha,
                            elBlend,
                            elFlags);
                        break;
                    default:
                        // error !
                        continue;
                }

                ret.Push(el);
            }

            return ret;
        }
    }
}
