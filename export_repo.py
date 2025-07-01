import subprocess
from pathlib import Path


def get_repo_files() -> list[str]:
    """Return tracked repository files excluding exports and this script."""
    files = subprocess.check_output(["git", "ls-files"]).decode().splitlines()
    return sorted(f for f in files if not f.startswith("export/") and f != "export_repo.py")


def read_all_lines(file_list: list[str]) -> list[str]:
    lines: list[str] = []
    for path in file_list:
        # include a header with the source filename so exported segments
        # can be traced back to their origin
        lines.append(f"-- {path} --\n")
        try:
            with open(path, "r", encoding="utf-8", errors="ignore") as fh:
                for line in fh:
                    lines.append(line if line.endswith("\n") else line + "\n")
        except Exception as exc:
            print(f"Error reading {path}: {exc}")
        # add a blank line after each file to make separation clear
        lines.append("\n")
    return lines


def write_exports(lines: list[str], export_root: Path) -> None:
    export_root.mkdir(parents=True, exist_ok=True)
    index = 1
    pos = 0
    while pos < len(lines):
        target = 225 if index % 2 == 1 else 180
        chunk = lines[pos:pos + target]
        pos += target
        if len(chunk) < target:
            chunk.extend(["\n"] * (target - len(chunk)))
        out_path = export_root / f"part_{index:03d}.txt"
        with open(out_path, "w", encoding="utf-8") as fh:
            fh.writelines(chunk)
        index += 1


def main() -> None:
    repo_files = get_repo_files()
    lines = read_all_lines(repo_files)
    write_exports(lines, Path("export"))
    print("Export complete")


if __name__ == "__main__":
    main()
