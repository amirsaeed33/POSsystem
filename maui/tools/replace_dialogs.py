from pathlib import Path
import re

root = Path(r"d:\GitHub\POSsystem\maui\src\SmartPos.Mobile")
files = list(root.rglob("*.cs"))


def ensure_using(s: str) -> str:
    if "AljannatDialogService" in s and "using SmartPos.Mobile.Services;" not in s:
        lines = s.splitlines(keepends=True)
        last_using = -1
        for i, line in enumerate(lines):
            if line.startswith("using "):
                last_using = i
        if last_using >= 0:
            lines.insert(last_using + 1, "using SmartPos.Mobile.Services;\n")
            return "".join(lines)
    return s


def transform(content: str) -> str:
    content = re.sub(
        r"_\s*=\s*Shell\.Current\.DisplayAlert\(",
        "_ = AljannatDialogService.Current.ShowAlertAsync(",
        content,
    )
    content = re.sub(
        r"Shell\.Current\?\.DisplayAlert\(",
        "_ = AljannatDialogService.Current.ShowAlertAsync(",
        content,
    )
    content = re.sub(
        r"await\s+Shell\.Current\.DisplayAlert\(",
        "await AljannatDialogService.Current.ShowAlertAsync(",
        content,
    )
    content = re.sub(
        r"await\s+Shell\.Current\.DisplayPromptAsync\(",
        "await AljannatDialogService.Current.ShowPromptAsync(",
        content,
    )
    content = re.sub(
        r"await\s+Shell\.Current\.DisplayActionSheet\(",
        "await AljannatDialogService.Current.ShowActionSheetAsync(",
        content,
    )

    # 4-arg ShowAlertAsync -> ShowConfirmAsync
    pattern = re.compile(
        r"AljannatDialogService\.Current\.ShowAlertAsync\(\s*"
        r"([^,]+?)\s*,\s*"
        r"((?:[^,]|\$(?:\"[^\"]*\"))+?)\s*,\s*"
        r"([^,]+?)\s*,\s*"
        r"([^)]+?)\s*\)",
        re.DOTALL,
    )

    def to_confirm(m: re.Match) -> str:
        title, message, accept, cancel = (
            m.group(1).strip(),
            m.group(2).strip(),
            m.group(3).strip(),
            m.group(4).strip(),
        )
        blob = f"{title} {accept} {message}".lower()
        destructive = False
        if any(k in blob for k in ["clear tray", "delete address", "delete ", "remove item", "remove photo", "reject"]):
            destructive = True
        if "cancel" in title.lower() and "order" in blob:
            destructive = True
        if re.search(r"\bremove\b", title.lower()) and "remove" in accept.lower():
            destructive = True
        if "clear" in title.lower() and "clear" in accept.lower():
            destructive = True
        if "delete" in title.lower():
            destructive = True

        dest = "true" if destructive else "false"
        return (
            f"AljannatDialogService.Current.ShowConfirmAsync({title}, {message}, "
            f"{accept}, {cancel}, isDestructive: {dest})"
        )

    content = pattern.sub(to_confirm, content)
    return ensure_using(content)


changed = []
for f in files:
    text = f.read_text(encoding="utf-8")
    if (
        "DisplayAlert(" not in text
        and "DisplayActionSheet(" not in text
        and "DisplayPromptAsync(" not in text
    ):
        continue
    new = transform(text)
    if new != text:
        f.write_text(new, encoding="utf-8")
        changed.append(str(f))

print("Changed", len(changed), "files")
for c in changed:
    print(c)

left = []
for f in files:
    t = f.read_text(encoding="utf-8")
    if re.search(r"DisplayAlert\(|DisplayActionSheet\(|DisplayPromptAsync\(", t):
        left.append(str(f))
print("LEFT", len(left))
for l in left:
    print(l)
