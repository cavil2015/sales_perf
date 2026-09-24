import re
import subprocess
import os

output = subprocess.run(["npx.cmd", "tsc", "-b"], cwd="D:/Projects/JOB/DEMO/sales_perf/frontend", capture_output=True, text=True)

pattern = re.compile(r"([a-zA-Z0-9_/\.\-]+)\((\d+),\d+\): error TS")
errors = {}
for line in output.stdout.split('\n'):
    m = pattern.search(line)
    if m:
        filepath = os.path.join("D:/Projects/JOB/DEMO/sales_perf/frontend", m.group(1).strip())
        filepath = os.path.normpath(filepath)
        line_num = int(m.group(2)) - 1
        if filepath not in errors:
            errors[filepath] = set()
        errors[filepath].add(line_num)

for filepath, lines in errors.items():
    if not os.path.exists(filepath):
        continue
    print(f"\n--- {filepath} ---")
    with open(filepath, "r", encoding="utf-8") as f:
        file_lines = f.readlines()
        for i in sorted(list(lines)):
            if i < len(file_lines):
                print(f"Line {i+1}: {file_lines[i].rstrip()}")
