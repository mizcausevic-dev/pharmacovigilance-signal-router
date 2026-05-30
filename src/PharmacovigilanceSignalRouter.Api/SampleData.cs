namespace PharmacovigilanceSignalRouter.Api;

public static class SampleData
{
    public static readonly PharmacovigilanceExport Payload = new(
        Snapshots:
        [
            new(
                "sig-cardiac",
                "Cardiac rhythm adverse-event cluster",
                "Case intake and medical review lane",
                "Global Safety DB",
                "WATCH",
                "CURRENT",
                "Safety Operations",
                9,
                4,
                DateTimeOffset.Parse("2026-05-30T14:05:00Z")
            ),
            new(
                "sig-hepatic",
                "Hepatic lab-elevation signal lane",
                "Aggregate reporting and signal review lane",
                "Literature + E2B feed",
                "CRITICAL",
                "STALE",
                "Regulatory Affairs",
                6,
                3,
                DateTimeOffset.Parse("2026-05-29T11:40:00Z")
            ),
            new(
                "sig-peds",
                "Pediatric rash follow-up cluster",
                "Follow-up and coding lane",
                "Affiliate inbox",
                "WATCH",
                "CURRENT",
                "Patient Safety",
                4,
                1,
                DateTimeOffset.Parse("2026-05-30T09:25:00Z")
            )
        ],
        Gaps:
        [
            new(
                "gap-intake",
                "sig-cardiac",
                "Case Intake",
                "high",
                "Initial case intake completeness",
                "Every serious adverse-event case should keep reporter, suspect product, event date, and seriousness fields complete before escalation.",
                "Three incoming cases still lack a confirmed event onset date and one seriousness rationale is missing from the intake packet.",
                14,
                true
            ),
            new(
                "gap-seriousness",
                "sig-cardiac",
                "Seriousness Assessment",
                "high",
                "Medical seriousness assessment",
                "Medical review should keep hospitalization criteria and medical-significance rationale explicit before signal escalation.",
                "One case is still marked non-serious even though the narrative describes an overnight hospitalization.",
                19,
                true
            ),
            new(
                "gap-coding",
                "sig-peds",
                "MedDRA Coding",
                "medium",
                "MedDRA coding consistency",
                "Preferred terms and LLTs should stay consistent across affiliate intake and central safety review.",
                "Two pediatric rash cases still carry local free-text terms instead of the approved MedDRA coding set.",
                11,
                false
            ),
            new(
                "gap-reporting",
                "sig-hepatic",
                "Aggregate Reporting",
                "high",
                "Periodic safety report evidence",
                "Aggregate reporting should keep the linked case set, data-cut timestamp, and reviewer approval complete before submission windows close.",
                "The hepatic signal packet still lacks the locked case-set export for the next periodic reporting cycle.",
                27,
                true
            ),
            new(
                "gap-followup",
                "sig-peds",
                "Follow-Up",
                "medium",
                "Reporter follow-up closure",
                "Follow-up lanes should keep outstanding reporter questions and due dates visible before the next review board.",
                "Two pediatric cases still lack the second-round follow-up needed to confirm dechallenge outcome.",
                16,
                false
            ),
            new(
                "gap-signal",
                "sig-hepatic",
                "Signal Review",
                "high",
                "Signal committee packet",
                "Signal review packets should keep literature rationale, cross-functional signoff, and next action explicit before closure.",
                "The current hepatic packet still lacks the literature-review addendum and the named action owner for escalation.",
                22,
                true
            )
        ]
    );

    public static readonly IReadOnlyList<SignalLanePacket> SignalLane =
    [
        new(
            "intake-lane",
            "Case intake and triage lane",
            "Safety Operations",
            "red",
            "Serious case completeness, reporter traceability, and ready-for-review intake packets",
            "Close the missing onset date and seriousness rationale before another intake packet escalates with a blind spot.",
            "The intake queue is currently the biggest safety-handling pressure point."
        ),
        new(
            "medical-review-lane",
            "Medical review and coding lane",
            "Medical Review",
            "red",
            "Seriousness assessment, MedDRA coding, and medically significant narrative review",
            "Correct the hospitalization classification and close the open coding drift before the next committee pass.",
            "Medical review posture is not committee-safe yet."
        ),
        new(
            "follow-up-lane",
            "Follow-up and reporter closure lane",
            "Patient Safety",
            "yellow",
            "Reporter questions, affiliate handoffs, and evidence completeness for open pediatric cases",
            "Close the overdue follow-up questions before the next review cycle hardens the open unknowns.",
            "Follow-up posture is recoverable if the next outreach closes on time."
        ),
        new(
            "reporting-lane",
            "Aggregate reporting and signal committee lane",
            "Regulatory Affairs",
            "red",
            "Periodic reporting evidence, literature review continuity, and named action ownership",
            "Close the missing case-set export and literature addendum before another hepatic signal review cycle opens.",
            "The signal packet is not yet escalation-safe."
        )
    ];

    public static readonly IReadOnlyList<SafetyCase> SafetyPosture =
    [
        new(
            "PVSR-14",
            "Cardiac intake packet",
            "Safety Operations",
            "red",
            54,
            "Case completeness is still missing a confirmed onset date and seriousness rationale.",
            "Do not let a partially described serious case move downstream without a defensible intake packet.",
            6
        ),
        new(
            "PVSR-21",
            "Hepatic signal packet",
            "Regulatory Affairs",
            "red",
            59,
            "The aggregate reporting packet still lacks the locked case-set export and literature addendum.",
            "Do not let the signal committee close review while the reporting evidence is still incomplete.",
            12
        ),
        new(
            "PVSR-27",
            "Pediatric follow-up packet",
            "Patient Safety",
            "yellow",
            76,
            "Two cases still lack second-round follow-up needed to confirm dechallenge outcome.",
            "The lane can recover if reporter follow-up lands in the next review window.",
            18
        ),
        new(
            "PVSR-30",
            "Medical review packet",
            "Medical Review",
            "yellow",
            71,
            "Hospitalization criteria and one MedDRA coding set still need correction before signoff.",
            "Committee posture can recover if medical review closes before the next signal meeting.",
            9
        )
    ];
}
