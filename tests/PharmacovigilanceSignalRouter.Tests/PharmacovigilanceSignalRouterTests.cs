using Microsoft.AspNetCore.Mvc.Testing;
using PharmacovigilanceSignalRouter.Api;

namespace PharmacovigilanceSignalRouter.Tests;

public sealed class PharmacovigilanceSignalRouterTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PharmacovigilanceSignalRouterTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Overview_route_renders_safety_signal_shell()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains("Pharmacovigilance Signal Router", html);
        Assert.Contains("safety-signal", html);
    }

    [Fact]
    public async Task Api_summary_returns_expected_counts()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/dashboard/summary");
        var json = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains("\"snapshots\":3", json);
        Assert.Contains("\"currentIntakeLanes\":2", json);
        Assert.Contains("\"escalationBlocks\":4", json);
    }

    [Fact]
    public void Analysis_flags_high_risk_safety_gaps()
    {
        var report = AnalysisService.Analyze(SampleData.Payload);

        Assert.Equal(3, report.Snapshots);
        Assert.Equal(6, report.Gaps);
        Assert.Contains(report.Findings, finding => finding.Code == "seriousness-assessment-gap");
        Assert.Contains(report.Findings, finding => finding.Code == "signal-review-gap");
    }
}
