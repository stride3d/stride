namespace CSharpIntermediate
{
    class CSharpIntermediateApp
    {
        static void Main(string[] args)
        {
            using (var game = new Stride.Engine.Game())
            {
                game.Run();
            }
        }
    }
}
