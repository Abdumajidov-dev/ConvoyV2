import csv

print("Daily Distance Reports:")
print("=" * 80)

with open(r'C:\Users\abdum\source\repos\ConvoyV2\Convoy.Data\daily_distance_reports.csv', 'r', encoding='utf-8') as f:
    reader = csv.DictReader(f)
    for row in reader:
        user_id = row['user_id']
        report_date = row['report_date'].split(' ')[0]
        total_km = row['total_distance_km']
        location_count = row['location_count']

        print(f"User {user_id} | Date: {report_date} | {total_km} km | {location_count} locations")
