import csv
from collections import defaultdict

# Count locations by date
date_counts = defaultdict(int)

with open(r'C:\Users\abdum\source\repos\ConvoyV2\Convoy.Data\data-1772865763862.csv', 'r', encoding='utf-8') as f:
    reader = csv.DictReader(f)
    for row in reader:
        recorded_at = row['recorded_at']
        date_str = recorded_at.split(' ')[0]  # Extract date part
        date_counts[date_str] += 1

print("Dates in CSV file:")
for date, count in sorted(date_counts.items()):
    print(f"  {date}: {count} locations")
