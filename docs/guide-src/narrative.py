import html
import json
from pathlib import Path

# Narrative sections, diagrams and styling for the code guide.

TEMPLATE = """<!doctype html>
<html lang="en"><head><meta charset="utf-8"><title>Yahtzee Code Guide</title>
<style>{css}</style></head>
<body>
{cover}
{toc}
{narrative}
{walkthrough}
{closing}
</body></html>"""

CSS = r"""
@page { size: A4; margin: 16mm 15mm 18mm 15mm;
  @bottom-left { content: "Yahtzee (F20SC CW1) - Code Guide and Development Log"; font: 8pt 'Segoe UI', sans-serif; color: #888; }
  @bottom-right { content: "Page " counter(page) " of " counter(pages); font: 8pt 'Segoe UI', sans-serif; color: #888; } }
@page :first { @bottom-left { content: none; } @bottom-right { content: none; } }
* { box-sizing: border-box; }
body { font: 10pt/1.45 'Segoe UI', Arial, sans-serif; color: #1d1d1f; margin: 0; }
h1 { font: 600 26pt 'Segoe UI Semibold', 'Segoe UI', sans-serif; color: #165e3c; margin: 0 0 6pt; }
h2 { font: 600 16pt 'Segoe UI Semibold', 'Segoe UI', sans-serif; color: #165e3c; border-bottom: 2px solid #165e3c; padding-bottom: 3pt; margin: 18pt 0 8pt; }
h3 { font: 600 12.5pt 'Segoe UI Semibold', 'Segoe UI', sans-serif; color: #0b3d6b; margin: 14pt 0 3pt; break-after: avoid; }
h4 { font: 600 10.5pt 'Segoe UI', sans-serif; margin: 10pt 0 3pt; break-after: avoid; }
p { margin: 4pt 0 6pt; }
.newpage { break-before: page; }
a { color: #0b5cad; text-decoration: none; }
code { font: 8.6pt Consolas, 'Cascadia Mono', monospace; background: #f1f3f5; padding: 0 2px; border-radius: 2px; }
.muted { color: #777; font-size: 8.5pt; }
table.t { border-collapse: collapse; width: 100%; margin: 6pt 0 10pt; font-size: 9pt; break-inside: auto; }
table.t th { background: #165e3c; color: #fff; text-align: left; padding: 4pt 6pt; }
table.t td { border-bottom: 1px solid #d8dde2; padding: 3.5pt 6pt; vertical-align: top; }
table.t tr { break-inside: avoid; }
.ok { color: #137333; font-weight: 600; } .no { color: #c5221f; font-weight: 600; } .part { color: #b06000; font-weight: 600; }
.box { border-left: 4px solid #165e3c; background: #f0f7f3; padding: 6pt 10pt; margin: 8pt 0; break-inside: avoid; }
.warn { border-left: 4px solid #c5221f; background: #fdf1f0; padding: 6pt 10pt; margin: 8pt 0; break-inside: avoid; }
.note { border-left: 4px solid #0b5cad; background: #eef4fb; padding: 6pt 10pt; margin: 8pt 0; break-inside: avoid; }
figure { margin: 8pt 0 12pt; break-inside: avoid; text-align: center; }
figure img, figure svg { max-width: 100%; border: 1px solid #ccd; }
figure svg { border: none; }
figcaption { font-size: 8.5pt; color: #555; margin-top: 3pt; font-style: italic; }
pre.cmd { font: 8.5pt Consolas, monospace; background: #1e1e1e; color: #e6e6e6; padding: 6pt 8pt; border-radius: 4px; white-space: pre-wrap; break-inside: avoid; }
pre.tree { font: 8.5pt Consolas, monospace; background: #f6f8fa; padding: 6pt 8pt; border: 1px solid #e1e4e8; border-radius: 4px; break-inside: avoid; }
/* cover */
.cover { height: 255mm; display: flex; flex-direction: column; justify-content: center; }
.cover .sub { font-size: 14pt; color: #444; margin-bottom: 18pt; }
.cover .meta { font-size: 10pt; color: #555; margin-top: 24pt; }
.status { border: 2px solid #c5221f; border-radius: 6px; padding: 10pt 12pt; background: #fff7f6; margin-top: 10pt; }
/* toc */
.toc ul { list-style: none; padding-left: 0; } .toc ul ul { padding-left: 16pt; font-size: 9pt; }
.toc li { margin: 2pt 0; }
/* walkthrough */
section.file { break-before: page; }
section.file:first-of-type { break-before: auto; }
.path { font: 8.5pt Consolas, monospace; color: #555; margin-bottom: 4pt; }
.seg { margin: 0 0 6pt; break-inside: avoid; border: 1px solid #e3e6ea; border-radius: 4px; overflow: hidden; }
pre.code { margin: 0; padding: 4pt 6pt; background: #fbfbfc; font: 7.9pt/1.35 Consolas, 'Cascadia Mono', monospace; white-space: pre-wrap;
  word-break: break-word; border-bottom: 1px solid #e3e6ea; }
.ln { display: inline-block; width: 2.6em; color: #9aa0a6; user-select: none; }
.exp { padding: 4pt 8pt 5pt; font-size: 9pt; background: #fff; }
.lr { display: inline-block; font-size: 7.5pt; font-weight: 700; color: #fff; background: #0b3d6b; border-radius: 3px; padding: 0 5px; margin-right: 6px; }
.kw { color: #0000ff; } .ty { color: #2b91af; } .str { color: #a31515; } .com { color: #008000; } .doc { color: #6a8a6a; } .num { color: #098658; }
/* annotated screenshot */
.annot { position: relative; display: inline-block; width: 100%; }
.annot img { width: 100%; display: block; }
.callout { position: absolute; width: 20px; height: 20px; margin: -10px 0 0 -10px; border-radius: 50%; background: #d93025; color: #fff;
  font: 700 10pt/20px 'Segoe UI', sans-serif; text-align: center; box-shadow: 0 0 0 2px #fff; }
ol.legend { columns: 2; font-size: 9pt; padding-left: 0; list-style: none; break-inside: avoid; }
ol.legend li { margin: 0 0 3pt; break-inside: avoid; }
ol.legend .num { display: inline-block; width: 16px; height: 16px; border-radius: 50%; background: #d93025; color: #fff; text-align: center;
  font-size: 8pt; line-height: 16px; margin-right: 4px; }
.two { display: flex; gap: 10pt; } .two > * { flex: 1; }
.dice { display: inline-flex; gap: 4px; } .dice span { width: 22px; height: 22px; border: 1.5px solid #333; border-radius: 4px; display: inline-flex;
  align-items: center; justify-content: center; font-weight: 700; background: #fff; }
"""

# Callouts on the annotated screenshot: (x, y) in screenshot pixels, legend text.
UI_CALLOUTS = [
    (195, 64, "<b>Menu bar</b>: Game (New Game Ctrl+N, Exit) and Help (How to Play F1)."),
    (322, 143, "<b>Whose turn it is</b> (requirement). AI players are marked \"(computer)\"."),
    (242, 180, "<b>Round</b> n of 13, derived from the scorecard."),
    (266, 213, "<b>Rolls left</b> (requirement), in bold gold."),
    (60, 268, "<b>Held die</b>: gold face and border."),
    (168, 394, "<b>HELD</b> caption: this die is set aside (requirement)."),
    (718, 394, "<b>WILL ROLL</b> caption: this die will be re-rolled (requirement)."),
    (408, 482, "<b>Roll button</b> states exactly how many dice it will roll; R is the shortcut."),
    (700, 575, "<b>Hint</b> text explaining what the player can do next."),
    (1262, 594, "<b>\"This roll\" preview column</b> (requirement): blue = points available, grey = 0. Click a value to score it."),
    (1500, 132, "<b>Current player's column</b>, highlighted gold/cream."),
    (1000, 810, "<b>TOTAL row</b>: running total for every player."),
    (300, 1086, "<b>Status bar</b>: the last roll or score, e.g. \"Player 1 rolled 3 5 3 1 6\"."),
]


def cover():
    return """
<div class="cover">
  <h1>Yahtzee in C#</h1>
  <div class="sub">F20SC Industrial Programming &middot; Coursework 1<br>Code Guide, Architecture and Development Log</div>
  <p>This guide explains <b>every file and every line</b> of the code written so far, the design behind it, and every step taken to build and
  verify it. It is meant to help you understand, explain and continue the project (including in a possible in-person demonstration).</p>
  <div class="status">
    <b style="color:#c5221f">Status on 8 October 2026: all 7 implementation steps are complete, but the submission is not finished yet.</b><br>
    Done: every feature in the specification's checklist, 130 passing unit tests, an end-to-end UI test, and the standalone
    <code>publish\\Yahtzee.exe</code>.<br>
    Still to do (your work): the report (PDF, max 10 pages), the narrated video, testing the exe on a lab machine, and your
    evaluation notes in the GenAI log. See section 1 and section 13.
  </div>
  <div class="warn" style="margin-top:14pt"><b>This is not your coursework report.</b> The specification requires a report (max 10 pages) written and evaluated by you,
  with a GenAI usage statement and your prompts and the model's responses in an appendix. You can use this guide as source material while writing it,
  but submitting it as the report would not meet the specification.</div>
  <div class="meta">Generated with Claude Code (Claude Opus 5.5) by <code>docs/guide-src/build_guide.ps1</code>.<br>
  Source listings are read directly from the repository's files each time the PDF is built, so they always match the code.</div>
</div>"""


def toc(files_html):
    return f"""
<div class="toc newpage"><h2>Contents</h2><ul>
<li><a href="#s1">1. Is the project complete? Status against the specification</a></li>
<li><a href="#s2">2. The assignment in brief</a></li>
<li><a href="#s3">3. Development log: every step performed</a></li>
<li><a href="#s4">4. Building, running and testing</a></li>
<li><a href="#s5">5. Architecture and design</a> (including <a href="#s57">save files</a>, <a href="#s58">database</a>, <a href="#s59">AI</a>)</li>
<li><a href="#s6">6. The scoring rules, illustrated</a></li>
<li><a href="#s7">7. The user interface</a></li>
<li><a href="#s8">8. C# language features used</a></li>
<li><a href="#s8b">How to read the line-by-line walkthrough</a></li>
{files_html}
<li><a href="#s13">13. What remains: steps 4&ndash;7, report, video and submission</a></li>
<li><a href="#s14">14. Glossary</a></li>
</ul></div>"""


def svg_architecture():
    pill = lambda x, y, w, t: (f'<rect x="{x}" y="{y}" width="{w}" height="24" rx="12" fill="#fff" stroke="#1b6b45"/>'
                               f'<text x="{x + w / 2}" y="{y + 16}" text-anchor="middle" font-size="11">{t}</text>')
    sub = lambda x, y, w, h, title, items, dashed=False: (
        f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="6" fill="#fff" stroke="#2b5d8a" {"stroke-dasharray=\"5 4\"" if dashed else ""}/>'
        f'<text x="{x + 8}" y="{y + 16}" font-size="11.5" font-weight="700" fill="#0b3d6b">{title}</text>'
        + "".join(f'<text x="{x + 10}" y="{y + 34 + i * 15}" font-size="10.5">{it}</text>' for i, it in enumerate(items)))
    return f"""<svg viewBox="0 0 760 470" width="100%" font-family="Segoe UI, Arial">
<defs><marker id="ar" markerWidth="10" markerHeight="8" refX="9" refY="4" orient="auto"><path d="M0,0 L10,4 L0,8 z" fill="#444"/></marker></defs>
<rect x="20" y="10" width="720" height="96" rx="8" fill="#e8f5ec" stroke="#1b6b45" stroke-width="1.5"/>
<text x="34" y="32" font-size="13" font-weight="700" fill="#165e3c">Yahtzee.WinForms  (net10.0-windows, Yahtzee.exe): presentation only</text>
{pill(34, 46, 90, "MainForm")}{pill(132, 46, 120, "NewGameDialog")}{pill(260, 46, 100, "ResultsForm")}{pill(368, 46, 96, "DieControl")}
{pill(472, 46, 116, "HighScoresForm")}{pill(596, 46, 130, "PlayerTypeOption")}
<text x="34" y="96" font-size="10" fill="#555">Reads engine state, calls engine actions, redraws on events. Contains no game rules.</text>
<line x1="380" y1="108" x2="380" y2="146" stroke="#444" stroke-width="1.5" marker-end="url(#ar)"/>
<text x="390" y="132" font-size="10.5" fill="#333">project reference: calls Roll / SetHeld / Score / PlayAiStep, subscribes to events</text>
<rect x="20" y="150" width="720" height="236" rx="8" fill="#eef4fb" stroke="#2b5d8a" stroke-width="1.5"/>
<text x="34" y="172" font-size="13" font-weight="700" fill="#0b3d6b">Yahtzee.Core  (net10.0 class library): all game logic, no UI references</text>
{sub(34, 184, 220, 92, "Game", ["GameEngine (state machine)", "GameEvents (event records,", "  PlayerStanding, AiStepResult)"])}
{sub(266, 184, 220, 112, "Model", ["Category (+ extensions)", "DiceCup, IDiceRoller,", "  RandomDiceRoller", "ScoreCard, Player"])}
{sub(498, 184, 228, 60, "Rules", ["Scorer (pure scoring functions)"])}
{sub(498, 252, 228, 122, "AI", ["IPlayerStrategy (contract)", "RandomStrategy", "GreedyStrategy", "ExpectedValueStrategy", "StrategyCatalog, ScoringHeuristics"])}
{sub(34, 290, 220, 84, "Persistence", ["GameSnapshot / PlayerSnapshot", "SaveGameSerializer (JSON)", "InvalidSaveFileException"])}
{sub(266, 306, 220, 68, "Data", ["IGameHistoryRepository", "SqliteGameHistoryRepository"])}
<rect x="20" y="412" width="720" height="46" rx="8" fill="#fdf6e3" stroke="#b58900" stroke-width="1.5"/>
<text x="34" y="432" font-size="13" font-weight="700" fill="#7a5c00">Yahtzee.Core.Tests  (xUnit): 130 tests</text>
<text x="34" y="450" font-size="10.5" fill="#555">Uses test doubles FixedDiceRoller, ManualTimeProvider, ScriptedStrategy for deterministic results</text>
<line x1="600" y1="410" x2="600" y2="390" stroke="#444" stroke-width="1.5" marker-end="url(#ar)"/>
<text x="610" y="404" font-size="10.5" fill="#333">tests</text>
</svg>"""


def cls(x, y, w, title, stereo, members, fill="#fff"):
    h = 26 + (14 if stereo else 0) + 14 * len(members) + 6
    s = f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="4" fill="{fill}" stroke="#2b5d8a" stroke-width="1.3"/>'
    ty = y + 16
    if stereo:
        s += f'<text x="{x + w / 2}" y="{ty}" text-anchor="middle" font-size="9.5" fill="#666">&#171;{stereo}&#187;</text>'
        ty += 14
    s += f'<text x="{x + w / 2}" y="{ty}" text-anchor="middle" font-size="12" font-weight="700" fill="#0b3d6b">{title}</text>'
    s += f'<line x1="{x}" y1="{ty + 6}" x2="{x + w}" y2="{ty + 6}" stroke="#2b5d8a"/>'
    for i, m in enumerate(members):
        s += f'<text x="{x + 6}" y="{ty + 20 + i * 14}" font-size="9.8" font-family="Consolas, monospace">{m}</text>'
    return s


def svg_classes():
    return f"""<svg viewBox="0 0 760 560" width="100%" font-family="Segoe UI, Arial">
<defs><marker id="a2" markerWidth="10" markerHeight="8" refX="9" refY="4" orient="auto"><path d="M0,0 L10,4 L0,8 z" fill="#444"/></marker>
<marker id="tri" markerWidth="12" markerHeight="12" refX="11" refY="6" orient="auto"><path d="M0,0 L11,6 L0,12 z" fill="#fff" stroke="#444"/></marker>
<marker id="dia" markerWidth="14" markerHeight="10" refX="1" refY="5" orient="auto"><path d="M1,5 L7,1 L13,5 L7,9 z" fill="#444"/></marker></defs>
{cls(10, 10, 250, "GameEngine", "", ["Players, CurrentPlayer", "Round (derived), RollsUsed", "RollsRemaining, Dice, Held", "IsGameOver", "CanRoll / CanHold / CanScore", "Roll()", "SetHeld(i, held), ToggleHeld(i)", "Preview()", "Score(category) : int", "PlayAiStep() : AiStepResult", "GetStandings()", "events: StateChanged, TurnStarted,", "  DiceRolled, CategoryScored, GameOver"], "#eef4fb")}
{cls(310, 10, 200, "Player", "", ["Name : string", "Strategy : IPlayerStrategy?", "IsAi : bool", "ScoreCard : ScoreCard", "Total : int"])}
{cls(555, 10, 195, "ScoreCard", "", ["Scores : Category&#8594;int", "AvailableCategories", "IsComplete, Total", "IsFilled(c), GetScore(c)", "Record(c, dice) : int", "Preview(dice)"])}
{cls(310, 200, 200, "DiceCup", "", ["Values : int[5]", "Held : bool[5]", "HasRolled", "Roll(), SetHeld(), Reset()"])}
{cls(310, 340, 200, "IDiceRoller", "interface", ["RollDie() : int"])}
{cls(310, 450, 200, "RandomDiceRoller", "", ["RollDie() via Random"])}
{cls(555, 200, 195, "IPlayerStrategy", "interface", ["Name", "ChooseHolds(...) : bool[]?", "ChooseCategory(...)"])}
{cls(555, 330, 195, "Scorer", "static class", ["Score(category, dice)", "Preview(dice, categories)", "ValidateDice(dice)"])}
{cls(555, 455, 195, "Category", "enum", ["Aces ... Sixes, Chance,", "ThreeOfAKind ... Yahtzee"])}
{cls(10, 290, 250, "Event data", "records", ["TurnStartedEventArgs", "DiceRolledEventArgs", "CategoryScoredEventArgs", "GameOverEventArgs", "PlayerStanding (rank, name, score)", "AiStepResult (enum)"], "#fdf6e3")}
<line x1="262" y1="50" x2="308" y2="50" stroke="#444" stroke-width="1.3" marker-start="url(#dia)" marker-end="url(#a2)"/>
<text x="266" y="44" font-size="9.5">1..6</text>
<line x1="512" y1="50" x2="553" y2="50" stroke="#444" stroke-width="1.3" marker-start="url(#dia)" marker-end="url(#a2)"/>
<text x="518" y="44" font-size="9.5">1</text>
<path d="M 262 230 L 308 230" stroke="#444" stroke-width="1.3" fill="none" marker-start="url(#dia)" marker-end="url(#a2)"/>
<text x="268" y="224" font-size="9.5">owns</text>
<path d="M 500 120 L 600 198" stroke="#444" stroke-width="1.3" fill="none" marker-end="url(#a2)"/>
<text x="520" y="150" font-size="9.5">0..1 (null = human)</text>
<line x1="410" y1="282" x2="410" y2="338" stroke="#444" stroke-width="1.3" marker-end="url(#a2)"/>
<text x="416" y="314" font-size="9.5">uses</text>
<line x1="410" y1="450" x2="410" y2="388" stroke="#444" stroke-width="1.3" stroke-dasharray="5 3" marker-end="url(#tri)"/>
<text x="416" y="428" font-size="9.5">implements</text>
<path d="M 750 118 L 756 118 L 756 380 L 752 380" stroke="#444" stroke-width="1.3" fill="none" marker-end="url(#a2)"/>
<text x="700" y="312" font-size="9.5">calls</text>
<line x1="135" y1="214" x2="135" y2="288" stroke="#444" stroke-width="1.3" stroke-dasharray="5 3" marker-end="url(#a2)"/>
<text x="141" y="270" font-size="9.5">raises</text>
<text x="10" y="545" font-size="9.5" fill="#555">Filled diamond = owns (composition). Hollow triangle = implements. Dashed arrow = creates/raises. Category is used by almost every class (lines omitted).</text>
</svg>"""


def svg_states():
    box = lambda x, y, w, h, lines, fill="#eef4fb": (f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="10" fill="{fill}" stroke="#2b5d8a" stroke-width="1.4"/>'
        + "".join(f'<text x="{x + w / 2}" y="{y + 20 + i * 15}" text-anchor="middle" font-size="{12 if i == 0 else 10.5}" '
                  f'{"font-weight=\"700\"" if i == 0 else ""}>{t}</text>' for i, t in enumerate(lines)))
    return f"""<svg viewBox="0 0 760 330" width="100%" font-family="Segoe UI, Arial">
<defs><marker id="a3" markerWidth="10" markerHeight="8" refX="9" refY="4" orient="auto"><path d="M0,0 L10,4 L0,8 z" fill="#333"/></marker></defs>
{box(10, 40, 180, 74, ["Turn starts", "RollsUsed = 0", "dice show ?  (CanRoll only)"])}
{box(285, 40, 200, 74, ["Rolled once or twice", "RollsUsed = 1 or 2", "may hold, re-roll or score"])}
{box(580, 40, 170, 74, ["Rolled three times", "RollsUsed = 3", "must score"])}
<line x1="192" y1="77" x2="282" y2="77" stroke="#333" stroke-width="1.4" marker-end="url(#a3)"/><text x="210" y="70" font-size="10.5">Roll()</text>
<path d="M 355 40 C 355 0, 415 0, 415 38" stroke="#333" stroke-width="1.4" fill="none" marker-end="url(#a3)"/>
<text x="425" y="16" font-size="10.5">Roll() (2nd)  |  SetHeld / ToggleHeld</text>
<line x1="487" y1="77" x2="577" y2="77" stroke="#333" stroke-width="1.4" marker-end="url(#a3)"/><text x="500" y="70" font-size="10.5">Roll() (3rd)</text>
<polygon points="385,180 500,225 385,270 270,225" fill="#fff7e0" stroke="#b58900" stroke-width="1.4"/>
<text x="385" y="220" text-anchor="middle" font-size="10.5">every scorecard</text><text x="385" y="235" text-anchor="middle" font-size="10.5">complete?</text>
<line x1="385" y1="116" x2="385" y2="178" stroke="#333" stroke-width="1.4" marker-end="url(#a3)"/><text x="392" y="150" font-size="10.5">Score(category)</text>
<path d="M 665 116 L 665 150 L 450 196" stroke="#333" stroke-width="1.4" fill="none" marker-end="url(#a3)"/><text x="560" y="162" font-size="10.5">Score(category)</text>
{box(10, 196, 200, 58, ["Next player", "index = (index + 1) mod n"], "#e8f5ec")}
<line x1="268" y1="225" x2="212" y2="225" stroke="#333" stroke-width="1.4" marker-end="url(#a3)"/><text x="228" y="218" font-size="10.5">no</text>
<line x1="100" y1="194" x2="100" y2="116" stroke="#333" stroke-width="1.4" marker-end="url(#a3)"/><text x="106" y="160" font-size="10.5">TurnStarted</text>
{box(560, 200, 190, 50, ["Game over", "GameOver event, results"], "#fdecea")}
<line x1="502" y1="225" x2="558" y2="225" stroke="#333" stroke-width="1.4" marker-end="url(#a3)"/><text x="515" y="218" font-size="10.5">yes</text>
<text x="10" y="300" font-size="10" fill="#555">Every transition also raises StateChanged. Illegal actions (e.g. Roll() in the right-hand state, Score() in the left-hand state)</text>
<text x="10" y="315" font-size="10" fill="#555">throw InvalidOperationException; the UI never offers them because it enables buttons from CanRoll / CanHold / CanScore.</text>
</svg>"""


def svg_sequence():
    xs = {"User": 55, "MainForm": 205, "GameEngine": 375, "ScoreCard": 530, "Scorer": 680}
    s = ['<svg viewBox="0 0 760 450" width="100%" font-family="Segoe UI, Arial">',
         '<defs><marker id="a4" markerWidth="10" markerHeight="8" refX="9" refY="4" orient="auto"><path d="M0,0 L10,4 L0,8 z" fill="#333"/></marker></defs>']
    for name, x in xs.items():
        s.append(f'<rect x="{x - 55}" y="10" width="110" height="26" rx="4" fill="#eef4fb" stroke="#2b5d8a"/>'
                 f'<text x="{x}" y="28" text-anchor="middle" font-size="11.5" font-weight="700">{name}</text>'
                 f'<line x1="{x}" y1="36" x2="{x}" y2="440" stroke="#999" stroke-dasharray="4 4"/>')
    msgs = [("User", "MainForm", "1. clicks a preview value", False),
            ("MainForm", "MainForm", "2. IsScorableCell? confirm if 0 points", False),
            ("MainForm", "GameEngine", "3. Score(category)", False),
            ("GameEngine", "ScoreCard", "4. Record(category, dice)", False),
            ("ScoreCard", "Scorer", "5. Score(category, dice)", False),
            ("Scorer", "ScoreCard", "6. points", True),
            ("ScoreCard", "GameEngine", "7. points (box filled)", True),
            ("GameEngine", "MainForm", "8. CategoryScored", True),
            ("GameEngine", "GameEngine", "9. next player, or game over", False),
            ("GameEngine", "MainForm", "10. TurnStarted / GameOver", True),
            ("GameEngine", "MainForm", "11. StateChanged", True),
            ("MainForm", "User", "12. redrawn window", True)]
    y = 62
    for a, b, text, dashed in msgs:
        xa, xb = xs[a], xs[b]
        dash = ' stroke-dasharray="5 3"' if dashed else ""
        if a == b:
            s.append(f'<path d="M {xa} {y} L {xa + 30} {y} L {xa + 30} {y + 14} L {xa + 2} {y + 14}" stroke="#333" fill="none"{dash} marker-end="url(#a4)"/>'
                     f'<text x="{xa + 36}" y="{y + 11}" font-size="10">{text}</text>')
            y += 34
        else:
            s.append(f'<line x1="{xa}" y1="{y}" x2="{xb + (-2 if xb > xa else 2)}" y2="{y}" stroke="#333"{dash} marker-end="url(#a4)"/>'
                     f'<text x="{(xa + xb) / 2}" y="{y - 4}" text-anchor="middle" font-size="10">{text}</text>')
            y += 31
    s.append("</svg>")
    return "".join(s)


def svg_ai():
    box = lambda x, y, w, h, t, fill="#eef4fb": (f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="8" fill="{fill}" stroke="#2b5d8a"/>'
        + "".join(f'<text x="{x + w / 2}" y="{y + 18 + i * 14}" text-anchor="middle" font-size="10.5">{line}</text>' for i, line in enumerate(t)))
    dia = lambda cx, cy, t: (f'<polygon points="{cx},{cy - 30} {cx + 90},{cy} {cx},{cy + 30} {cx - 90},{cy}" fill="#fff7e0" stroke="#b58900"/>'
        f'<text x="{cx}" y="{cy + 4}" text-anchor="middle" font-size="10.5">{t}</text>')
    ln = lambda x1, y1, x2, y2, t="", tx=0, ty=0: (f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="#333" stroke-width="1.3" marker-end="url(#a5)"/>'
        + (f'<text x="{tx}" y="{ty}" font-size="10">{t}</text>' if t else ""))
    return f"""<svg viewBox="0 0 760 360" width="100%" font-family="Segoe UI, Arial">
<defs><marker id="a5" markerWidth="10" markerHeight="8" refX="9" refY="4" orient="auto"><path d="M0,0 L10,4 L0,8 z" fill="#333"/></marker></defs>
{box(20, 10, 170, 40, ["Timer tick (every 800 ms)", "MainForm.PlayAiStep()"], "#e8f5ec")}
{ln(105, 50, 105, 78)}
{dia(105, 110, "RollsUsed == 0 ?")}
{ln(195, 110, 268, 110, "yes", 222, 104)}
{box(270, 88, 170, 44, ["Roll()", "return Rolled"])}
{ln(105, 140, 105, 178, "no", 112, 162)}
{dia(105, 210, "RollsRemaining > 0 ?")}
{ln(195, 210, 268, 210, "yes", 222, 204)}
{box(270, 188, 170, 44, ["holds = strategy.", "ChooseHolds(dice, ...)"])}
{ln(440, 210, 468, 210)}
{dia(560, 210, "holds == null ?")}
{ln(560, 180, 560, 134, "no", 566, 160)}
{box(470, 88, 180, 44, ["apply holds, Roll()", "return Rolled"])}
{ln(560, 240, 560, 288, "yes", 566, 268)}
{ln(105, 240, 105, 310, "no", 112, 280)}
{ln(105, 310, 468, 310)}
{box(470, 290, 270, 50, ["category = strategy.ChooseCategory(...)", "Score(category); return Scored (turn ends)"], "#fdecea")}
<text x="20" y="352" font-size="10" fill="#555">Each call performs exactly one visible action; StateChanged restarts the timer while the AI is still on turn.</text>
</svg>"""


def svg_loop():
    return """<svg viewBox="0 0 760 210" width="100%" font-family="Segoe UI, Arial">
<defs><marker id="a6" markerWidth="10" markerHeight="8" refX="9" refY="4" orient="auto"><path d="M0,0 L10,4 L0,8 z" fill="#333"/></marker></defs>
<rect x="10" y="70" width="150" height="60" rx="8" fill="#e8f5ec" stroke="#1b6b45"/><text x="85" y="95" text-anchor="middle" font-size="11.5" font-weight="700">User action</text>
<text x="85" y="112" text-anchor="middle" font-size="10">click, key, AI timer</text>
<rect x="205" y="70" width="160" height="60" rx="8" fill="#eef4fb" stroke="#2b5d8a"/><text x="285" y="95" text-anchor="middle" font-size="11.5" font-weight="700">GameEngine action</text>
<text x="285" y="112" text-anchor="middle" font-size="10">Roll / SetHeld / Score</text>
<rect x="410" y="70" width="150" height="60" rx="8" fill="#fdf6e3" stroke="#b58900"/><text x="485" y="95" text-anchor="middle" font-size="11.5" font-weight="700">StateChanged</text>
<text x="485" y="112" text-anchor="middle" font-size="10">event raised</text>
<rect x="605" y="70" width="145" height="60" rx="8" fill="#e8f5ec" stroke="#1b6b45"/><text x="677" y="95" text-anchor="middle" font-size="11.5" font-weight="700">RefreshUi()</text>
<text x="677" y="112" text-anchor="middle" font-size="10">reads state, redraws</text>
<line x1="160" y1="100" x2="203" y2="100" stroke="#333" stroke-width="1.4" marker-end="url(#a6)"/>
<line x1="365" y1="100" x2="408" y2="100" stroke="#333" stroke-width="1.4" marker-end="url(#a6)"/>
<line x1="560" y1="100" x2="603" y2="100" stroke="#333" stroke-width="1.4" marker-end="url(#a6)"/>
<path d="M 677 132 L 677 170 L 85 170 L 85 134" stroke="#333" stroke-width="1.4" fill="none" marker-end="url(#a6)"/>
<text x="380" y="164" text-anchor="middle" font-size="10.5">screen now shows the new state; buttons enabled from CanRoll / CanHold / CanScore</text>
<text x="380" y="40" text-anchor="middle" font-size="10.5" fill="#555">One-way data flow: the UI never edits state directly, so the screen can never disagree with the rules.</text>
</svg>"""


def svg_timeline():
    steps = [("1", "Model + Scorer", True), ("2", "GameEngine", True), ("3", "WinForms UI", True), ("4", "Save / load", True),
             ("5", "SQLite DB", True), ("6", "AI players", True), ("7", "Publish exe", True), ("R", "Report + video", False)]
    s = ['<svg viewBox="0 0 760 120" width="100%" font-family="Segoe UI, Arial">',
         '<line x1="50" y1="45" x2="710" y2="45" stroke="#bbb" stroke-width="4"/>']
    for i, (n, label, done) in enumerate(steps):
        x = 50 + i * 94
        fill = "#137333" if done else "#fff"
        tc = "#fff" if done else "#777"
        s.append(f'<circle cx="{x}" cy="45" r="18" fill="{fill}" stroke="{"#137333" if done else "#999"}" stroke-width="2"/>'
                 f'<text x="{x}" y="50" text-anchor="middle" font-size="13" font-weight="700" fill="{tc}">{n}</text>'
                 f'<text x="{x}" y="84" text-anchor="middle" font-size="10.5">{label}</text>'
                 f'<text x="{x}" y="99" text-anchor="middle" font-size="9.5" fill="{"#137333" if done else "#c5221f"}">{"done" if done else "to do"}</text>')
    s.append("</svg>")
    return "".join(s)


def svg_pips():
    cells = ""
    for c in (1, 2, 3):
        cells += f'<line x1="{20 + c * 45}" y1="20" x2="{20 + c * 45}" y2="200" stroke="#9ab" stroke-dasharray="3 3"/>'
        cells += f'<line x1="20" y1="{20 + c * 45}" x2="200" y2="{20 + c * 45}" stroke="#9ab" stroke-dasharray="3 3"/>'
    pips = "".join(f'<circle cx="{20 + (c + 1) * 45}" cy="{20 + (r + 1) * 45}" r="16" fill="#222"/>'
                   f'<text x="{20 + (c + 1) * 45}" y="{20 + (r + 1) * 45 + 30}" text-anchor="middle" font-size="9" fill="#c5221f">({c},{r})</text>'
                   for c, r in [(0, 0), (2, 0), (1, 1), (0, 2), (2, 2)])
    return f"""<svg viewBox="0 0 520 230" width="80%" font-family="Segoe UI, Arial">
<rect x="20" y="20" width="180" height="180" rx="30" fill="#fff" stroke="#333" stroke-width="2"/>{cells}{pips}
<text x="230" y="50" font-size="11.5" font-weight="700">Face 5: PipLayout[5]</text>
<text x="230" y="70" font-size="10.5">= [(0,0), (2,0), (1,1), (0,2), (2,2)]</text>
<text x="230" y="100" font-size="10.5">cell = width / 4: grid lines at 1/4, 1/2, 3/4</text>
<text x="230" y="118" font-size="10.5">pip centre x = left + cell * (col + 1)</text>
<text x="230" y="136" font-size="10.5">pip centre y = top  + cell * (row + 1)</text>
<text x="230" y="154" font-size="10.5">pip radius   = width / 11</text>
<text x="230" y="182" font-size="10.5">Corner radius of the face = width / 6</text>
</svg>"""


def body(img, annotated_ui, total_lines, total_segs):
    extra_sections = new_sections()
    return f"""
<h2 id="s1" class="newpage">1. Is the project complete? Status against the specification</h2>
<p><b>The program is complete:</b> every feature in the specification's checklist is implemented, unit tested, and was exercised end to end through the real
user interface of the published executable. <b>The submission is not complete yet:</b> the report, the narrated video and a test on a lab machine are your work.</p>
<h4>Feature checklist (the exact rows required by Appendix A of the specification)</h4>
<table class="t"><tr><th style="width:42%">Feature</th><th style="width:22%">Status</th><th>Where / how it was verified</th></tr>
<tr><td>Single-player gameplay</td><td class="ok">fully implemented</td><td>Full 13-round game played through the UI.</td></tr>
<tr><td>Multi-player gameplay</td><td class="ok">fully implemented</td><td>Hotseat, up to 6 players (any mix of humans and computers).</td></tr>
<tr><td>Specifying player names</td><td class="ok">fully implemented</td><td>New-game dialog; blank/duplicate names rejected.</td></tr>
<tr><td>Displaying whose turn it is</td><td class="ok">fully implemented</td><td>Heading plus highlighted scorecard column.</td></tr>
<tr><td>Displaying the number of rolls available</td><td class="ok">fully implemented</td><td>\"Rolls left: n of 3\".</td></tr>
<tr><td>Indicating which dice will be rolled/set aside</td><td class="ok">fully implemented</td><td>Gold HELD dice vs WILL ROLL captions; roll button counts dice.</td></tr>
<tr><td>Score preview</td><td class="ok">fully implemented</td><td>\"This roll\" column after every roll.</td></tr>
<tr><td>Saving/loading games</td><td class="ok">fully implemented</td><td>Save after any roll (Ctrl+S), JSON <code>.yahtzee</code> file, validated on load; AI turns resume.</td></tr>
<tr><td>Offer to save the game before exiting</td><td class="ok">fully implemented</td><td>Yes/No/Cancel <i>only when there are unsaved changes</i> (also before New Game / Open).</td></tr>
<tr><td>Result summary at the end of the game</td><td class="ok">fully implemented</td><td>Results dialog, ordered by score, ties share a rank.</td></tr>
<tr><td>Games database</td><td class="ok">fully implemented</td><td>SQLite; every finished game recorded; top 5 overall and per player, with dates.</td></tr>
<tr><td>AI opponents</td><td class="ok">fully implemented</td><td>Three strategies (easy/medium/hard); averages 39 / 176 / 228 points.</td></tr>
</table>
<h4>Deliverables required for submission (section 6 of the specification)</h4>
<table class="t"><tr><th style="width:42%">Deliverable</th><th style="width:22%">Status</th><th>Notes</th></tr>
<tr><td>Complete source code</td><td class="ok">done</td><td>Zip without <code>bin/</code>, <code>obj/</code>, <code>.vs/</code>, <code>publish/</code> build folders.</td></tr>
<tr><td>Standalone executable (Windows 11)</td><td class="ok">built</td><td><code>publish\\Yahtzee.exe</code> (53 MB, needs no .NET). Passed the end-to-end UI test here;
<b>still test it on a lab machine</b>.</td></tr>
<tr><td>Report (PDF, max 10 pages, 7 sections)</td><td class="no">your work</td><td>Must be written and evaluated by you; see section 13.</td></tr>
<tr><td>Video with voice-over showing every feature</td><td class="no">your work</td><td><b>Mandatory</b>: without it the submission is incomplete and may score 0. Features not
shown count as not implemented.</td></tr>
</table>
<h4>Marks (from the marking scheme)</h4>
<table class="t"><tr><th>Criterion</th><th>Marks</th><th>Covered by the program?</th></tr>
<tr><td>Single-player gameplay</td><td>5</td><td class="ok">yes</td></tr>
<tr><td>Multi-player gameplay</td><td>10</td><td class="ok">yes</td></tr>
<tr><td>Score preview</td><td>5</td><td class="ok">yes</td></tr>
<tr><td>Saving/loading game</td><td>10</td><td class="ok">yes</td></tr>
<tr><td>GUI quality</td><td>5</td><td class="ok">yes (judged by the marker)</td></tr>
<tr><td>Games database</td><td>15</td><td class="ok">yes</td></tr>
<tr><td>AI opponents</td><td>10</td><td class="ok">yes, with three strategies</td></tr>
<tr><td>Code organisation and quality</td><td>20</td><td class="ok">layered design, 130 tests, XML docs, modern C# (judged by the marker)</td></tr>
<tr><td>Report quality</td><td>15</td><td class="no">your report</td></tr>
<tr><td>Video demonstration</td><td>5</td><td class="no">your video</td></tr>
</table>
<div class="note">Every feature only counts if it is <b>shown in your video</b>. Deadline: <b>29 October 2026</b>.</div>

<h2 id="s2" class="newpage">2. The assignment in brief</h2>
<p>The specification (<code>F20SC_Coursework1.pdf</code>, 6 pages) asks for an <b>individual</b> implementation of a simplified Yahtzee in C# with a graphical user interface,
targeting Visual Studio 2026 on Windows 11. The key points that shaped the design:</p>
<table class="t"><tr><th style="width:30%">Area</th><th>Requirement (MoSCoW wording from the spec)</th></tr>
<tr><td>GUI</td><td><b>Must</b> provide a GUI for starting, playing, saving and loading games.</td></tr>
<tr><td>Players</td><td><b>Must</b> allow single-player; <b>should</b> allow hotseat multi-player (a player limit is allowed); <b>must</b> let users name each player.</td></tr>
<tr><td>During a turn</td><td><b>Must</b> show whose turn it is and how many rolls remain; <b>must</b> show which dice will be rolled and which are set aside;
<b>should</b> show a score preview for every available category after each roll.</td></tr>
<tr><td>Saving</td><td><b>Should</b> allow saving after any roll with a self-designed save-file format; <b>should</b> offer to save when exiting an unsaved game.</td></tr>
<tr><td>End of game</td><td><b>Must</b> display names and scores ordered by score.</td></tr>
<tr><td>Database</td><td><b>Should</b> record completed games (date, names, scores) using a database engine; <b>should</b> show the top 5 overall and the top 5 of a player;
<b>could</b> show the date of each high score.</td></tr>
<tr><td>AI</td><td><b>Should</b> allow computer opponents (even trivial); <b>could</b> offer different strategies.</td></tr>
<tr><td>Rules (Appendix B)</td><td>13 rounds; up to 3 rolls; hold any dice between rolls; 13 categories each used once; non-matching dice score 0;
total = sum of the 13 boxes; <b>a Yahtzee is not a valid full house</b>; no bonuses.</td></tr>
<tr><td>Submission</td><td>Zip with source, standalone executable and report PDF; narrated video (mandatory); possible in-person demo.</td></tr>
<tr><td>GenAI policy</td><td>Level 6: GenAI may be used, but you must evaluate its output yourself, be able to explain all code, and include your prompts and the model's
responses in the report's appendix.</td></tr>
</table>

<h2 id="s3" class="newpage">3. Development log: every step performed</h2>
<figure>{svg_timeline()}<figcaption>Figure 1. The seven implementation steps from the plan in CLAUDE.md, plus the report and video.</figcaption></figure>
<p>The work was done in conversation sessions with Claude Code on 6&ndash;7 October 2026. Each prompt is also recorded in <code>docs/genai-log.md</code>
for the GenAI appendix. Below, everything that was done, including problems and how they were fixed.</p>

<h3>Session 1: reading the specification and planning (prompt: <i>/init &hellip; read the pdf and implement a plan &hellip;</i>)</h3>
<ol>
<li>Listed the project folder: it contained only <code>F20SC_Coursework1.pdf</code>; it was not a git repository.</li>
<li>Read all six pages of the PDF: overview, lab environment, functional requirements (3.1 basic, 3.2 advanced), report format, video format, submission and marking
scheme, GenAI policy, academic integrity, Appendix A (feature checklist) and Appendix B (simplified rules).</li>
<li>Checked for a .NET SDK with <code>dotnet --list-sdks</code>: <b>none installed</b>.</li>
<li>Chose the technology: .NET 10 (LTS, ships with VS 2026), Windows Forms (recommended by the spec and the simplest), JSON save files
(<code>System.Text.Json</code>, built in), SQLite via <code>Microsoft.Data.Sqlite</code> (a real database engine with no server), xUnit for tests.</li>
<li>Wrote <code>CLAUDE.md</code>: project context, planned commands, layered architecture, design rules, the simplified scoring rules, the planned save-file format and
database schema, a requirement-to-class map, the 7-step order of work, and the submission format.</li>
<li>Pointed out that the specification says the coursework is an <b>individual</b> project (the request had called it a group project).</li>
</ol>

<h3>Session 2: step 1, the model and the scoring rules (prompt: <i>Proceed with step 1.</i>)</h3>
<ol>
<li>Created <code>Yahtzee.slnx</code>, <code>Directory.Build.props</code> (latest C#, nullable, implicit usings, warnings as errors) and <code>.gitignore</code>.</li>
<li>Created the <code>Yahtzee.Core</code> class library with XML documentation generation, then wrote <code>Category</code>, <code>Scorer</code>,
<code>IDiceRoller</code>/<code>RandomDiceRoller</code>, <code>DiceCup</code> and <code>ScoreCard</code>.</li>
<li>Created the xUnit test project with <code>FixedDiceRoller</code> and the tests <code>ScorerTests</code>, <code>ScoreCardTests</code>, <code>DiceCupTests</code>.</li>
<li>Replaced collection expressions inside <code>Assert.Equal(...)</code> calls with <code>new[] {{ ... }}</code> arrays, because collection expressions there can make the
overload choice ambiguous.</li>
<li>Installed the .NET 10 SDK (10.0.401) <b>temporarily</b>, into the session's scratch folder (not system-wide), using Microsoft's <code>dotnet-install.ps1</code> script.</li>
<li>Ran <code>dotnet test</code>: build clean, <b>57 tests passed</b>.</li>
<li>Updated <code>CLAUDE.md</code> and created <code>docs/genai-log.md</code>.</li>
</ol>

<h3>Session 3: step 2, the game engine (prompt: <i>proceed with step 2.</i>)</h3>
<ol>
<li>Wrote <code>IPlayerStrategy</code> (the AI contract), <code>Player</code>, <code>GameEvents</code> (event records) and <code>GameEngine</code>.</li>
<li>Design decisions: the round number is <i>derived</i> rather than stored; <code>CanRoll</code>/<code>CanHold</code>/<code>CanScore</code> guard every action; a
<code>StateChanged</code> event after every change; <code>PlayAiStep()</code> performs one visible AI action at a time; time comes from an injected <code>TimeProvider</code>.</li>
<li>Added an explicit index check to <code>SetHeld</code> so a bad index gives <code>ArgumentOutOfRangeException</code> rather than an indexing error.</li>
<li>Wrote <code>TestDoubles.cs</code>, <code>PlayerTests</code> and <code>GameEngineTests</code>.</li>
<li>Fixed problems found by the build: one test scripted 10 die values but needed 11 (the third AI roll re-rolls three dice); the xUnit analyzer rule
<b>xUnit2013</b> rejected <code>Assert.Equal(1, collection.Count)</code>, replaced with <code>Assert.Single(...)</code>.</li>
<li>Result: <b>88 tests passed</b>. Updated <code>CLAUDE.md</code> and the GenAI log (and fixed the order of two log rows).</li>
</ol>

<h3>Session 4: step 3, the Windows Forms interface (prompts: <i>start with step 3.</i> / <i>continue</i>)</h3>
<ol>
<li>Created <code>Yahtzee.WinForms</code> (WinExe, <code>net10.0-windows</code>, PerMonitorV2 DPI, output <code>Yahtzee.exe</code>) and added it to the solution.</li>
<li>Wrote <code>Program</code>, <code>UiTheme</code>, <code>DieControl</code>, <code>PlayerTypeOption</code>, <code>NewGameDialog</code>, <code>ResultsForm</code> and
<code>MainForm</code>. Forms are built in code (no designer files) with layout panels.</li>
<li>Fixed compile issues before building: a <code>(int)</code> cast for <code>Keys</code> arithmetic and a missing <code>using</code> in <code>Program.cs</code>.</li>
<li>The first build attempt was interrupted (the tool permission prompt closed); the next day the build succeeded with <b>0 warnings, 0 errors</b>.</li>
<li><b>Verified the real app</b> by driving it with Windows UI Automation from PowerShell scripts and taking screenshots: welcome screen, new-game dialog,
a roll with two held dice, the zero-score confirmation, a complete 13-round single-player game, the results dialog and the final board.</li>
<li>Testing found two <b>display-scaling (DPI) bugs</b> on this 150% display: scorecard rows and headers were not scaled (fixed with
<code>LogicalToDeviceUnits</code> when the grid is built), and the results list's column widths left an empty strip (fixed by sizing columns in the
<code>Load</code> event). Both were re-tested.</li>
<li>Re-ran all tests (88 passing). Updated <code>CLAUDE.md</code> (WinForms conventions, DPI notes) and the GenAI log.</li>
</ol>

<h3>Session 5: this guide (prompt: <i>Is the project complete &hellip; prepare a PDF &hellip;</i>)</h3>
<ol>
<li>Answered the status question (section 1).</li>
<li>Retook the screenshots after the DPI fixes. While doing so, the first version of the screenshot script found its window by the title
\"Yahtzee\". Because you had your own copy of the game open, two runs may have sent <code>Ctrl+N</code> and a \"Start Game\" click to <b>your</b> window and started
a new two-player game in it. The script was changed to only control the process it starts itself.</li>
<li>Generated the first version of this document: a Python script reads every source file, attaches explanations to specific source lines (the build stops if
a line it refers to cannot be found, so explanations cannot silently drift from the code), adds diagrams and screenshots, and Microsoft Edge prints it to PDF.</li>
</ol>

<h3>Session 6: steps 4&ndash;7 and the guide generator (prompt: <i>okay continue with next steps and also add the pdf script</i>)</h3>
<ol>
<li><b>Step 4, saving and loading.</b> Wrote <code>GameSnapshot</code>/<code>PlayerSnapshot</code>, <code>SaveGameSerializer</code> (strict JSON, versioned, safe write via a
temporary file), <code>InvalidSaveFileException</code>, and <code>GameEngine.Persistence.cs</code> (<code>CreateSnapshot</code>, a validating <code>Restore</code>). The
engine became a <code>partial</code> class with a private shared constructor. 21 tests (109 in total) passed first time.</li>
<li><b>Step 5, database.</b> Added <code>Microsoft.Data.Sqlite</code> 10.0.12 (latest stable, checked on NuGet), <code>IGameHistoryRepository</code> and
<code>SqliteGameHistoryRepository</code> (schema created on first use, one transaction per game, parameterised queries). 7 tests including an SQL-injection attempt.</li>
<li><b>Step 6, AI.</b> <code>RandomStrategy</code>, <code>GreedyStrategy</code>, <code>ExpectedValueStrategy</code>, <code>StrategyCatalog</code> and <code>ScoringHeuristics</code>.
14 tests, including 30 seeded games per strategy. Fixes: an <code>ITestOutputHelper</code> namespace import, and a test comment that described the wrong dice.
Result: <b>130 tests passing</b>.</li>
<li><b>User interface.</b> Game menu (Open, Save, Save As, High Scores), unsaved-changes tracking with a change counter, the Save/Don't save/Cancel prompt,
<code>ShowModal</code> (pauses computer players during dialogs), recording finished games, <code>HighScoresForm</code>, computer types in the new-game dialog
(automatically renamed \"Computer n\"), and a <code>--database</code> command-line option so automated tests never touch the real high scores.</li>
<li><b>End-to-end UI test</b> (UI Automation script against the app's own process and a throwaway database). It created a game against the hard computer,
saved after a roll, played on, and checked that exiting asks to save (Cancel keeps the window open, No exits). It then reopened the save, confirmed it resumed at
the saved roll, played the game to the end, checked the results and high-score windows, and checked that a finished game exits without a prompt. Two failures
were problems in the test script, not the app: it treated the disabled Roll button of the AI's turn as a human turn, and it gave up too soon waiting for a
zero-score prompt.</li>
<li><b>Step 7, executable.</b> Added the <code>Standalone.pubxml</code> publish profile (self-contained, single file, compressed, no extra files):
<code>publish\\Yahtzee.exe</code>, 53 MB. The whole end-to-end UI test passed against the published exe.</li>
<li>Moved this guide's generator into the project (<code>docs/guide-src/</code>), retook all screenshots, added explanations for every new file, and updated
<code>CLAUDE.md</code> and the GenAI log. The walkthrough now covers <b>{total_lines} source lines in {total_segs} explained segments</b>.</li>
</ol>
<div class="note"><b>Nothing outside the project was changed permanently.</b> The temporary .NET SDK and the UI-test scripts live in the session's scratch folder.
The project files are the ones described in sections 9&ndash;12, plus <code>CLAUDE.md</code>, <code>docs/genai-log.md</code>, this PDF and its generator in
<code>docs/guide-src/</code>.</div>

<h2 id="s4" class="newpage">4. Building, running and testing</h2>
<h4>Folder structure</h4>
<pre class="tree">yahtzee-proj/
├── F20SC_Coursework1.pdf          the specification
├── CLAUDE.md                      plan and conventions (guidance for future AI sessions)
├── Yahtzee.slnx                   solution (open this in Visual Studio 2026)
├── Directory.Build.props          settings shared by all projects
├── .gitignore
├── docs/
│   ├── genai-log.md               prompts and outcomes, for the report's GenAI appendix
│   ├── Yahtzee_Code_Guide.pdf     this document
│   └── guide-src/                 the generator of this document (Python + Edge)
├── publish/Yahtzee.exe            the standalone executable (created by dotnet publish)
├── src/
│   ├── Yahtzee.Core/              game logic library
│   │   ├── AI/                    IPlayerStrategy, Random/Greedy/ExpectedValue strategies, StrategyCatalog, ScoringHeuristics
│   │   ├── Data/                  IGameHistoryRepository, SqliteGameHistoryRepository
│   │   ├── Game/                  GameEngine.cs, GameEngine.Persistence.cs, GameEvents.cs
│   │   ├── Model/                 Category, DiceCup, IDiceRoller, Player, ScoreCard
│   │   ├── Persistence/           GameSnapshot, SaveGameSerializer, InvalidSaveFileException
│   │   └── Rules/                 Scorer
│   └── Yahtzee.WinForms/          the application (Yahtzee.exe)
│       ├── Program.cs, UiTheme.cs
│       ├── Controls/DieControl.cs
│       ├── Forms/                 MainForm, NewGameDialog, ResultsForm, HighScoresForm, PlayerTypeOption
│       └── Properties/PublishProfiles/Standalone.pubxml
└── tests/Yahtzee.Core.Tests/      xUnit tests (130)</pre>
<h4>Commands</h4>
<pre class="cmd"># build everything (warnings are errors)
dotnet build Yahtzee.slnx

# run the game
dotnet run --project src/Yahtzee.WinForms

# run all tests, one class, or one test
dotnet test
dotnet test --filter "FullyQualifiedName~ScorerTests"
dotnet test --filter "FullyQualifiedName~ScorerTests.FullHouse_RejectsYahtzee"

# standalone executable for submission: publish\\Yahtzee.exe (needs no .NET installation)
dotnet publish src/Yahtzee.WinForms -p:PublishProfile=Standalone

# run with a separate games-history database (e.g. a clean one for the video)
publish\\Yahtzee.exe --database C:\\temp\\demo.db

# rebuild this PDF after changing the code (needs Python 3 with Pillow, and Microsoft Edge)
powershell -File docs\\guide-src\\build_guide.ps1</pre>
<p>In Visual Studio 2026: open <code>Yahtzee.slnx</code>, set <b>Yahtzee.WinForms</b> as the startup project, press F5. Tests appear in <b>Test &rarr; Test Explorer</b>.</p>
<div class="note">Where things are stored: save files wherever you choose (extension <code>.yahtzee</code>); the games history in
<code>%LOCALAPPDATA%\\Yahtzee\\yahtzee.db</code> (delete that file to reset the high scores). If <code>dotnet</code> is not found in a terminal, install the .NET 10 SDK
(Visual Studio 2026 includes it).</div>

<h2 id="s5" class="newpage">5. Architecture and design</h2>
<h3>5.1 Layers</h3>
<p>The solution is split so that <b>all rules live in a UI-free library</b>. This is the single most important design decision: the rules can be unit tested
without a window, the UI cannot accidentally break them, and another front end (for example MAUI) could reuse the library unchanged.</p>
<figure>{svg_architecture()}<figcaption>Figure 2. Projects and namespaces.</figcaption></figure>
<h3>5.2 Classes and their relationships</h3>
<figure>{svg_classes()}<figcaption>Figure 3. Class diagram of Yahtzee.Core (main members only).</figcaption></figure>
<table class="t"><tr><th style="width:22%">Class</th><th>Responsibility (one job each)</th></tr>
<tr><td><code>Scorer</code></td><td>Pure scoring arithmetic for one category and five dice.</td></tr>
<tr><td><code>ScoreCard</code></td><td>One player's 13 boxes: once-only rule, totals, preview.</td></tr>
<tr><td><code>DiceCup</code></td><td>Five values and held flags; re-rolls the unheld dice.</td></tr>
<tr><td><code>IDiceRoller</code></td><td>Where randomness comes from (swappable for tests).</td></tr>
<tr><td><code>Player</code></td><td>Name + scorecard + optional AI strategy.</td></tr>
<tr><td><code>IPlayerStrategy</code></td><td>AI decisions only (which dice to hold, which category).</td></tr>
<tr><td><code>GameEngine</code></td><td>Turn order, roll limit, legality of every action, game end, events.</td></tr>
<tr><td><code>MainForm</code> etc.</td><td>Displaying state and forwarding user input; no rules.</td></tr>
</table>
<h3>5.3 The turn state machine</h3>
<figure>{svg_states()}<figcaption>Figure 4. States of a turn as enforced by GameEngine.</figcaption></figure>
<h3>5.4 Event-driven updates</h3>
<figure>{svg_loop()}<figcaption>Figure 5. Every change goes through the engine and comes back to the screen via StateChanged.</figcaption></figure>
<figure>{svg_sequence()}<figcaption>Figure 6. Sequence of calls when a human scores a category. Step 8 updates the status bar; step 10 shows the results
dialog (via BeginInvoke) if the game ended; step 11 makes RefreshUi() redraw everything from the engine's state.</figcaption></figure>
<h3>5.5 How computer players take turns</h3>
<figure>{svg_ai()}<figcaption>Figure 7. GameEngine.PlayAiStep(): one visible action per timer tick.</figcaption></figure>
<h3>5.6 Design decisions and why (useful for the report's \"Design considerations\")</h3>
<table class="t"><tr><th style="width:30%">Decision</th><th>Reason / trade-off</th></tr>
<tr><td>Windows Forms over MAUI/WPF</td><td>Recommended by the spec as the simplest; target platform is Windows only. Trade-off: not cross-platform.</td></tr>
<tr><td>Core library with no UI references</td><td>Testability and separation of concerns; the developer guide can describe rules independently of the GUI.</td></tr>
<tr><td>Frequency table in <code>Scorer</code></td><td>Counting faces once (an <code>int[7]</code>) makes every rule a one-liner and runs in constant time.</td></tr>
<tr><td>Dictionary for the scorecard</td><td>An absent key = empty box; avoids parallel \"filled\" flags; O(1) lookups.</td></tr>
<tr><td>Round derived, not stored</td><td>No duplicated state to keep consistent or to save.</td></tr>
<tr><td><code>Can…</code> guards + exceptions</td><td>The UI enables controls from the guards; the engine still rejects illegal calls, so rules hold even if the UI has a bug.</td></tr>
<tr><td>Events + one refresh method</td><td>One-way data flow; the screen always matches the state; also gives the save-before-exit \"dirty\" flag for free (step 4).</td></tr>
<tr><td>Strategy pattern for AI</td><td>Strategies only decide, the engine applies: AIs cannot cheat; new strategies plug in without changing the engine.</td></tr>
<tr><td>Injected randomness and clock</td><td>Deterministic tests (<code>FixedDiceRoller</code>, <code>ManualTimeProvider</code>).</td></tr>
<tr><td>Stable enum values</td><td>The save format and database will store category numbers.</td></tr>
<tr><td>Warnings as errors + XML docs</td><td>Keeps code clean and documented (code-quality marks).</td></tr>
<tr><td>Forms built in code, custom-drawn dice</td><td>Readable diffs and explicit layout; dice stay sharp at any display scale.</td></tr>
<tr><td>JSON save files with a version number</td><td>Human-readable, built into .NET, easy to extend; old files are recognised by their version.</td></tr>
<tr><td>Strict parsing + logical validation on load</td><td>A damaged or hand-edited file gives a clear message instead of a crash or an impossible game.</td></tr>
<tr><td>Write to a temporary file, then move</td><td>A crash during saving can never destroy the previous save.</td></tr>
<tr><td>Change counter instead of a dirty flag</td><td>Stays correct when an AI move happens during an asynchronous save.</td></tr>
<tr><td>Repository interface + SQLite</td><td>A real database engine with no server or installation; the UI depends only on the interface.</td></tr>
<tr><td>One transaction per game, parameterised SQL</td><td>No half-recorded games; player names can never inject SQL.</td></tr>
<tr><td>Strategy catalog by name</td><td>One list feeds the new-game dialog and save loading; adding a strategy is one line.</td></tr>
<tr><td>Expected-value AI with a cache</td><td>Exact probabilities instead of guesses; caching by sorted dice (252 hands) keeps each decision to milliseconds.</td></tr>
<tr><td>async/await for files and database</td><td>The window never freezes while the disk is busy.</td></tr>
</table>
{extra_sections}

<h2 id="s6" class="newpage">6. The scoring rules, illustrated</h2>
<p>All scoring goes through <code>Scorer.Score</code>. It first converts the five dice into a <b>frequency table</b> (how many dice show each face), then each rule is a
simple question about that table. Worked example with the dice from the screenshot in section 7 (<span class="dice"><span>4</span><span>1</span><span>6</span><span>6</span><span>1</span></span>):</p>
<div class="two">
<table class="t"><tr><th>Face</th><th>1</th><th>2</th><th>3</th><th>4</th><th>5</th><th>6</th></tr>
<tr><td><b>counts[face]</b></td><td>2</td><td>0</td><td>0</td><td>1</td><td>0</td><td>2</td></tr></table>
<table class="t"><tr><th>Derived value</th><th>Result</th></tr><tr><td>sum of dice</td><td>18</td></tr><tr><td>largest count</td><td>2</td></tr>
<tr><td>longest run of present faces</td><td>1 (faces 1, 4, 6 are not consecutive)</td></tr></table>
</div>
<table class="t"><tr><th style="width:22%">Category</th><th style="width:40%">Rule in code</th><th>Example result</th></tr>
<tr><td>Aces &hellip; Sixes</td><td><code>counts[face] * face</code></td><td>Aces 2&times;1 = <b>2</b>, Fours <b>4</b>, Sixes 2&times;6 = <b>12</b>, others 0</td></tr>
<tr><td>Chance</td><td><code>sum</code></td><td><b>18</b></td></tr>
<tr><td>Three / Four of a Kind</td><td><code>counts.Max() &gt;= 3 (4) ? sum : 0</code></td><td>largest count 2 &rarr; <b>0</b></td></tr>
<tr><td>Full House</td><td><code>counts.Contains(3) &amp;&amp; counts.Contains(2) ? 25 : 0</code></td><td>no face appears 3 times &rarr; <b>0</b></td></tr>
<tr><td>Small Straight</td><td><code>LongestRun(counts) &gt;= 4 ? 30 : 0</code></td><td>run 1 &rarr; <b>0</b></td></tr>
<tr><td>Large Straight</td><td><code>LongestRun(counts) == 5 ? 40 : 0</code></td><td><b>0</b></td></tr>
<tr><td>Yahtzee</td><td><code>counts.Contains(5) ? 50 : 0</code></td><td><b>0</b></td></tr>
</table>
<p>These are exactly the preview values shown by the application in that situation (Aces 2, Fours 4, Sixes 12, Chance 18, all others 0).</p>
<div class="box"><b>Why a Yahtzee is not a Full House:</b> five 4s give counts <code>[0,0,0,0,5,0,0]</code>. That table contains neither a 3 nor a 2, so the
Full House test fails, as the note under Figure 1 of the specification requires. A test (<code>FullHouse_RejectsYahtzee</code>) guards this.<br>
<b>Why a large straight also counts as a small straight:</b> 1-2-3-4-5 has a longest run of 5, which is &ge; 4.<br>
<b>Duplicates inside a straight</b> (2-3-4-5-5) are fine because the run only checks whether each face is present.</div>

<h2 id="s7" class="newpage">7. The user interface</h2>
{annotated_ui()}
<h3>Screens</h3>
{img("welcome", "Figure 10. Welcome screen before any game: start a new game or open a saved one.")}
<div class="two">{img("new-game", "Figure 11. New-game dialog: seat 2 switched to the hard computer and renamed automatically.")}
{img("zero-prompt", "Figure 12. Confirmation before scoring 0 (a common mis-click).")}</div>
{img("ai-turn", "Figure 13. After the computer's turn: its score appears in its column and the status bar reports the move.")}
{img("mid-game", "Figure 14. Mid-game against the computer: filled boxes disappear from the preview column; totals update.")}
<div class="two">{img("exit-prompt", "Figure 15. Exiting with unsaved changes: Yes saves, No discards, Cancel keeps playing.")}
{img("results", "Figure 16. End-of-game summary ordered by score.")}</div>
{img("loaded", "Figure 17. A saved game reopened: the title shows the file name, and the turn resumes at the saved roll (\"Re-roll 4 dice\").")}
{img("final-board", "Figure 18. The board after the game, with buttons to start or open another game.")}
{img("high-scores", "Figure 19. High scores from the database: top 5 overall and top 5 of a chosen player, with dates.", "75%")}
<h3>How requirements map to what you see</h3>
<table class="t"><tr><th style="width:38%">Requirement</th><th>Where on screen / in code</th></tr>
<tr><td>Whose turn it is</td><td>Heading (callout 2) and gold column (11) &mdash; <code>RefreshUi</code>, <code>RefreshScoreGrid</code>.</td></tr>
<tr><td>Rolls available</td><td>\"Rolls left\" (4) &mdash; <code>GameEngine.RollsRemaining</code>.</td></tr>
<tr><td>Which dice roll / are set aside</td><td>HELD / WILL ROLL (5&ndash;7), roll button text (8) &mdash; <code>DieControl</code>.</td></tr>
<tr><td>Score preview</td><td>\"This roll\" column (10) &mdash; <code>GameEngine.Preview()</code>.</td></tr>
<tr><td>Result summary</td><td>Figure 16 &mdash; <code>ResultsForm</code>, <code>GetStandings()</code>.</td></tr>
<tr><td>Saving/loading</td><td>Game menu (Ctrl+S, Ctrl+O), title bar, Figure 17 &mdash; <code>SaveGameAsync</code>, <code>OpenGameAsync</code>.</td></tr>
<tr><td>Offer to save before exiting</td><td>Figure 15 &mdash; <code>OnFormClosing</code>, <code>ConfirmUnsavedChangesAsync</code>.</td></tr>
<tr><td>Games database</td><td>Figure 19 (Ctrl+H) &mdash; <code>HighScoresForm</code>, <code>SqliteGameHistoryRepository</code>.</td></tr>
<tr><td>AI opponents</td><td>Figures 11, 13, 14 &mdash; strategies chosen per seat; AI moves every 0.8 s.</td></tr>
</table>
<h3>Drawing a die</h3>
<figure>{svg_pips()}<figcaption>Figure 20. How DieControl places pips: a 3&times;3 grid and a lookup table of positions per face.</figcaption></figure>
<h3>Display scaling (DPI): a bug found by testing</h3>
<p>Windows can scale the interface (this computer uses 150%). WinForms automatically scales control <i>sizes, positions and fonts</i> when a form uses
<code>AutoScaleMode.Dpi</code>, but some pixel values are <b>not</b> scaled: DataGridView row and header heights and ListView column widths. At 150% the scorecard rows
were therefore cramped. The fix converts those values with <code>LogicalToDeviceUnits</code> once the window exists (or sizes columns in the <code>Load</code>
event), so a \"32-pixel\" row becomes 48 physical pixels at 150%.</p>
<h3>Keyboard support</h3>
<p><b>Ctrl+N</b> new game &middot; <b>Ctrl+O</b> open &middot; <b>Ctrl+S</b> save &middot; <b>Ctrl+H</b> high scores &middot; <b>R</b> roll &middot; <b>1&ndash;5</b> hold/release a die &middot; <b>Tab</b> to a die then <b>Space/Enter</b> &middot;
<b>F1</b> how to play &middot; <b>Enter/Esc</b> in dialogs.</p>

<h2 id="s8" class="newpage">8. C# language features used</h2>
<p>The marking scheme rewards \"use of advanced language features\". These are the ones used so far, with an example of where to find each.</p>
<table class="t"><tr><th style="width:26%">Feature</th><th style="width:38%">Example</th><th>Where</th></tr>
<tr><td>Records</td><td><code>record PlayerStanding(int Rank, ...)</code></td><td>GameEvents.cs, PlayerTypeOption.cs, NewGameDialog.SeatRow</td></tr>
<tr><td>Primary constructors</td><td><code>class RandomDiceRoller(Random? random = null)</code></td><td>IDiceRoller.cs, test doubles</td></tr>
<tr><td>Switch expressions</td><td><code>category switch {{ ... }}</code></td><td>Scorer, Category.DisplayName, MainForm</td></tr>
<tr><td>Relational/logical patterns</td><td><code>is &gt;= Category.Aces and &lt;= Category.Sixes</code></td><td>Scorer, Category.Face, MainForm keys</td></tr>
<tr><td>Property patterns</td><td><code>_game is {{ IsGameOver: false, CurrentPlayer.IsAi: false }}</code></td><td>MainForm</td></tr>
<tr><td>Tuple patterns</td><td><code>(_value, _held, _showRollCaption) switch</code></td><td>DieControl.DrawCaption, ResultsForm.Ordinal</td></tr>
<tr><td>List patterns</td><td><code>[var only] when ...</code></td><td>ResultsForm.DescribeWinner</td></tr>
<tr><td>Collection expressions, spread</td><td><code>[.. players]</code>, <code>[]</code></td><td>GameEngine, ScoreCard, DieControl</td></tr>
<tr><td>Nullable reference types</td><td><code>IPlayerStrategy?</code>, <code>int?</code></td><td>everywhere</td></tr>
<tr><td>LINQ</td><td><code>OrderByDescending</code>, <code>DistinctBy</code>, <code>ToDictionary</code></td><td>GameEngine, Scorer, ScoreCard</td></tr>
<tr><td>Events and delegates</td><td><code>event EventHandler&lt;DiceRolledEventArgs&gt;?</code>, <code>Func&lt;IPlayerStrategy?&gt;</code></td><td>GameEngine, PlayerTypeOption</td></tr>
<tr><td>Lambdas</td><td><code>die.Click += (_, _) =&gt; ToggleDie(index);</code></td><td>MainForm, dialogs</td></tr>
<tr><td>Interfaces &amp; generics</td><td><code>IDiceRoller</code>, <code>IReadOnlyList&lt;int&gt;</code></td><td>Core</td></tr>
<tr><td>Extension methods</td><td><code>category.DisplayName()</code></td><td>Category.cs</td></tr>
<tr><td>Deconstruction</td><td><code>foreach (var (category, score) in scores)</code></td><td>ScoreCard, DieControl</td></tr>
<tr><td>Throw expressions</td><td><code>roller ?? throw new ...</code></td><td>DiceCup, GameEngine</td></tr>
<tr><td>File-scoped namespaces</td><td><code>namespace Yahtzee.Core.Model;</code></td><td>every file</td></tr>
<tr><td>XML documentation</td><td><code>/// &lt;summary&gt;</code></td><td>every public member of Core (enforced by the build)</td></tr>
<tr><td>async/await, Task&lt;T&gt;</td><td><code>await SaveGameSerializer.SaveAsync(...)</code></td><td>SaveGameSerializer, SqliteGameHistoryRepository, MainForm, HighScoresForm</td></tr>
<tr><td>Generic methods</td><td><code>T ShowModal&lt;T&gt;(Func&lt;T&gt; show)</code></td><td>MainForm</td></tr>
<tr><td>Partial classes</td><td><code>public sealed partial class GameEngine</code></td><td>GameEngine.cs + GameEngine.Persistence.cs</td></tr>
<tr><td>Static factory method</td><td><code>GameEngine.Restore(...)</code></td><td>GameEngine.Persistence.cs</td></tr>
<tr><td>Exception filters</td><td><code>catch (Exception ex) when (ex is IOException or ...)</code></td><td>MainForm, HighScoresForm</td></tr>
<tr><td>Custom exception type</td><td><code>InvalidSaveFileException</code></td><td>Persistence</td></tr>
<tr><td>Raw string literals</td><td><code>\"\"\" CREATE TABLE ... \"\"\"</code></td><td>SqliteGameHistoryRepository</td></tr>
<tr><td>Local functions</td><td><code>bool IsOpenAndMade(Category c) =&gt; ...</code></td><td>GreedyStrategy</td></tr>
<tr><td>Nested classes</td><td><code>PositionEvaluator</code></td><td>ExpectedValueStrategy</td></tr>
<tr><td>Bitwise operators</td><td><code>(mask &amp; (1 &lt;&lt; i)) != 0</code></td><td>ExpectedValueStrategy</td></tr>
<tr><td>Method groups as delegates</td><td><code>GameEngine.Restore(snapshot, roller, StrategyCatalog.Create)</code></td><td>MainForm, GameEngine.Persistence</td></tr>
<tr><td><code>with</code> expressions</td><td><code>snapshot with {{ RollsUsed = 4 }}</code></td><td>SaveGameTests</td></tr>
<tr><td>Repository and strategy patterns, dependency injection</td><td><code>new MainForm(history)</code></td><td>Program, MainForm</td></tr>
</table>

<h2 id="s8b" class="newpage">How to read the line-by-line walkthrough (sections 9&ndash;12)</h2>
<p>Every source file is reproduced <b>in full</b>, split into small consecutive segments. Each segment shows the code with its real line numbers, followed by an
explanation of those lines. Segments follow each other without gaps, so every line of every file is covered. Blank lines and closing braces are shown and belong
to the segment above them. Syntax colours follow Visual Studio: <span class="kw">keywords</span>, <span class="ty">type names</span>, <span class="str">strings</span>,
<span class="com">comments</span>, <span class="doc">documentation comments</span>.</p>
<p>Recommended reading order: <code>Category</code> &rarr; <code>Scorer</code> &rarr; <code>DiceCup</code> &rarr; <code>ScoreCard</code> &rarr; <code>Player</code> &rarr;
<code>GameEngine</code> &rarr; <code>MainForm</code>, with the tests next to the class they test.</p>
"""


def closing():
    return """
<h2 id="s13" class="newpage">13. What remains: report, video, lab test and submission</h2>
<p>All program features are finished. What is left is yours to do, and it is worth 20 marks directly (report 15, video 5). The video also decides whether
the 75 feature and quality marks count.</p>
<h3>1. Test the executable on a lab machine</h3>
<ul><li>Copy <code>publish\\Yahtzee.exe</code> to a Windows 11 lab PC (no installation needed) and play a short game, save, load, check the high scores.</li>
<li>Rebuild it at any time with <code>dotnet publish src/Yahtzee.WinForms -p:PublishProfile=Standalone</code>.</li></ul>
<h3>2. Your report (15 marks), max 10 pages, required structure</h3>
<ol><li><b>Introduction</b> (purpose, remit, assumptions, e.g. at most 6 players, simplified rules, Windows only).</li>
<li><b>Requirements checklist</b> with <b>exactly</b> the Appendix A rows (the table in section 1 has them all as fully implemented).</li>
<li><b>Design considerations</b>: section 5.6 lists the decisions and reasons; explain them in your own words.</li>
<li><b>Developer guide</b>: classes and responsibilities (5.2), interfaces, <b>save-file format</b> (5.7), <b>database organisation</b> (5.8).</li>
<li><b>GenAI usage statement</b>, with your prompts and the model's responses in an appendix (source: <code>docs/genai-log.md</code>; fill in the
\"my evaluation\" column yourself, since the policy requires you to evaluate the output).</li>
<li><b>Conclusion</b>: what you are proud of, what you would do differently.</li><li>References (optional).</li></ol>
<p>Declare the external dependency: <code>Microsoft.Data.Sqlite</code> 10.0.12 (bundling SQLitePCLRaw / native SQLite).</p>
<h3>3. Your video (5 marks, mandatory)</h3>
<p>Tip: start the program with <code>Yahtzee.exe --database C:\\temp\\video.db</code> for a clean high-score table, then narrate:</p>
<ol><li>New game: name the players, mix a human and a computer (and show the three computer levels).</li>
<li>A turn: whose turn, rolls left, rolling, holding dice (HELD / WILL ROLL), the score preview, scoring.</li>
<li>The computer taking its turn.</li>
<li>Save after a roll (Ctrl+S), close the program: show the save prompt (try Cancel, then exit), reopen with Ctrl+O and continue from the same roll.</li>
<li>A single-player game too (it can be quick: a few rounds, then show a finished one).</li>
<li>Finish a game: the results summary.</li>
<li>High scores (Ctrl+H): top 5 overall, top 5 of a player, with dates.</li></ol>
<h3>4. Submission checklist</h3>
<table class="t"><tr><th>Item</th><th>Done?</th></tr>
<tr><td>Zip: source code without bin/ obj/ .vs/ (docs/guide-src/build/ can be left out too)</td><td>&#9744;</td></tr>
<tr><td>Zip: standalone executable (publish\\Yahtzee.exe)</td><td>&#9744;</td></tr>
<tr><td>Zip: report PDF</td><td>&#9744;</td></tr><tr><td>Video uploaded to Canvas or OneDrive link in the report</td><td>&#9744;</td></tr>
<tr><td>Executable tested on a lab Windows 11 machine</td><td>&#9744;</td></tr><tr><td>GenAI appendix with prompts, responses and your evaluation</td><td>&#9744;</td></tr>
<tr><td>Able to explain every file (this guide)</td><td>&#9744;</td></tr><tr><td>Submitted on Canvas before 29 October 2026</td><td>&#9744;</td></tr></table>

<h2 id="s14">14. Glossary</h2>
<table class="t">
<tr><td style="width:24%"><b>Class library</b></td><td>A project compiled to a DLL that other projects use; it cannot run on its own.</td></tr>
<tr><td><b>DPI / display scaling</b></td><td>Windows enlarges the UI on high-resolution screens (125%, 150%&hellip;); programs must scale pixel sizes accordingly.</td></tr>
<tr><td><b>async / await</b></td><td>Lets slow work (disk, database) run without freezing the window; <code>await</code> resumes the method when the work is done.</td></tr>
<tr><td><b>Event</b></td><td>A notification a class raises (\"the dice were rolled\") that other code can subscribe to with a handler method.</td></tr>
<tr><td><b>Expected value</b></td><td>The average result over all possible outcomes, weighted by their probability.</td></tr>
<tr><td><b>Hotseat</b></td><td>Several people taking turns on the same computer.</td></tr>
<tr><td><b>Immutable</b></td><td>Cannot be changed after creation (e.g. records, get-only properties).</td></tr>
<tr><td><b>Interface</b></td><td>A contract listing members a class must implement; code can depend on the contract instead of a concrete class.</td></tr>
<tr><td><b>Modal dialog</b></td><td>A window that blocks the window behind it until closed (<code>ShowDialog</code>).</td></tr>
<tr><td><b>Parameterised query</b></td><td>SQL whose values are sent separately from the SQL text, which prevents SQL injection.</td></tr>
<tr><td><b>Partial class</b></td><td>One class whose code is split across several files.</td></tr>
<tr><td><b>Pure function</b></td><td>Output depends only on input; no side effects.</td></tr>
<tr><td><b>Repository</b></td><td>An object that hides how data is stored behind an interface of meaningful operations.</td></tr>
<tr><td><b>Self-contained publish</b></td><td>An executable that carries its own .NET runtime, so the PC needs nothing installed.</td></tr>
<tr><td><b>Snapshot</b></td><td>A copy of the complete game state at one moment, used for saving.</td></tr>
<tr><td><b>State machine</b></td><td>A design where an object is in one of a few states and only certain actions move it between them.</td></tr>
<tr><td><b>Strategy pattern</b></td><td>Interchangeable algorithms behind one interface (here: AI players).</td></tr>
<tr><td><b>Transaction</b></td><td>A group of database changes that are stored all together or not at all.</td></tr>
<tr><td><b>Test double</b></td><td>A stand-in object used in tests (fixed dice, fake clock, scripted AI).</td></tr>
<tr><td><b>UI Automation</b></td><td>The Windows accessibility API; used here by test scripts to click the real application and take screenshots.</td></tr>
<tr><td><b>xUnit [Fact] / [Theory]</b></td><td>A single test / a test run once per row of <code>[InlineData]</code>.</td></tr>
</table>
"""


def svg_save_flow():
    box = lambda x, y, w, t, fill="#eef4fb": (f'<rect x="{x}" y="{y}" width="{w}" height="46" rx="8" fill="{fill}" stroke="#2b5d8a"/>'
        + "".join(f'<text x="{x + w / 2}" y="{y + 19 + i * 15}" text-anchor="middle" font-size="10.5">{line}</text>' for i, line in enumerate(t)))
    ar = lambda x1, y1, x2, y2, t="": (f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="#333" stroke-width="1.3" marker-end="url(#a7)"/>'
        + (f'<text x="{(x1 + x2) / 2}" y="{y1 - 6}" text-anchor="middle" font-size="9.5">{t}</text>' if t else ""))
    return f"""<svg viewBox="0 0 760 190" width="100%" font-family="Segoe UI, Arial">
<defs><marker id="a7" markerWidth="10" markerHeight="8" refX="9" refY="4" orient="auto"><path d="M0,0 L10,4 L0,8 z" fill="#333"/></marker></defs>
<text x="10" y="18" font-size="11" font-weight="700" fill="#165e3c">Save</text>
{box(10, 26, 140, ["GameEngine", "CreateSnapshot()"])}{ar(150, 49, 196, 49)}
{box(198, 26, 130, ["GameSnapshot", "(records)"], "#fdf6e3")}{ar(328, 49, 374, 49)}
{box(376, 26, 150, ["SaveGameSerializer", "Serialize (JSON)"])}{ar(526, 49, 572, 49)}
{box(574, 26, 176, ["write file.tmp,", "then move over file"], "#e8f5ec")}
<text x="10" y="112" font-size="11" font-weight="700" fill="#165e3c">Load</text>
{box(574, 120, 176, ["read .yahtzee file"], "#e8f5ec")}{ar(572, 143, 528, 143)}
{box(376, 120, 150, ["Deserialize: shape,", "types, version"])}{ar(374, 143, 330, 143)}
{box(198, 120, 130, ["GameSnapshot", "(records)"], "#fdf6e3")}{ar(196, 143, 152, 143)}
{box(10, 120, 140, ["GameEngine.Restore:", "is this game possible?"])}
<text x="380" y="184" text-anchor="middle" font-size="9.5" fill="#555">Any problem on load becomes an InvalidSaveFileException with a readable message; the current game is not touched.</text>
</svg>"""


def svg_er():
    return """<svg viewBox="0 0 760 170" width="100%" font-family="Segoe UI, Arial">
<defs><marker id="a8" markerWidth="12" markerHeight="10" refX="11" refY="5" orient="auto"><path d="M0,0 L11,5 L0,10" fill="none" stroke="#333"/></marker></defs>
<rect x="40" y="20" width="230" height="110" rx="4" fill="#fff" stroke="#2b5d8a" stroke-width="1.3"/>
<rect x="40" y="20" width="230" height="26" rx="4" fill="#165e3c"/><text x="155" y="38" text-anchor="middle" font-size="12" font-weight="700" fill="#fff">Games</text>
<text x="52" y="66" font-size="10.5" font-family="Consolas, monospace">Id        INTEGER  PK</text>
<text x="52" y="84" font-size="10.5" font-family="Consolas, monospace">PlayedAt  TEXT  (ISO-8601 UTC)</text>
<rect x="430" y="20" width="300" height="140" rx="4" fill="#fff" stroke="#2b5d8a" stroke-width="1.3"/>
<rect x="430" y="20" width="300" height="26" rx="4" fill="#165e3c"/><text x="580" y="38" text-anchor="middle" font-size="12" font-weight="700" fill="#fff">GameResults</text>
<text x="442" y="66" font-size="10.5" font-family="Consolas, monospace">Id          INTEGER  PK</text>
<text x="442" y="84" font-size="10.5" font-family="Consolas, monospace">GameId      INTEGER  FK -> Games.Id</text>
<text x="442" y="102" font-size="10.5" font-family="Consolas, monospace">PlayerName  TEXT  COLLATE NOCASE</text>
<text x="442" y="120" font-size="10.5" font-family="Consolas, monospace">Score       INTEGER</text>
<text x="442" y="138" font-size="10.5" font-family="Consolas, monospace">IsAi        INTEGER (0/1)</text>
<line x1="270" y1="84" x2="428" y2="84" stroke="#333" stroke-width="1.3" marker-end="url(#a8)"/>
<text x="282" y="78" font-size="10">1</text><text x="372" y="78" font-size="10">many</text>
<text x="300" y="104" font-size="9.5" fill="#555">one game has one row</text><text x="300" y="117" font-size="9.5" fill="#555">per player</text>
</svg>"""


def save_excerpt():
    """A shortened, faithful view of the real example save: every field, but only a few of the 13 scores."""
    data = json.loads((Path(__file__).with_name("screenshots") / "example-save.yahtzee").read_text(encoding="utf-8"))
    q = json.dumps

    def player(p):
        scores = list(p["scores"].items())
        shown = ", ".join(f"{q(k)}: {q(v)}" for k, v in scores[:2])
        return (f'    {{\n      "name": {q(p["name"])},\n      "strategy": {q(p["strategy"])},\n'
                f'      "scores": {{ {shown}, ... (13 categories) ..., {q(scores[-1][0])}: {q(scores[-1][1])} }}\n    }}')

    players = ",\n".join(player(p) for p in data["players"])
    return (f'{{\n  "formatVersion": {data["formatVersion"]},\n  "startedAt": {q(data["startedAt"])},\n  "players": [\n{players}\n  ],\n'
            f'  "currentPlayerIndex": {data["currentPlayerIndex"]},\n  "rollsUsed": {data["rollsUsed"]},\n'
            f'  "dice": {q(data["dice"])},\n  "held": {q(data["held"])}\n}}')


def new_sections():
    excerpt = save_excerpt()
    return f"""
<h3 id="s57">5.7 The save-file format</h3>
<p>A save is one JSON text file with the extension <code>.yahtzee</code>. It can be written <b>at any moment of a turn</b>, including straight after a roll with some dice
held, because the dice values, the held flags and the number of rolls used are all part of it. This excerpt is from a real save made during the UI test (a human
against the hard computer, first roll made, die 1 held):</p>
<pre class="tree">{html.escape(excerpt)}</pre>
<p class="muted">Shortened for this guide: in the real file each of the 13 categories is listed on its own line for every player.</p>
<table class="t"><tr><th style="width:24%">Field</th><th>Meaning</th></tr>
<tr><td><code>formatVersion</code></td><td>Always 1 for this program; any other value is refused.</td></tr>
<tr><td><code>startedAt</code></td><td>When the game was started (ISO-8601 with time zone offset).</td></tr>
<tr><td><code>players</code></td><td>In turn order. <code>name</code>; <code>strategy</code> = <code>null</code> for a human or one of <code>Random</code>, <code>Greedy</code>,
<code>ExpectedValue</code>; <code>scores</code> = every category with its score, or <code>null</code> if the box is empty.</td></tr>
<tr><td><code>currentPlayerIndex</code></td><td>Whose turn it is (0 = first player).</td></tr>
<tr><td><code>rollsUsed</code></td><td>0&ndash;3 rolls already made in this turn.</td></tr>
<tr><td><code>dice</code>, <code>held</code></td><td>Five values (all 0 before the first roll) and five held flags.</td></tr>
</table>
<p>The round number is <b>not</b> saved: it is calculated from the scorecards. On loading, the file is checked in two layers (Figure 8): the serializer checks the
JSON shape, types and version; <code>GameEngine.Restore</code> then checks that the game is possible (valid dice, 1&ndash;6 unique players, a known strategy, and
scorecards consistent with whose turn it is).</p>
<figure>{svg_save_flow()}<figcaption>Figure 8. Saving and loading.</figcaption></figure>

<h3 id="s58">5.8 The games database</h3>
<p>Every finished game is recorded automatically in a SQLite database file, <code>%LOCALAPPDATA%\\Yahtzee\\yahtzee.db</code>, created on first use.</p>
<figure>{svg_er()}<figcaption>Figure 8b. Database schema: one Games row per finished game, one GameResults row per player.</figcaption></figure>
<table class="t"><tr><th style="width:30%">Operation</th><th>SQL (simplified)</th></tr>
<tr><td>Record a game</td><td><code>INSERT INTO Games</code>, then one <code>INSERT INTO GameResults</code> per player, all in <b>one transaction</b>.</td></tr>
<tr><td>Top 5 overall</td><td><code>SELECT … FROM GameResults r JOIN Games g ON g.Id = r.GameId ORDER BY r.Score DESC, g.PlayedAt LIMIT $count</code></td></tr>
<tr><td>Top 5 of a player</td><td>Same, with <code>WHERE r.PlayerName = $name</code> (case-insensitive because of <code>COLLATE NOCASE</code>).</td></tr>
<tr><td>Player names</td><td><code>SELECT DISTINCT PlayerName FROM GameResults ORDER BY PlayerName</code></td></tr>
</table>
<p>Values are always passed as <b>parameters</b> (<code>$name</code>, <code>$count</code>), never pasted into the SQL text, so a player called
<code>O'Brien"; DROP TABLE Games; --</code> is stored safely (there is a test for exactly that).</p>

<h3 id="s59">5.9 The computer players</h3>
<table class="t"><tr><th style="width:18%">Level</th><th style="width:20%">Class</th><th>How it plays</th><th style="width:12%">Average score*</th></tr>
<tr><td>Easy</td><td><code>RandomStrategy</code></td><td>Random holds, stops one time in three, scores in a random open box.</td><td>39</td></tr>
<tr><td>Medium</td><td><code>GreedyStrategy</code></td><td>Stops on a Yahtzee or large straight; keeps a run of four for straights; otherwise keeps its most common face.
Scores the most points available, sacrificing the cheapest box on ties.</td><td>176</td></tr>
<tr><td>Hard</td><td><code>ExpectedValueStrategy</code></td><td>For each of the 32 ways to hold dice, computes the exact average result of the next roll over all outcomes;
values results as "points above par" so strong boxes are saved for good rolls.</td><td>228</td></tr>
</table>
<p class="muted">* Average over 30 seeded single-player games, from the <code>StrongerStrategies_ScoreMoreOnAverage</code> test.</p>
<div class="box"><b>Expected value, briefly.</b> Suppose the hard computer holds three dice and re-rolls two. Two dice have 6 &times; 6 = 36 equally likely outcomes. For each
outcome it works out the best "points above par" it could then score, and averages the 36 numbers. It does this for all 32 hold choices (from re-rolling
nothing to re-rolling all five, which has 6<sup>5</sup> = 7,776 outcomes) and picks the highest average. If no re-roll beats keeping the current dice,
it stops. Because only the <i>sorted</i> dice matter, the 16,807 outcomes collapse to at most 252 different hands, which are cached.</div>
"""
