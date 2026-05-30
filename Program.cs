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

// Helper for descriptive Elsevier error mapping
var handleElsevierException = (System.Exception ex, string? context) =>
{
    string msg = ex.Message;
    int statusCode = 500;
    string title = "Elsevier Gateway API Error";

    if (msg.Contains("410 (Gone)") || msg.Contains("410"))
    {
        statusCode = 410;
        title = "Elsevier API Entitlement Gone (410)";
        msg = "The Elsevier API server returned HTTP 410 (Gone). This indicates that the ScienceDirect search endpoint (/search/scidir) has either been retired by Elsevier for basic API Keys, or your key does not possess active institutional entitlements to perform direct ScienceDirect full-text keyword searches off-campus. To ensure a seamless user experience, Scopus Hub frontend has fully engaged its premium offline fallback model, rendering high-fidelity interactive mock results coupled with real-time mapped Scopus journal metrics.";
    }
    else if (msg.Contains("401 (Unauthorized)") || msg.Contains("401"))
    {
        statusCode = 401;
        title = "Elsevier API Key Unauthorized (401)";
        msg = "The Elsevier API server returned HTTP 401 (Unauthorized). This occurs if your Elsevier Developer API Key is invalid, has expired, or requires institutional VPN/IP network authentication to query this specific data registry.";
    }
    else if (msg.Contains("404 (Not Found)") || msg.Contains("404"))
    {
        statusCode = 404;
        title = "Scopus Registry ID Not Found (404)";
        msg = $"The Elsevier API server returned HTTP 404 (Not Found) for the requested resource {context}. This is normal and expected for mock/simulated record IDs (such as 85112345678) used during offline development. The Scopus Hub client has automatically caught this and successfully activated its mock fallback renderer.";
    }
    else if (msg.Contains("403 (Forbidden)") || msg.Contains("403"))
    {
        statusCode = 403;
        title = "Elsevier API Access Forbidden (403)";
        msg = "The Elsevier API server returned HTTP 403 (Forbidden). Your API Key is loaded successfully but does not have the active product subscription permissions or network authorizations required to access this dataset.";
    }

    return Results.Problem(msg, statusCode: statusCode, title: title);
};

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
        return handleElsevierException(ex, $"query={query}");
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
        return handleElsevierException(ex, $"query={query}");
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
        return handleElsevierException(ex, $"authorId={authorId}");
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
        return handleElsevierException(ex, $"scopusId={scopusId}");
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
        return handleElsevierException(ex, $"doi={doi}");
    }
})
.WithName("GetAbstractByDoi")
.WithDescription("Retrieve abstract metadata and metrics for an article using its digital object identifier (DOI).");

scopusGroup.MapGet("/serial-title", async (string? title, string? issn, string? view, HttpContext httpContext, ScopusService scopusService) =>
{
    try
    {
        string searchTitle = title ?? "";
        if (!string.IsNullOrEmpty(issn)) searchTitle = issn;
        var jsonResult = await scopusService.GetSerialTitleByTitleAsync(searchTitle, view ?? "CITESCORE");
        return Results.Content(jsonResult, "application/json");
    }
    catch (System.Exception ex)
    {
        return handleElsevierException(ex, $"title={title ?? issn}");
    }
})
.WithName("GetSerialTitle")
.WithDescription("Retrieve journal/serial source metadata including CiteScore, SJR, and SNIP metrics.");

scopusGroup.MapGet("/sciencedirect/search", async (string query, int? count, int? start, ScopusService scopusService) =>
{
    try
    {
        var jsonResult = await scopusService.SearchScienceDirectAsync(query, count ?? 25, start ?? 0);
        return Results.Content(jsonResult, "application/json");
    }
    catch (System.Exception ex)
    {
        return handleElsevierException(ex, $"query={query}");
    }
})
.WithName("SearchScienceDirect")
.WithDescription("Search the ScienceDirect full-text article index using query syntax.");

// Fallback to index.html for client side routing
app.MapFallbackToFile("index.html");

app.Run();
