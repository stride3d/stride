using MySurvivalGame.Game.Weapons; // For BaseWeapon or BaseRangedWeapon
using Stride.Engine;

namespace MySurvivalGame.Game.Weapons.Ranged
{
    public abstract class BaseBowWeapon : BaseRangedWeapon // Or BaseWeapon if BaseRangedWeapon isn't suitable for bows
    {
        protected bool isDrawing = false;
        protected float drawStartTime = 0f;

        public virtual void StartDraw()
        {
            isDrawing = true;
            drawStartTime = (float)Game.UpdateTime.Total.TotalSeconds;
            Log.Info($"{Entity.Name}: Bow draw started.");
            // Future: Play draw animation, sound
        }

        public virtual void CancelDraw()
        {
            if (!isDrawing) return;
            isDrawing = false;
            Log.Info($"{Entity.Name}: Bow draw cancelled.");
            // Future: Revert to idle animation
        }

        // Called by PlayerEquipment when shoot button is released
        public override void OnPrimaryActionReleased() 
        {
            if (isDrawing)
            {
                float chargeTime = (float)Game.UpdateTime.Total.TotalSeconds - drawStartTime;
                ReleaseArrow(chargeTime);
                isDrawing = false;
            }
        }

        protected abstract void ReleaseArrow(float chargeTime);

        // Override PrimaryAction to handle draw start
        public override void PrimaryAction()
        {
            StartDraw();
        }
    }
}
