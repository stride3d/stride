using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Mathematics;
using Xenko.Engine;

namespace CSharpBeginner.Code
{
    /// <summary>
    /// This script is used to demonstrate how we can get child entities of an entity
    /// </summary>
    public class ChildEntitiesDemo : SyncScript
    {
        Entity child0;
        Entity child1;
        List<Entity> children;

        public override void Start()
        {
            //We can get a child by using GetChild(). This takes an index number starting at 0
            child0 = Entity.GetChild(0);
            child1 = Entity.GetChild(1);
            
            //If we would try to get Child 3 (which doesn't exist), we would get an exception
            //var nonExistinGChild = Entity.GetChild(2); 

            //We retrieve all children from our entity and store it in a list. 
            //NOTE: This does not include any subchildren of those children
            children = Entity.GetChildren().ToList();
        }

        public override void Update()
        {
            //We store some drawing positions
            int drawX = 350, drawY = 230, increment = 70;

            //We print the name of the our entity
            DebugText.Print(Entity.Name, new Int2(drawX, drawY));

            //we loop over all the children that we have found and display their name
            foreach (var child in children)
            {
                //We print the name of the child
                drawY += increment;
                DebugText.Print(child.Name, new Int2(drawX + increment, drawY));

                //It is possible that this child, also has children. We retrieve them, loop over them and print their name too
                var subChildren = child.GetChildren().ToList();
                foreach (var subChild in subChildren)
                {
                    drawY += increment;
                    DebugText.Print(subChild.Name, new Int2(drawX + (increment * 2), drawY));
                }
            }
        }
    }
}
