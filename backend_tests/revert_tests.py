import re

filepath = "D:/Projects/JOB/DEMO/sales_perf/backend_tests/AnalyticsTests.cs"

with open(filepath, "r", encoding="utf-8") as f:
    lines = f.readlines()

for i in range(len(lines)):
    if lines[i].startswith("// "):
        lines[i] = lines[i].replace("// ", "", 1)
    elif lines[i].startswith("    // "):
        lines[i] = lines[i].replace("    // ", "    ", 1)
    elif lines[i].startswith("        // "):
        lines[i] = lines[i].replace("        // ", "        ", 1)
    elif lines[i].startswith("            // "):
        lines[i] = lines[i].replace("            // ", "            ", 1)

with open(filepath, "w", encoding="utf-8") as f:
    f.writelines(lines)
