from pathlib import Path

root = Path(r"d:\GitHub\POSsystem\maui\src\SmartPos.Mobile")
skip = {
    "AljannatDialogOverlay.xaml.cs",
    "AljannatDialogPage.xaml.cs",
    "AljannatDialogService.cs",
    "IAljannatDialogService.cs",
}

for f in root.rglob("*.cs"):
    if f.name in skip:
        continue
    t = f.read_text(encoding="utf-8")
    orig = t
    while ", isDestructive: false" in t or ", isDestructive: true" in t:
        t = t.replace(", isDestructive: false", "")
        t = t.replace(", isDestructive: true", "")
    # Fix leftover Trim(, )
    t = t.replace("Trim(, )", "Trim()")
    t = t.replace("Trim(,)", "Trim()")
    # Fix BranchSheetItem signature if broken
    t = t.replace(
        "public BranchSheetItem(OnlineBranchDto branch, bool isActive)",
        "public BranchSheetItem(OnlineBranchDto branch, bool isActive)",
    )
    if t != orig:
        f.write_text(t, encoding="utf-8")
        print("fixed", f.relative_to(root))

print("remaining illegal injections:")
for f in root.rglob("*.cs"):
    if f.name in skip:
        continue
    t = f.read_text(encoding="utf-8")
    if "isDestructive: false" in t or "isDestructive: true" in t:
        for i, line in enumerate(t.splitlines(), 1):
            if "isDestructive:" in line:
                print(f"{f.name}:{i}: {line.strip()[:120]}")
