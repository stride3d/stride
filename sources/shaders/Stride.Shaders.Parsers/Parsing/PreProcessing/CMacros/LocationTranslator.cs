using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;

namespace Stride.Shaders.Parsing.SDSL.PreProcessing.Macros;


public class LocationTranslator(Memory<char> origin, Memory<char> processed)
{
    public List<TextLink> Links { get; set; } = [];
    public Memory<char> Origin { get; set; } = origin;
    public Memory<char> Processed { get; set; } = processed;


    /// <summary>
    /// Gets the list of text locations that translate the range chosen to the original file.
    /// </summary>
    /// <value></value>
    public List<Range> this[Range range]
    {
        get
        {

            var result = new List<Range>();

            foreach (var link in Links)
            {
                if (link.Processed.Intersect(range, Processed.Length))
                {
                    var (start, length) = link.Origin.GetOffsetAndLength(Origin.Length);
                    result.Add(new Range(start, length));
                }
            }

            return result;
        }
    }
}
