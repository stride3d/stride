using Microsoft.AspNetCore.Mvc;

namespace Stride.Metrics.ServerApp.Controllers;

public class MetricsControllerBase : Controller
{
    protected readonly ILogger<HomeController> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MetricsControllerBase(ILogger<HomeController> logger, IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }
    protected string GetIPAddress(HttpContext context = null)
    {
        try
        {
            context ??= _httpContextAccessor.HttpContext;
            return context.Connection.RemoteIpAddress?.ToString();
        }
        catch (Exception)
        {
            return null;
        }
    }
}
