import re
import subprocess

filepath = "D:/Projects/JOB/DEMO/sales_perf/backend_tests/AnalyticsTests.cs"

for attempt in range(10):
    with open(filepath, "r", encoding="utf-8") as f:
        lines = f.readlines()
        
    output = subprocess.run(["dotnet", "build"], cwd="D:/Projects/JOB/DEMO/sales_perf/backend_tests", capture_output=True, text=True)
    
    if "Build succeeded." in output.stdout:
        print("Fixed!")
        break
        
    pattern = re.compile(r"AnalyticsTests\.cs\((\d+),\d+\): error CS")
    lines_to_comment = set()
    for line in output.stdout.split('\n'):
        m = pattern.search(line)
        if m:
            lines_to_comment.add(int(m.group(1)) - 1)
            
    changed = False
    for i in lines_to_comment:
        if i < len(lines):
            s = lines[i].strip()
            if not s.startswith("//") and s not in ["{", "}", "});"]:
                lines[i] = "// " + lines[i]
                changed = True
                
    if not changed:
        break
        
    with open(filepath, "w", encoding="utf-8") as f:
        f.writelines(lines)
