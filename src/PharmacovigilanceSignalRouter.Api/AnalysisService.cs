namespace PharmacovigilanceSignalRouter.Api;

public static class AnalysisService
{
    public static PharmacovigilanceReport Analyze(PharmacovigilanceExport payload)
    {
        var findings = payload.Gaps
            .Select(gap => new PharmacovigilanceFinding(
                GapCode(gap.ControlFamily),
                gap.Severity,
                gap.Subject,
                gap.ObservedState,
                OwnerForGap(gap.ControlFamily)))
            .ToList();

        var snapshots = payload.Snapshots.Count;
        var currentIntakeLanes = payload.Snapshots.Count(snapshot => snapshot.IntakeStatus == "CURRENT");
        var gaps = payload.Gaps.Count;
        var escalationBlocks = payload.Gaps.Count(gap => gap.BlocksEscalation);
        var evidenceRisks = payload.Gaps.Count(gap => gap.ControlFamily is "Case Intake" or "Seriousness Assessment" or "MedDRA Coding");
        var reportingRisks = payload.Gaps.Count(gap => gap.ControlFamily is "Aggregate Reporting" or "Follow-Up" or "Signal Review");
        var ok = escalationBlocks == 0;

        return new PharmacovigilanceReport(
            snapshots,
            currentIntakeLanes,
            gaps,
            escalationBlocks,
            evidenceRisks,
            reportingRisks,
            findings,
            ok
        );
    }

    public static object Summary()
    {
        var report = Analyze(SampleData.Payload);
        return new
        {
            snapshots = report.Snapshots,
            currentIntakeLanes = report.CurrentIntakeLanes,
            gaps = report.Gaps,
            escalationBlocks = report.EscalationBlocks,
            evidenceRisks = report.EvidenceRisks,
            reportingRisks = report.ReportingRisks,
            ok = report.Ok
        };
    }

    private static string GapCode(string family) => family switch
    {
        "Case Intake" => "case-intake-gap",
        "Seriousness Assessment" => "seriousness-assessment-gap",
        "MedDRA Coding" => "meddra-coding-gap",
        "Aggregate Reporting" => "aggregate-reporting-gap",
        "Follow-Up" => "follow-up-gap",
        "Signal Review" => "signal-review-gap",
        _ => "pharmacovigilance-gap"
    };

    private static string OwnerForGap(string family) => family switch
    {
        "Case Intake" => "Safety Operations",
        "Seriousness Assessment" => "Medical Review",
        "MedDRA Coding" => "Case Processing",
        "Aggregate Reporting" => "Regulatory Affairs",
        "Follow-Up" => "Patient Safety",
        "Signal Review" => "Signal Committee",
        _ => "Safety Operations"
    };
}
