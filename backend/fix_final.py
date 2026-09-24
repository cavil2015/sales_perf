import re

filepath = "D:/Projects/JOB/DEMO/sales_perf/backend/Infrastructure/Data/DataSeeder.cs"

with open(filepath, "r", encoding="utf-8") as f:
    lines = f.readlines()

for i, line in enumerate(lines):
    if 50 <= i <= 110:
        s = line.strip()
        if s in ["{", "}", "});"]:
            lines[i] = "// " + line

with open(filepath, "w", encoding="utf-8") as f:
    f.writelines(lines)
