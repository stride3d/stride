using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Tools;

public static partial class Spv
{
    struct DisData(NewSpirvBuffer buffer, bool useNames, bool writeToConsole)
    {
        static int MAX_OFFSET = 16;
        SortedList<int, string> nameTable = [];
        NewSpirvBuffer buffer = buffer;
        int idOffset;
        bool useNames = useNames;
        bool writeToConsole = writeToConsole;

        void ComputeIdOffset()
        {
            idOffset = 9;
            if (!useNames)
            {
                var bound = buffer.Header.Bound;
                idOffset = 3;
                while (bound > 0)
                {
                    bound /= 10;
                    idOffset += 1;
                }
            }
            else
            {
                var maxName = 0;
                foreach (var i in buffer)
                {
                    maxName = i.Op switch
                    {
                        Op.OpName => maxName > ((OpName)i).Name.Length ? maxName : ((OpName)i).Name.Length,
                        Op.OpMemberName => maxName > ((OpMemberName)i).Name.Length ? maxName : ((OpMemberName)i).Name.Length,
                        _ => maxName
                    };
                }
                idOffset += maxName;
            }
            idOffset = Math.Min(idOffset, MAX_OFFSET);
        }
    }


    public static string Dis(NewSpirvBuffer buffer, bool useNames = true, bool writeToConsole = true)
    {
        // this.buffer = buffer;
        // ComputeIdOffset();
        // Assembly code generation logic goes here
        var data = new DisData(buffer, useNames, writeToConsole);
        return "";
    }
}