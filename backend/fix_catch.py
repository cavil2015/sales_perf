import re

files = [
    "D:/Projects/JOB/DEMO/sales_perf/backend/Program.cs",
    "D:/Projects/JOB/DEMO/sales_perf/backend/Infrastructure/Data/DataSeeder.cs"
]

for filepath in files:
    with open(filepath, "r", encoding="utf-8") as f:
        content = f.read()
    
    # Uncomment catch blocks
    content = re.sub(r"^[ \t]*//[ \t]*(catch \()", r"        \1", content, flags=re.MULTILINE)
    content = re.sub(r"^[ \t]*//[ \t]*({)", r"        \1", content, flags=re.MULTILINE)
    content = re.sub(r"^[ \t]*//[ \t]*(var logger = services\.GetRequiredService)", r"            \1", content, flags=re.MULTILINE)
    content = re.sub(r"^[ \t]*//[ \t]*(logger\.LogCritical)", r"            \1", content, flags=re.MULTILINE)
    content = re.sub(r"^[ \t]*//[ \t]*(throw;)", r"            \1", content, flags=re.MULTILINE)
    content = re.sub(r"^[ \t]*//[ \t]*(})", r"        \1", content, flags=re.MULTILINE)
    content = re.sub(r"^[ \t]*//[ \t]*(Console\.WriteLine)", r"            \1", content, flags=re.MULTILINE)
    
    with open(filepath, "w", encoding="utf-8") as f:
        f.write(content)

