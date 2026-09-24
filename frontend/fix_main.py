import re

filepath = "D:/Projects/JOB/DEMO/sales_perf/frontend/src/main.tsx"

with open(filepath, "r", encoding="utf-8") as f:
    lines = f.readlines()

for i in range(len(lines)):
    s = lines[i].strip()
    if s == "Brittle Module Resolution":
        lines[i] = "// " + lines[i].lstrip()
    elif s == "Import Hoisting Violation":
        lines[i] = "// " + lines[i].lstrip()
    elif s == "Promise Rejection Default Behavior":
        lines[i] = "// " + lines[i].lstrip()
    elif s == "Pre-Mount Synchronous Crashes":
        lines[i] = "// " + lines[i].lstrip()
    elif s == "Microfrontend ID Collisions (useId Namespace)":
        lines[i] = "// " + lines[i].lstrip()
    elif s == "React 18 Recoverable Error Telemetry":
        lines[i] = "// " + lines[i].lstrip()
    elif "If App or any child later uses React 18" in lines[i]:
        lines[i] = "      {/* " + lines[i].lstrip()

with open(filepath, "w", encoding="utf-8") as f:
    f.writelines(lines)
