using Microsoft.Extensions.Logging;

namespace Stride.Shaders.Parsing.LSP;


internal class Foo(ILogger<Foo> logger)
{
    private readonly ILogger<Foo> _logger = logger;

    public void SayFoo()
    {
        _logger.LogInformation("Fooooo!");
    }
}