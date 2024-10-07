using Stride.Shaders.Spirv.Core;

namespace Stride.Shaders.Spirv;


public partial class Mixer
{
    public ref struct Inheritance
    {
        Mixer mixer;
        public Inheritance(Mixer mixer)
        {
            this.mixer = mixer;
        }

        public Inheritance Inherit(string name)
        {
            mixer.Inherit(name);
            return this;
        }
        public Mixer FinishInherit()
        {
            return mixer;
        }
    }
}