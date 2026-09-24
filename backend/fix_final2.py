filepath = "D:/Projects/JOB/DEMO/sales_perf/backend/Infrastructure/Data/DataSeeder.cs"

with open(filepath, "r", encoding="utf-8") as f:
    lines = f.readlines()

for i in range(len(lines)):
    if 60 <= i <= 105:
        if lines[i].strip().startswith("//"):
            lines[i] = lines[i].replace("// ", "", 1)
            lines[i] = lines[i].replace("//", "", 1)

with open(filepath, "w", encoding="utf-8") as f:
    f.writelines(lines)
