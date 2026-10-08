# Code guide generator

Builds `docs/Yahtzee_Code_Guide.pdf`, the guide to the code, its design and the development log. This is a documentation tool, not part of the game.

```powershell
powershell -File docs\guide-src\build_guide.ps1
```

You need Python 3 with Pillow (`pip install pillow`) and Microsoft Edge.

| File | Purpose |
|---|---|
| `build.py` | Reads every source file, highlights it, joins it with its explanations, and writes `build/Yahtzee_Code_Guide.html` |
| `ann_core.py`, `ann_new.py`, `ann_ui.py`, `ann_tests.py` | The line-by-line explanations, one list per source file |
| `narrative.py` | The written sections, the diagrams (inline SVG), and the page style |
| `check.py` | Debugging aid: `python check.py MainForm.cs` lists where each explanation starts |
| `screenshots/` | Screenshots of the running app, plus an example save file |
| `build_guide.ps1` | Runs `build.py`, then prints the HTML to PDF with headless Edge |

## How explanations stay in sync with the code

Each explanation is anchored to the source line where it starts, using text from that line. An anchor that begins with `=` must match the whole trimmed line.

An explanation runs until the next one starts, so every line of every file belongs to an explained segment.

If the code changes and an anchor no longer matches, `build.py` stops and names the file and anchor. Update the matching `ann_*.py` entry, then rebuild.
