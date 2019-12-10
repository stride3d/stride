using Xenko.Engine;

namespace CSharpBeginner.Code
{
    /// <summary>
    /// This script is used in combination with the GettingAComponent.cs script 
    /// </summary>
    public class AmmoComponent : StartupScript
    {
        private int _clips = 4;
        private int _bullets = 6;

        public override void Start()
        {
        }

        // This method return the total amount of ammo
        public int GetTotalAmmo()
        {
            return _bullets * _clips;
        }
    }
}
