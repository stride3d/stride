using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Stride.Metrics.ServerApp.Models;

namespace Stride.Metrics.ServerApp.Controllers;

public class HomeController : MetricsControllerBase
{
    public HomeController(ILogger<HomeController> logger, IHttpContextAccessor httpContextAccessor)
        : base(logger, httpContextAccessor)
    {
    }

    public IActionResult Index()
    {
        _logger.LogInformation($"Metrics dashboard view from {GetIPAddress()}");
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
