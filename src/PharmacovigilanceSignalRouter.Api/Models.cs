namespace PharmacovigilanceSignalRouter.Api;

public sealed record SafetySnapshot(
    string Id,
    string Name,
    string SignalLane,
    string SourceSystem,
    string Status,
    string IntakeStatus,
    string Owner,
    int OpenFindings,
    int EscalatedFindings,
    DateTimeOffset CollectedAt
);

public sealed record SafetyGap(
    string Id,
    string SnapshotId,
    string ControlFamily,
    string Severity,
    string Subject,
    string ExpectedState,
    string ObservedState,
    int HoursOpen,
    bool BlocksEscalation
);

public sealed record SignalLanePacket(
    string Id,
    string Lane,
    string Owner,
    string Status,
    string Focus,
    string NextAction,
    string Note
);

public sealed record SafetyCase(
    string CaseId,
    string Lane,
    string Owner,
    string Status,
    int ReadinessScore,
    string Blocker,
    string DecisionNote,
    int ReviewWindowHours
);

public sealed record PharmacovigilanceExport(
    IReadOnlyList<SafetySnapshot> Snapshots,
    IReadOnlyList<SafetyGap> Gaps
);

public sealed record PharmacovigilanceFinding(
    string Code,
    string Severity,
    string Subject,
    string Message,
    string Owner
);

public sealed record PharmacovigilanceReport(
    int Snapshots,
    int CurrentIntakeLanes,
    int Gaps,
    int EscalationBlocks,
    int EvidenceRisks,
    int ReportingRisks,
    IReadOnlyList<PharmacovigilanceFinding> Findings,
    bool Ok
);
