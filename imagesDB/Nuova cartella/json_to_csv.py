import pandas as pd
import json

# Load JSON file
with open(r"C:\Users\Utilizador\Desktop\images\imagesDB\manualdownload.Json", "r", encoding="utf-8") as f:
    data = json.load(f)

# Convert to DataFrame
df = pd.DataFrame(data)

# Save as CSV
df.to_csv("output.csv", index=False)