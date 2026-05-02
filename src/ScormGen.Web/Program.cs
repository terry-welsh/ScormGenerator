using Microsoft.Extensions.Caching.Memory;
using ScormGen.Core.Loading;
using ScormGen.Core.Packaging;
using ScormGen.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMemoryCache();
builder.Services.AddScoped<CourseLoader>();
builder.Services.AddScoped<ScormPackager>();

builder.WebHost.ConfigureKestrel(o =>
    o.Limits.MaxRequestBodySize = 16 * 1024 * 1024);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();

app.MapPost("/generate", async (HttpContext ctx, CourseLoader loader, ScormPackager packager) =>
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
        var course = loader.Load(json);
        var zipBytes = packager.PackageCourse(course);

        ctx.Response.Headers["X-Package-Count"] = course.Packages.Count.ToString();
        return Results.File(zipBytes, "application/zip", "scorm_packages.zip");
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
    catch (Exception)
    {
        return Results.StatusCode(500);
    }
}).DisableAntiforgery();

app.MapGet("/download/{id}", (string id, IMemoryCache cache) =>
{
    if (!cache.TryGetValue($"dl_{id}", out byte[]? bytes) || bytes is null)
        return Results.NotFound();
    cache.Remove($"dl_{id}");
    return Results.File(bytes, "application/zip", "scorm_packages.zip");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program { }
