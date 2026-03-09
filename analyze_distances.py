import csv
from datetime import datetime
from collections import defaultdict

# Read CSV file
locations_by_user_date = defaultdict(list)

with open(r'C:\Users\abdum\source\repos\ConvoyV2\Convoy.Data\data-1772865763862.csv', 'r', encoding='utf-8') as f:
    reader = csv.DictReader(f)
    for row in reader:
        user_id = int(row['user_id'])
        recorded_at = row['recorded_at']
        distance = row['distance_from_previous']

        # Extract date from timestamp
        date_str = recorded_at.split(' ')[0]  # "2026-03-05"

        # Parse distance (handle NULL)
        distance_val = 0.0
        if distance and distance != 'NULL':
            distance_val = float(distance)

        locations_by_user_date[(user_id, date_str)].append({
            'recorded_at': recorded_at,
            'distance': distance_val
        })

# Calculate totals
print("=" * 80)
print("USER DAILY DISTANCE SUMMARY")
print("=" * 80)

for (user_id, date), locations in sorted(locations_by_user_date.items()):
    total_distance_m = sum(loc['distance'] for loc in locations)
    total_distance_km = total_distance_m / 1000.0
    count = len(locations)

    print(f"\nUser: {user_id} | Date: {date}")
    print(f"  Locations: {count}")
    print(f"  Total Distance: {total_distance_m:.2f} meters = {total_distance_km:.2f} km")

    # Show first 10 distances
    print(f"  First 10 distances: {[loc['distance'] for loc in locations[:10]]}")

    # Count NULL/0 distances
    zero_count = sum(1 for loc in locations if loc['distance'] == 0.0)
    print(f"  Zero distances: {zero_count} / {count} ({zero_count*100/count:.1f}%)")

# Compare with report
print("\n" + "=" * 80)
print("COMPARING WITH DAILY DISTANCE REPORTS")
print("=" * 80)

with open(r'C:\Users\abdum\source\repos\ConvoyV2\Convoy.Data\daily_distance_reports.csv', 'r', encoding='utf-8') as f:
    reader = csv.DictReader(f)
    for row in reader:
        user_id = int(row['user_id'])
        report_date = row['report_date'].split(' ')[0]
        total_km = float(row['total_distance_km'])
        location_count = int(row['location_count'])

        # Find matching calculated data
        key = (user_id, report_date)
        if key in locations_by_user_date:
            calculated_locations = locations_by_user_date[key]
            calculated_km = sum(loc['distance'] for loc in calculated_locations) / 1000.0
            calculated_count = len(calculated_locations)

            print(f"\nUser: {user_id} | Date: {report_date}")
            print(f"  Report: {total_km:.2f} km ({location_count} locations)")
            print(f"  Calculated: {calculated_km:.2f} km ({calculated_count} locations)")
            print(f"  Difference: {abs(calculated_km - total_km):.2f} km")

            if abs(calculated_km - total_km) > 0.01:
                print(f"  ⚠️ MISMATCH DETECTED!")
