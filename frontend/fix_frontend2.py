import os

files_to_fix = {
    "D:/Projects/JOB/DEMO/sales_perf/frontend/src/lib/api.ts": [26],
    "D:/Projects/JOB/DEMO/sales_perf/frontend/vite.config.ts": [23]
}

for filepath, lines_to_fix in files_to_fix.items():
    with open(filepath, "r", encoding="utf-8") as f:
        lines = f.readlines()
        
    for idx in lines_to_fix:
        i = idx - 1
        s = lines[i].lstrip()
        if not s.startswith("//") and not s.startswith("/*"):
            lines[i] = "// " + lines[i].lstrip()
            
    with open(filepath, "w", encoding="utf-8") as f:
        f.writelines(lines)

print("Fixed known frontend crumbs")
