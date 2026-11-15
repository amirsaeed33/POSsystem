from pathlib import Path
import re

root = Path(r"d:\GitHub\POSsystem\maui\src\SmartPos.Mobile")

fixes = [
    (
        Path(r"d:\GitHub\POSsystem\maui\src\SmartPos.Mobile\ViewModels\TrackOrderViewModel.cs"),
        "int.TryParse(starParam, out var star, isDestructive: false)",
        "int.TryParse(starParam, out var star)",
    ),
    (
        Path(r"d:\GitHub\POSsystem\maui\src\SmartPos.Mobile\ViewModels\CustomerProfileViewModel.cs"),
        "Please enter a valid mobile number (at least 7 digits, isDestructive: false).",
        "Please enter a valid mobile number (at least 7 digits).",
    ),
]

for path, old, new in fixes:
    t = path.read_text(encoding="utf-8")
    if old in t:
        path.write_text(t.replace(old, new), encoding="utf-8")
        print("fixed", path.name)

for f in root.rglob("*.cs"):
    t = f.read_text(encoding="utf-8")
    orig = t

    t = re.sub(
        r"(ShowAlertAsync\([^;]*?),\s*isDestructive:\s*(?:true|false)\s*\)",
        r"\1)",
        t,
        flags=re.DOTALL,
    )

    t = re.sub(
        r"AljannatDialogService\.Current\.ShowConfirmAsync\(([^;\n]*?),\s*\"OK\"\s*\)",
        r'AljannatDialogService.Current.ShowAlertAsync(\1, "OK")',
        t,
    )

    t = re.sub(
        r"AljannatDialogService\.Current\.ShowConfirmAsync\(([^;\n]*?),\s*\"OK\",\s*isDestructive:\s*(?:true|false)\s*\)",
        r'AljannatDialogService.Current.ShowAlertAsync(\1, "OK")',
        t,
    )

    t = re.sub(
        r"(ShowPromptAsync\([^;]*?),\s*maxLength:\s*\d+\s*,\s*isDestructive:\s*(true|false)\s*\)",
        r'\1, initialValue: "", isDestructive: \2)',
        t,
        flags=re.DOTALL,
    )
    t = re.sub(
        r"(ShowPromptAsync\([^;]*?),\s*maxLength:\s*\d+\s*\)",
        r"\1)",
        t,
        flags=re.DOTALL,
    )

    if t != orig:
        f.write_text(t, encoding="utf-8")
        print("cleaned", f.relative_to(root))
