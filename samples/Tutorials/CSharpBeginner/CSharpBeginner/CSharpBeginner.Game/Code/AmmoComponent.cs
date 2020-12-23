using Stride.Engine;

namespace CSharpBeginner.Code
{
    /// <summary>
    /// This script is used in combination with the GettingAComponent.cs script 
    /// </summary>
    public class AmmoComponent : StartupScript
    {
        private readonly int maxBullets = 30;
        private readonly int currentBullets = 12;

        public override void Start() { }

        public int GetRemainingAmmo()
        {
            return maxBullets - currentBullets;
        }
    }
}
