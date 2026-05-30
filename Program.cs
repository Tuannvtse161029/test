using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using ScopusSwaggerTester;

var builder = WebApplication.CreateBuilder(args);

// Bind dynamically to Render's PORT environment variable
var port = Environment.GetEnvironmentVariable("PORT") ?? "5123";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Add services to the container.
builder.Services.AddHttpClient<ScopusService>();
builder.Services.AddOpenApi();

// Add CORS policy to allow local testing
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");

// Serve static files from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

// Expose OpenAPI endpoint in all environments to enable the custom Swagger tester
app.MapOpenApi();

// Scopus Gateway Endpoints
var scopusGroup = app.MapGroup("/api/scopus")
                     .WithGroupName("Scopus API Gateway")
                     .WithOpenApi();

scopusGroup.MapGet("/search", async (string query, int? count, int? start, ScopusService scopusService) =>
{
    try
    {
        var jsonResult = await scopusService.SearchScopusAsync(query, count ?? 25, start ?? 0);
        return Results.Content(jsonResult, "application/json");
    }
    catch (System.Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
})
.WithName("SearchScopus")
.WithDescription("Search the Scopus document index using complex query syntax.");

scopusGroup.MapGet("/author-search", async (string query, int? count, int? start, ScopusService scopusService) =>
{
    try
    {
        var jsonResult = await scopusService.SearchAuthorAsync(query, count ?? 25, start ?? 0);
        return Results.Content(jsonResult, "application/json");
    }
    catch (System.Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
})
.WithName("SearchAuthor")
.WithDescription("Search Elsevier's author profile registry.");

scopusGroup.MapGet("/author/{authorId}", async (string authorId, ScopusService scopusService) =>
{
    try
    {
        var jsonResult = await scopusService.GetAuthorDetailsAsync(authorId);
        return Results.Content(jsonResult, "application/json");
    }
    catch (System.Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
})
.WithName("GetAuthorDetails")
.WithDescription("Retrieve comprehensive academic profile metrics for a specific Author ID.");

scopusGroup.MapGet("/abstract/{scopusId}", async (string scopusId, string? view, ScopusService scopusService) =>
{
    try
    {
        var jsonResult = await scopusService.GetAbstractAsync(scopusId, view ?? "META_ABS");
        return Results.Content(jsonResult, "application/json");
    }
    catch (System.Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
})
.WithName("GetAbstract")
.WithDescription("Retrieve abstract metadata, metrics, and classification for a Scopus Document ID.");

scopusGroup.MapGet("/abstract-by-doi", async (string doi, string? view, ScopusService scopusService) =>
{
    try
    {
        var jsonResult = await scopusService.GetAbstractByDoiAsync(doi, view ?? "META_ABS");
        return Results.Content(jsonResult, "application/json");
    }
    catch (System.Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
})
.WithName("GetAbstractByDoi")
.WithDescription("Retrieve abstract metadata and metrics for an article using its digital object identifier (DOI).");

scopusGroup.MapGet("/serial-title", async (string? title, string? issn, string? view, HttpContext httpContext, ScopusService scopusService) =>
{
    try
    {
        string? instToken = httpContext.Request.Headers["X-ELS-Insttoken"];
        var jsonResult = await scopusService.GetSerialTitleAsync(title, issn, view ?? "CITESCORE", instToken);
        return Results.Content(jsonResult, "application/json");
    }
    catch (System.Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
})
.WithName("GetSerialTitle")
.WithDescription("Retrieve journal/serial source metadata including CiteScore, SJR, and SNIP metrics.");

// Fallback to index.html for client side routing
app.MapFallbackToFile("index.html");

app.Run();
