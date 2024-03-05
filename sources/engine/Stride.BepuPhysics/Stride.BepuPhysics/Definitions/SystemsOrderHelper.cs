namespace Stride.BepuPhysics.Definitions;

internal static class SystemsOrderHelper
{

    //Note : transform processor's Draw() is at -200;

    public const int ORDER_OF_CONSTRAINT_P = -900; //handle the creation of bepu constraints
    public const int ORDER_OF_GAME_SYSTEM = -810; //Handle the simulation Step(dt) + Transform update
    public const int ORDER_OF_DEBUG_P = 1000; // Handles drawing debug wireframe, transform processor's Draw operates at -200, as long as we're after it we're working optimally
    public const int ORDER_OF_COLLIDABLE_P = -100; // Handles the creation of bepu objects and rebuilds static meshes when they are moved, needs to occur after TransformProcessor's draw
}
