using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Stride.Metrics.ServerApp.Extensions;
using Stride.Metrics.ServerApp.Models;

namespace Stride.Metrics.ServerApp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HomeController(ILogger<HomeController> logger, IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public IActionResult Index()
    {
        var ipAddress = _httpContextAccessor.GetIPAddress();
        _logger.LogInformation($"Metrics dashboard view from {ipAddress}");
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
