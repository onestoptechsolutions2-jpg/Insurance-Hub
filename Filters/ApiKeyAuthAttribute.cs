using Insurance_Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Insurance_Hub.Filters
{
    /// <summary>
    /// Authenticates a public API endpoint via the "X-Api-Key" header, checked against the
    /// broker's inbound lead API key (managed at /admin/settings). Not [Authorize] — these
    /// endpoints are meant for external, unauthenticated-by-cookie callers.
    /// </summary>
    public class ApiKeyAuthAttribute : Attribute, IAsyncActionFilter
    {
        private const string HeaderName = "X-Api-Key";

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var settingsService = context.HttpContext.RequestServices.GetRequiredService<ISettingsService>();

            if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var providedKey) ||
                string.IsNullOrWhiteSpace(providedKey))
            {
                context.Result = new UnauthorizedObjectResult(new { error = "Missing X-Api-Key header." });
                return;
            }

            var settings = await settingsService.GetAsync();
            if (string.IsNullOrEmpty(settings.LeadApiKey) || providedKey != settings.LeadApiKey)
            {
                context.Result = new UnauthorizedObjectResult(new { error = "Invalid API key." });
                return;
            }

            await next();
        }
    }
}
