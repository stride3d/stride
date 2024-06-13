namespace Stride.Metrics.ServerApp.Extensions;

public static class IHttpContextAccessorExtensions
{
    public static string GetIPAddress(this IHttpContextAccessor httpContextAccessor, HttpContext context = null)
    {
        try
        {
            context ??= httpContextAccessor.HttpContext;
            return context.Connection.RemoteIpAddress?.ToString();
        }
        catch (Exception)
        {
            return null;
        }
    }
}
