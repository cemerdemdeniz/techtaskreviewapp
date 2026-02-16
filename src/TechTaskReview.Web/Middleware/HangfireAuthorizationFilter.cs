using Hangfire.Dashboard;

namespace TechTaskReview.Web.Middleware;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        // In development, allow all access. In production, check for Admin role.
        if (httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            return true;
        return httpContext.User.IsInRole("Admin");
    }
}
