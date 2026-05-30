using System.Text.Json;
using PharmacovigilanceSignalRouter.Api;

var app = PharmacovigilanceSignalRouterApplication.BuildApp(args);

if (args.Contains("--prerender"))
{
    await SiteBuilder.WriteAsync();
    return;
}

if (args.Contains("--demo"))
{
    Console.WriteLine(JsonSerializer.Serialize(AnalysisService.Summary(), new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine(JsonSerializer.Serialize(SampleData.SignalLane, new JsonSerializerOptions { WriteIndented = true }));
    return;
}

app.Run();

public partial class Program;

public static class PharmacovigilanceSignalRouterApplication
{
    public static WebApplication BuildApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        app.MapGet("/", () => Results.Content(RenderService.Overview(), "text/html"));
        app.MapGet("/signal-lane", () => Results.Content(RenderService.SignalLane(), "text/html"));
        app.MapGet("/case-routing", () => Results.Content(RenderService.CaseRouting(), "text/html"));
        app.MapGet("/safety-posture", () => Results.Content(RenderService.SafetyPosture(), "text/html"));
        app.MapGet("/verification", () => Results.Content(RenderService.Verification(), "text/html"));
        app.MapGet("/docs", () => Results.Content(RenderService.Docs(), "text/html"));

        app.MapGet("/api/dashboard/summary", () => Results.Json(AnalysisService.Summary()));
        app.MapGet("/api/signal-lane", () => Results.Json(SampleData.SignalLane));
        app.MapGet("/api/case-routing", () => Results.Json(SampleData.Payload.Gaps));
        app.MapGet("/api/safety-posture", () => Results.Json(SampleData.SafetyPosture));
        app.MapGet("/api/verification", () => Results.Json(new[]
        {
            "Synthetic pharmacovigilance evidence only; no patient-identifiable, tenant, or sponsor-confidential safety data is published.",
            "Case intake, seriousness assessment, MedDRA coding, follow-up, aggregate reporting, and signal review are modeled as operator surfaces.",
            "This repo demonstrates biotech safety workflow depth without claiming GVP, FDA, EMA, or pharmacovigilance compliance."
        }));
        app.MapGet("/api/sample", () => Results.Text(RenderService.Sample(), "application/json"));

        return app;
    }
}

public static class SiteBuilder
{
    public static async Task WriteAsync()
    {
        var root = FindRepoRoot();
        var siteDir = Path.Combine(root, "site");
        Directory.CreateDirectory(siteDir);

        var pages = new Dictionary<string, string>
        {
            ["index.html"] = RenderService.Overview(),
            [Path.Combine("signal-lane", "index.html")] = RenderService.SignalLane(),
            [Path.Combine("case-routing", "index.html")] = RenderService.CaseRouting(),
            [Path.Combine("safety-posture", "index.html")] = RenderService.SafetyPosture(),
            [Path.Combine("verification", "index.html")] = RenderService.Verification(),
            [Path.Combine("docs", "index.html")] = RenderService.Docs()
        };

        foreach (var (relative, html) in pages)
        {
            var target = Path.Combine(siteDir, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            await File.WriteAllTextAsync(target, html);
        }

        var apiDir = Path.Combine(siteDir, "api");
        Directory.CreateDirectory(Path.Combine(apiDir, "dashboard"));
        await File.WriteAllTextAsync(Path.Combine(apiDir, "dashboard", "summary.json"), JsonSerializer.Serialize(AnalysisService.Summary(), new JsonSerializerOptions { WriteIndented = true }));
        await File.WriteAllTextAsync(Path.Combine(apiDir, "signal-lane.json"), JsonSerializer.Serialize(SampleData.SignalLane, new JsonSerializerOptions { WriteIndented = true }));
        await File.WriteAllTextAsync(Path.Combine(apiDir, "case-routing.json"), JsonSerializer.Serialize(SampleData.Payload.Gaps, new JsonSerializerOptions { WriteIndented = true }));
        await File.WriteAllTextAsync(Path.Combine(apiDir, "safety-posture.json"), JsonSerializer.Serialize(SampleData.SafetyPosture, new JsonSerializerOptions { WriteIndented = true }));
        await File.WriteAllTextAsync(Path.Combine(apiDir, "verification.json"), JsonSerializer.Serialize(new[]
        {
            "Synthetic pharmacovigilance evidence only; no patient-identifiable, tenant, or sponsor-confidential safety data is published.",
            "Case intake, seriousness assessment, MedDRA coding, follow-up, aggregate reporting, and signal review are modeled as operator surfaces.",
            "This repo demonstrates biotech safety workflow depth without claiming GVP, FDA, EMA, or pharmacovigilance compliance."
        }, new JsonSerializerOptions { WriteIndented = true }));
        await File.WriteAllTextAsync(Path.Combine(apiDir, "sample.json"), RenderService.Sample());

        const string domain = "safety.kineticgain.com";
        await File.WriteAllTextAsync(
            Path.Combine(siteDir, "robots.txt"),
            $"User-agent: *{Environment.NewLine}Allow: /{Environment.NewLine}Sitemap: https://{domain}/sitemap.xml{Environment.NewLine}");
        await File.WriteAllTextAsync(Path.Combine(siteDir, "sitemap.xml"), """
<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
  <url><loc>https://safety.kineticgain.com/</loc></url>
  <url><loc>https://safety.kineticgain.com/signal-lane/</loc></url>
  <url><loc>https://safety.kineticgain.com/case-routing/</loc></url>
  <url><loc>https://safety.kineticgain.com/safety-posture/</loc></url>
  <url><loc>https://safety.kineticgain.com/verification/</loc></url>
  <url><loc>https://safety.kineticgain.com/docs/</loc></url>
</urlset>
""");
        await File.WriteAllTextAsync(Path.Combine(siteDir, "CNAME"), domain + Environment.NewLine);
    }

    private static string FindRepoRoot()
    {
        var current = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(current, "pharmacovigilance-signal-router.sln")))
            {
                return current;
            }

            current = Directory.GetParent(current)?.FullName
                ?? throw new DirectoryNotFoundException("Unable to resolve repo root.");
        }

        throw new DirectoryNotFoundException("Unable to resolve repo root.");
    }
}
