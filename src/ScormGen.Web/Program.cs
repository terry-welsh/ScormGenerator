using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using ScormGen.Core.Loading;
using ScormGen.Core.Packaging;
using ScormGen.Web.Components;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMemoryCache();

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("generate", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));
    options.AddPolicy("download", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.WebHost.ConfigureKestrel(o =>
    o.Limits.MaxRequestBodySize = 16 * 1024 * 1024);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; " +
        "connect-src 'self' wss: ws:; img-src 'self' data:; font-src 'self'; frame-ancestors 'self';");
    await next();
});

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAntiforgery();
app.UseStaticFiles(); // serves wwwroot files not in the MapStaticAssets manifest (e.g. build-generated tailwind.css)
app.MapStaticAssets();

app.MapPost("/generate", async (HttpContext ctx, ILogger<Program> logger) =>
{
    var request = ctx.Request;
    if (!request.HasFormContentType)
        return Results.BadRequest("Expected multipart/form-data.");

    var form = await request.ReadFormAsync();
    var file = form.Files["course"];
    if (file is null)
        return Results.BadRequest("Missing 'course' field.");

    try
    {
        using var reader = new StreamReader(file.OpenReadStream());
        var json = await reader.ReadToEndAsync();
        var course = CourseLoader.Load(json);
        var zipBytes = ScormPackager.PackageCourse(course);

        ctx.Response.Headers["X-Package-Count"] = course.Packages.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return Results.File(zipBytes, "application/zip", "scorm_packages.zip");
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Package generation failed for uploaded file '{FileName}'", file.FileName);
        return Results.StatusCode(500);
    }
// DisableAntiforgery: this endpoint accepts multipart uploads from both the Blazor
// UI and external integration-test clients, neither of which attach Blazor antiforgery tokens.
}).DisableAntiforgery().RequireRateLimiting("generate");

app.MapGet("/download/{id}", (string id, IMemoryCache cache) =>
{
    if (!cache.TryGetValue($"dl_{id}", out byte[]? bytes) || bytes is null)
        return Results.NotFound();
    cache.Remove($"dl_{id}");
    return Results.File(bytes, "application/zip", "scorm_packages.zip");
}).RequireRateLimiting("download");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program { }
