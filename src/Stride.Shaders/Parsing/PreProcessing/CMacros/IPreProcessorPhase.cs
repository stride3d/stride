namespace Stride.Shaders.Parsing.SDSL.PreProcessing.Macros;

public interface IPreProcessorPhase
{
    SDSLPreProcessor Apply(SDSLPreProcessor sdslpp);
}
