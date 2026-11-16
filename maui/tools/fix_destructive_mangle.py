from pathlib import Path
import re

root = Path(r"d:\GitHub\POSsystem\maui\src\SmartPos.Mobile")

for f in root.rglob("*.cs"):
    t = f.read_text(encoding="utf-8")
    orig = t
    # Remove illegally injected named args everywhere first.
    t = t.replace(", isDestructive: false)", ")")
    t = t.replace(", isDestructive: true)", ")")
    t = t.replace(", isDestructive: false", "")
    t = t.replace(", isDestructive: true", "")
    # Fix Trim(, ) style if any residual
    t = t.replace("Trim()", "Trim()")
    t = re.sub(r"Trim\(\s*,\s*\)", "Trim()", t)
    if t != orig:
        f.write_text(t, encoding="utf-8")
        print("stripped", f.relative_to(root))

# Targeted dialog fixes after strip
replacements = {
    Path(r"d:\GitHub\POSsystem\maui\src\SmartPos.Mobile\ViewModels\CartCheckoutViewModel.cs"): [
        (
            'await AljannatDialogService.Current.ShowAlertAsync("Clear Tray", "Remove all items from your tray?", "Clear", "Cancel")',
            'await AljannatDialogService.Current.ShowConfirmAsync("Clear Tray", "Remove all items from your tray?", "Clear", "Cancel", isDestructive: true)',
        ),
        (
            'await AljannatDialogService.Current.ShowConfirmAsync("Clear Tray", "Remove all items from your tray?", "Clear", "Cancel")',
            'await AljannatDialogService.Current.ShowConfirmAsync("Clear Tray", "Remove all items from your tray?", "Clear", "Cancel", isDestructive: true)',
        ),
    ],
}

# Broader pass: convert 4-arg ShowAlertAsync -> ShowConfirmAsync
four_alert = re.compile(
    r"AljannatDialogService\.Current\.ShowAlertAsync\(\s*"
    r"([^,]+)\s*,\s*"
    r"((?:[^,]|\$\"[^\"]*\"|\$@\"[^\"]*\")+)\s*,\s*"
    r"([^,]+)\s*,\s*"
    r"([^)]+)\)",
    re.DOTALL,
)

destructive_titles = ("clear tray", "delete address", "cancel order", "remove item", "reject")


def convert_four(m: re.Match) -> str:
    title, message, accept, cancel = [g.strip() for g in m.groups()]
    blob = f"{title} {accept}".lower()
    destructive = any(k in blob for k in destructive_titles) or (
        "cancel" in title.lower() and "order" in message.lower()
    ) or ("delete" in title.lower()) or ("remove" in title.lower() and "remove" in accept.lower())
    flag = "true" if destructive else "false"
    return (
        f"AljannatDialogService.Current.ShowConfirmAsync({title}, {message}, "
        f"{accept}, {cancel}, isDestructive: {flag})"
    )


three_confirm_ok = re.compile(
    r"AljannatDialogService\.Current\.ShowConfirmAsync\(\s*"
    r"((?:[^,]|\$\"[^\"]*\")+?)\s*,\s*"
    r"((?:[^,]|\$\"[^\"]*\")+?)\s*,\s*"
    r"\"OK\"\s*\)",
    re.DOTALL,
)

for f in root.rglob("*.cs"):
    t = f.read_text(encoding="utf-8")
    orig = t
    t = four_alert.sub(convert_four, t)
    t = three_confirm_ok.sub(
        r'AljannatDialogService.Current.ShowAlertAsync(\1, \2, "OK")',
        t,
    )
    if t != orig:
        f.write_text(t, encoding="utf-8")
        print("normalized", f.relative_to(root))

print("done")
