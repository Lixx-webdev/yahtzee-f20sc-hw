"""Builds build/Yahtzee_Code_Guide.html; build_guide.ps1 then prints it to docs/Yahtzee_Code_Guide.pdf with Edge.

Every source file is shown in full with line numbers; annotations (ann_*.py) are anchored to
source lines and the build fails if an anchor is missing, so explanations cannot drift from
the code and every line belongs to an explained segment.
"""
import base64, html, re, sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent.parent          # repository root (docs/guide-src -> repo)
SHOTS = HERE / "screenshots"       # screenshots of the running application
OUT = HERE / "build"               # generated HTML (not part of the submission)
sys.path.insert(0, str(HERE))
from ann_core import BUILD_FILES, CORE_FILES
from ann_ui import UI_FILES
from ann_tests import TEST_FILES
from ann_new import PUBLISH_FILES, PERSISTENCE_FILES, DATA_FILES, AI_FILES, NEW_TEST_FILES
import narrative as N

esc = html.escape

# ---------------------------------------------------------------- highlighting
CS_KEYWORDS = set("""abstract as base bool break byte case catch char checked class const continue decimal default delegate do double else enum
event explicit extern false finally fixed float for foreach get goto if implicit in init int interface internal is lock long namespace new null
object operator out override params private protected public readonly record ref return sbyte sealed set short sizeof stackalloc static string
struct switch this throw true try typeof uint ulong unchecked unsafe ushort using var virtual void volatile while when and or not with value
required global partial where yield async await nameof dynamic""".split())
CS_TOKEN = re.compile(r'(?P<doc>///.*$)|(?P<com>//.*$)|(?P<str>\$?@?"(?:[^"\\]|\\.)*")|(?P<chr>\'(?:[^\'\\]|\\.)\')'
                      r'|(?P<num>\b\d+(?:\.\d+)?[fFmMdD]?\b)|(?P<id>[A-Za-z_]\w*)|(?P<other>.)')
XML_TOKEN = re.compile(r'(?P<com><!--.*?-->)|(?P<tag></?[\w.:]+|/?>)|(?P<str>"[^"]*")|(?P<other>.)')


def hl_cs(line):
    out = []
    for m in CS_TOKEN.finditer(line):
        kind, text = m.lastgroup, m.group()
        t = esc(text)
        if kind == "doc":
            out.append(f'<span class="doc">{t}</span>')
        elif kind == "com":
            out.append(f'<span class="com">{t}</span>')
        elif kind in ("str", "chr"):
            out.append(f'<span class="str">{t}</span>')
        elif kind == "num":
            out.append(f'<span class="num">{t}</span>')
        elif kind == "id":
            if text in CS_KEYWORDS:
                out.append(f'<span class="kw">{t}</span>')
            elif text[0].isupper():
                out.append(f'<span class="ty">{t}</span>')
            else:
                out.append(t)
        else:
            out.append(t)
    return "".join(out)


def hl_xml(line):
    out = []
    for m in XML_TOKEN.finditer(line):
        kind, t = m.lastgroup, esc(m.group())
        out.append({"com": f'<span class="com">{t}</span>', "tag": f'<span class="kw">{t}</span>',
                    "str": f'<span class="str">{t}</span>'}.get(kind, t))
    return "".join(out)


def highlighter(path):
    return hl_cs if path.endswith(".cs") else (hl_xml if path.endswith((".csproj", ".props", ".slnx", ".pubxml")) else esc)


# ---------------------------------------------------------------- segmentation
def segments(path, entries):
    lines = (ROOT / path).read_text(encoding="utf-8").splitlines()
    starts, prev = [], -1
    for entry in entries:
        anchor, text = entry[0], entry[1]
        expect = entry[2] if len(entry) > 2 else None
        exact = anchor.startswith("=")
        needle = anchor[1:] if exact else anchor
        for i in range(prev + 1, len(lines)):
            if (lines[i].strip() == needle) if exact else (needle in lines[i]):
                break
        else:
            raise SystemExit(f"{path}: anchor not found after line {prev + 1}: {anchor!r}")
        if not starts and i != 0:
            raise SystemExit(f"{path}: first anchor must match line 1, matched line {i + 1}")
        starts.append((i, text, expect))
        prev = i
    segs = []
    for n, (start, text, expect) in enumerate(starts):
        end = starts[n + 1][0] if n + 1 < len(starts) else len(lines)
        while end - 1 > start and not lines[end - 1].strip():
            end -= 1  # trailing blank lines are shown but not counted in the label
        real_end = starts[n + 1][0] if n + 1 < len(starts) else len(lines)
        body = lines[start:real_end]
        if expect and not any(f"{expect}(" in l for l in body):
            raise SystemExit(f"{path}: segment at line {start + 1} does not contain test {expect}")
        segs.append((start + 1, end, real_end, text))
    return lines, segs


def render_file(num, path, title, intro, entries):
    lines, segs = segments(path, entries)
    hl = highlighter(path)
    parts = [f'<section class="file" id="f-{num}"><h3>{num} {esc(title)}</h3>'
             f'<div class="path">{esc(path)} &middot; {len(lines)} lines</div><p>{intro}</p>']
    for first, last, real_end, text in segs:
        code = "\n".join(f'<span class="ln">{i:>3}</span>{hl(lines[i - 1]) or " "}' for i in range(first, real_end + 1) if i <= len(lines))
        label = f"Line {first}" if first == last else f"Lines {first}&ndash;{last}"
        parts.append(f'<div class="seg"><pre class="code">{code}</pre><div class="exp"><span class="lr">{label}</span>{text}</div></div>')
    parts.append("</section>")
    return "\n".join(parts), len(lines), len(segs)


# ---------------------------------------------------------------- images
# Window screenshots include Windows' invisible resize border (about 8 px left, right and bottom),
# which shows whatever was behind the window, so it is cropped off.
CROP = 8
from io import BytesIO
from PIL import Image


def load_png(name):
    with Image.open(SHOTS / f"{name}.png") as im:
        w, h = im.size
        cropped = im.crop((CROP, 0, w - CROP, h - CROP))
        buf = BytesIO()
        cropped.save(buf, format="PNG", optimize=True)
        return base64.b64encode(buf.getvalue()).decode(), cropped.size


def img(name, caption, width="100%"):
    data, _ = load_png(name)
    return (f'<figure><img src="data:image/png;base64,{data}" style="width:{width}">'
            f'<figcaption>{caption}</figcaption></figure>')


def annotated_ui():
    data, (w, h) = load_png("turn-held")
    marks = "".join(
        f'<div class="callout" style="left:{(x - CROP) / w * 100:.2f}%;top:{y / h * 100:.2f}%">{n}</div>'
        for n, (x, y, _) in enumerate(N.UI_CALLOUTS, 1))
    legend = "".join(f'<li><span class="num">{n}</span> {text}</li>' for n, (_, _, text) in enumerate(N.UI_CALLOUTS, 1))
    return (f'<figure><div class="annot"><img src="data:image/png;base64,{data}">{marks}</div>'
            f'<figcaption>Figure 9. Anatomy of the main window during a turn (a human against the hard computer, '
            f'after the first roll, die 1 held).</figcaption></figure>'
            f'<ol class="legend">{legend}</ol>')


# ---------------------------------------------------------------- assemble
def main():
    groups = [("9", "Build and project files", BUILD_FILES + PUBLISH_FILES),
              ("10", "Yahtzee.Core: the game logic", CORE_FILES + PERSISTENCE_FILES + DATA_FILES + AI_FILES),
              ("11", "Yahtzee.WinForms: the user interface", UI_FILES),
              ("12", "Yahtzee.Core.Tests: the unit tests", TEST_FILES + NEW_TEST_FILES)]
    walkthrough, toc_files, total_lines, total_segs = [], [], 0, 0
    for gnum, gtitle, files in groups:
        walkthrough.append(f'<h2 id="g-{gnum}" class="newpage">{gnum}. Line-by-line walkthrough: {esc(gtitle)}</h2>')
        sub = []
        for k, (path, title, intro, entries) in enumerate(files, 1):
            num = f"{gnum}.{k}"
            body, nlines, nsegs = render_file(num, path, title, intro, entries)
            walkthrough.append(body)
            sub.append(f'<li><a href="#f-{num}">{num} {esc(title)}</a> <span class="muted">({esc(path)})</span></li>')
            total_lines += nlines
            total_segs += nsegs
        toc_files.append(f'<li><a href="#g-{gnum}">{gnum}. Walkthrough: {esc(gtitle)}</a><ul>{"".join(sub)}</ul></li>')

    doc = N.TEMPLATE.format(
        css=N.CSS,
        cover=N.cover(),
        toc=N.toc("".join(toc_files)),
        narrative=N.body(img=img, annotated_ui=annotated_ui, total_lines=total_lines, total_segs=total_segs),
        walkthrough="\n".join(walkthrough),
        closing=N.closing(),
    )
    OUT.mkdir(exist_ok=True)
    out = OUT / "Yahtzee_Code_Guide.html"
    out.write_text(doc, encoding="utf-8")
    print(f"OK: {out} ({total_lines} source lines in {total_segs} explained segments)")


if __name__ == "__main__":
    main()
