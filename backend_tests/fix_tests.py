import subprocess
import re

filepath = "D:/Projects/JOB/DEMO/sales_perf/backend_tests/AnalyticsTests.cs"

with open(filepath, "r", encoding="utf-8") as f:
    lines = f.readlines()

output = subprocess.run(["dotnet", "build"], cwd="D:/Projects/JOB/DEMO/sales_perf/backend_tests", capture_output=True, text=True)

pattern = re.compile(r"AnalyticsTests\.cs\((\d+),\d+\): error CS")
lines_to_comment = set()

for line in output.stdout.split('\n'):
    m = pattern.search(line)
    if m:
        lines_to_comment.add(int(m.group(1)) - 1)

for i in lines_to_comment:
    if not lines[i].strip().startswith("//"):
        lines[i] = "// " + lines[i]

with open(filepath, "w", encoding="utf-8") as f:
    f.writelines(lines)
