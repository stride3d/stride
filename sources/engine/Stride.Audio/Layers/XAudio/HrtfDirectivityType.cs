namespace Stride.Audio.Layers.XAudio;

// Indicates one of several stock directivity patterns.
public enum HrtfDirectivityType
{
	// The sound emission is in all directions.
    OmniDirectional = 0,
    // The sound emission is a cardiod shape.
    Cardioid,
    // The sound emission is a cone.
    Cone
}