using Assimp;
using Assimp.Unmanaged;
using Assimp.Configs;

namespace Stride.Impoort.FBX
{
    internal class Program
    {
        static void Main(string[] args)
        {
            FBXImporter importer=new FBXImporter();
            importer.RUN();
            Console.WriteLine("Hello, World!");
            Console.ReadLine();
        }

        
       


    }

    public class FBXImporter
    {


       public void TraverseNodes(Node node, int depth, List<Node> outNodesList)
        {
            outNodesList??=new List<Node>();
            outNodesList.Add(node);
            foreach (Node childNode in node.Children)
            {
                TraverseNodes(childNode, depth + 1, outNodesList);
            }
        }


        public void RUN()
        {
            AssimpContext importer = new AssimpContext();
            var filePath = "C:\\Users\\Shadow\\Desktop\\testfiles\\testfiles.fbx";
            Scene scene = importer.ImportFile(filePath, PostProcessSteps.PreTransformVertices);


            // Check if the import was successful
            if (scene == null || scene.RootNode == null)
            {
                
                return;
            }



            foreach (var mesh in scene.Meshes)
            {
                Console.WriteLine($"Mesh Name: {mesh.Name}");

                // Retrieve the material index assigned to this mesh
                int materialIndex = mesh.MaterialIndex;

                // Check if the material index is valid
                if (materialIndex >= 0 && materialIndex < scene.Materials.Count)
                {
                    // Get the material assigned to this mesh
                    Material material = scene.Materials[materialIndex];
                    Console.WriteLine($"Material Name: {material.Name}");

                    // You can access other properties of the material here
                    // For example, diffuse color, specular color, textures, etc.
                }
                else
                {
                    Console.WriteLine("No material assigned to this mesh.");
                }
            }


            foreach (Material material in scene.Materials)
            {
                Console.WriteLine("Material Name: " + material.Name);

                // You can access other properties of the material here
                // For example, diffuse color, specular color, textures, etc.
            }

            List<Node> nodes = new List<Node>();    
            // Recursively traverse the scene graph starting from the root node
            TraverseNodes(scene.RootNode, 0, nodes);
        }
    }
}
