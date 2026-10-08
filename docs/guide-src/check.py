import re, sys
import build
from ann_core import BUILD_FILES, CORE_FILES
from ann_ui import UI_FILES
from ann_tests import TEST_FILES

want = set(sys.argv[1:])
for path, _title, _intro, entries in BUILD_FILES + CORE_FILES + UI_FILES + TEST_FILES:
    if path.split("/")[-1] not in want:
        continue
    lines, segs = build.segments(path, entries)
    print("==", path)
    for first, last, _real, text in segs:
        print(f"{first:>3}-{last:<3} {lines[first - 1].strip()[:58]:<58} | {re.sub('<[^>]+>', '', text)[:42]}")
