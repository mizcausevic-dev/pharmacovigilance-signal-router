from pathlib import Path
import textwrap


ROOT = Path(__file__).resolve().parents[1]
SHOT_DIR = ROOT / "screenshots"
SHOT_DIR.mkdir(exist_ok=True)


def wrap(text: str, width: int):
    return textwrap.wrap(text, width=width) or [text]


def draw_lines(lines, x, y, font_size=22, color="#e9f3ff", weight="400", family="Inter,Segoe UI,Arial"):
    parts = [f'<text x="{x}" y="{y}" fill="{color}" font-size="{font_size}" font-weight="{weight}" font-family="{family}">']
    dy = 0
    for line in lines:
        parts.append(f'<tspan x="{x}" dy="{dy}">{line}</tspan>')
        dy = int(font_size * 1.25)
    parts.append("</text>")
    return "\n".join(parts)


def stat_card(x, y, w, h, label, value, detail, accent):
    return f"""
    <rect x="{x}" y="{y}" width="{w}" height="{h}" rx="20" fill="#111827" stroke="rgba(120,255,170,.14)" />
    {draw_lines([label.upper()], x + 24, y + 34, 11, "#8fdcff", "700", "Consolas, monospace")}
    {draw_lines([value], x + 24, y + 86, 34, accent, "700", "Segoe UI, Arial")}
    {draw_lines(wrap(detail, 28), x + 24, y + 124, 15, "#b8c6db", "400")}
    """


def panel(x, y, w, h, kicker, title, body, accent="#19c7ff"):
    return f"""
    <rect x="{x}" y="{y}" width="{w}" height="{h}" rx="22" fill="#0b1220" stroke="rgba(120,255,170,.18)" />
    {draw_lines([kicker.upper()], x + 26, y + 34, 11, accent, "700", "Consolas, monospace")}
    {draw_lines(wrap(title, 30), x + 26, y + 84, 28, "#f5f7ff", "700", "Georgia, serif")}
    {draw_lines(wrap(body, 50), x + 26, y + 136, 16, "#b8c6db", "400")}
    """


def bullet_list(items, x, y, accent="#37ff8b"):
    rows = []
    offset = 0
    for item in items:
        rows.append(f'<circle cx="{x}" cy="{y + offset - 6}" r="5" fill="{accent}" />')
        rows.append(draw_lines(wrap(item, 70), x + 16, y + offset, 16, "#e9f3ff", "400"))
        offset += 54
    return "\n".join(rows)


def shell(eyebrow, title, subtitle, inner):
    return f"""<svg xmlns="http://www.w3.org/2000/svg" width="1400" height="860" viewBox="0 0 1400 860">
    <rect width="1400" height="860" fill="#070a0f"/>
    <rect x="24" y="24" width="1352" height="812" rx="30" fill="#0a1426" stroke="rgba(120,255,170,.18)"/>
    <rect x="58" y="58" width="1284" height="152" rx="26" fill="#0b1220" stroke="rgba(120,255,170,.12)"/>
    {draw_lines([eyebrow.upper()], 94, 96, 13, "#37ff8b", "700", "Consolas, monospace")}
    {draw_lines(wrap(title, 36), 94, 146, 34, "#f5f7ff", "700", "Georgia, serif")}
    {draw_lines(wrap(subtitle, 100), 94, 194, 18, "#b8c6db", "400")}
    {inner}
    </svg>"""


overview = shell(
    "Pharmacovigilance Signal Router",
    "Control-plane summary for safety signals, case-routing gaps, and committee-safe review posture.",
    "Case intake completeness, seriousness assessment, MedDRA coding, follow-up pressure, aggregate reporting evidence, and named signal actions stay visible together before committee review hardens around weak packets.",
    f"""
    {stat_card(58, 238, 288, 150, "signal clusters", "3", "Synthetic safety-signal clusters across active intake, follow-up, and reporting lanes.", "#19c7ff")}
    {stat_card(364, 238, 288, 150, "open gaps", "6", "Case intake, seriousness, coding, reporting, follow-up, and signal-review gaps still open.", "#ffcc66")}
    {stat_card(670, 238, 288, 150, "escalation blocks", "4", "Four issues still block clean escalation or committee closure.", "#ff5c7a")}
    {stat_card(976, 238, 366, 150, "lead recommendation", "Repair intake and reporting evidence", "Do not clear the hepatic signal packet until aggregate reporting evidence and named signal actions recover.", "#37ff8b")}

    {panel(58, 420, 614, 356, "Case routing", "The riskiest safety gaps stay visible first.", "Case-intake holes, seriousness drift, coding inconsistencies, and missing literature evidence stay tied to their owners so the queue is readable before committee closure.", "#19c7ff")}
    <rect x="708" y="420" width="634" height="356" rx="22" fill="#0b1220" stroke="rgba(120,255,170,.18)" />
    {draw_lines(["SAFETY POSTURE"], 734, 454, 11, "#37ff8b", "700", "Consolas, monospace")}
    {draw_lines(["What must close before", "the next signal review"], 734, 506, 28, "#f5f7ff", "700", "Georgia, serif")}
    {bullet_list([
      "The intake packet still lacks a confirmed onset date and one seriousness rationale for a hospitalized case.",
      "The hepatic signal packet still depends on a missing locked case-set export and a literature-review addendum.",
      "Two pediatric follow-up packets still need dechallenge outcome confirmation before the next committee cycle."
    ], 744, 580)}
    """,
)

lane = shell(
    "Signal Lane",
    "Each lane keeps owner, safety focus, and next action visible.",
    "Case intake, medical review, follow-up, and aggregate reporting lanes stay separated cleanly so safety routing does not collapse into one noisy queue.",
    f"""
    {panel(58, 238, 620, 250, "Case intake lane", "Serious-case completeness and intake discipline", "Reporter traceability, suspect product attribution, seriousness signals, and ready-for-review intake packets stay grouped under Safety Operations ownership.", "#19c7ff")}
    {panel(708, 238, 634, 250, "Medical review lane", "Seriousness review and MedDRA coding posture", "Medical review evidence, hospitalization criteria, coding consistency, and downstream signal dependencies stay visible before another committee cycle.", "#ffcc66")}
    {panel(58, 520, 620, 256, "Follow-up lane", "Reporter closure and affiliate handoff timing", "Open follow-up questions stay tied to Patient Safety review pressure before the next escalation window.", "#b88cff")}
    {panel(708, 520, 634, 256, "Reporting lane", "Aggregate reporting and signal action continuity", "Periodic reporting evidence, literature rationale, and named action ownership provide the final committee-safe gate.", "#37ff8b")}
    """,
)

posture = shell(
    "Safety Posture",
    "Packet readiness, missing evidence, and signal-review timing stay readable for biotech operators.",
    "The board keeps final pharmacovigilance posture explicit instead of hiding it behind generic safety status metrics.",
    f"""
    {panel(58, 238, 402, 244, "Packet PVSR-14", "Cardiac intake packet", "54 percent ready. Case completeness is still missing a confirmed onset date and seriousness rationale.", "#ff5c7a")}
    {panel(498, 238, 402, 244, "Packet PVSR-21", "Hepatic signal packet", "59 percent ready. The aggregate reporting packet still lacks the locked case-set export and literature addendum.", "#ffcc66")}
    {panel(938, 238, 404, 244, "Packet PVSR-30", "Medical review packet", "71 percent ready. Hospitalization criteria and one MedDRA coding set still need correction before signoff.", "#37ff8b")}
    {panel(58, 514, 1284, 262, "Why this monetizes cleanly", "Hosted preview planned, paid template pack later, embedded by engagement.", "This is a strong biotech safety wedge because it lives where teams actually feel pressure: case intake quality, seriousness review, follow-up closure, periodic reporting evidence, and the named signal action needed before committee closure.", "#19c7ff")}
    """,
)

(SHOT_DIR / "01-overview.svg").write_text(overview, encoding="utf-8")
(SHOT_DIR / "02-signal-lane.svg").write_text(lane, encoding="utf-8")
(SHOT_DIR / "03-safety-posture.svg").write_text(posture, encoding="utf-8")
